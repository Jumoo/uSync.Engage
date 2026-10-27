using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Engage.Data.Common.NPoco;
using Umbraco.Engage.Infrastructure.Events;
using Umbraco.Engage.Infrastructure.Personalization.Campaign;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Engage.Services;

using EngageCampaign = Umbraco.Engage.Infrastructure.Personalization.Campaign.Campaign;

namespace uSync.Engage.Serializers;

/// <remarks>
///  campaigns and their scores live inside the group's file. Engage matches them by numeric
///  id when it saves and removes any that aren't in the list, so each one is matched to the
///  one on this site first.
/// </remarks>
[SyncSerializer("E4C5DD3E-0754-477F-B0DB-9E553668BAEB", "Engage Campaign Group Serializer", EngageConstants.ItemTypes.CampaignGroup)]
public class CampaignGroupSerializer : EngageSerializerBase<CampaignGroup>
{
    private readonly ICampaignGroupRepository _repository;
    private readonly IDatabaseContextProvider _databaseContextProvider;
    private readonly EngageReferenceMapper _referenceMapper;

    public CampaignGroupSerializer(
        ILogger<SyncSerializerRoot<CampaignGroup>> logger,
        ICampaignGroupRepository repository,
        IDatabaseContextProvider databaseContextProvider,
        EngageReferenceMapper referenceMapper)
        : base(logger)
    {
        _repository = repository;
        _databaseContextProvider = databaseContextProvider;
        _referenceMapper = referenceMapper;
    }

    public override Guid ItemKey(CampaignGroup item) => item.Key;

    public override string ItemAlias(CampaignGroup item) => item.Name ?? item.Key.ToString();

    protected override Task<CampaignGroup?> FindByKeyAsync(Guid key)
        => Task.FromResult(_repository.Get(key));

    protected override Task<CampaignGroup?> FindByNameAsync(string name)
        => Task.FromResult(_repository.GetAll().FirstOrDefault(x => name.Equals(x.Name, StringComparison.OrdinalIgnoreCase)));

    public override Task SaveItemAsync(CampaignGroup item)
    {
        _repository.Save(item);
        return Task.CompletedTask;
    }

    public override Task DeleteItemAsync(CampaignGroup item)
    {
        _repository.Delete(item.Key);
        return Task.CompletedTask;
    }

    private static readonly string[] _utmFields = [
        "UtmSource", "UtmMedium", "UtmCampaign", "UtmTerm", "UtmContent",
        "UtmSourcePlatform", "UtmCreativeFormat", "UtmMarketingTactic"];

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(CampaignGroup item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        node.Add(new XElement("Info",
            new XElement("Name", item.Name ?? string.Empty),
            new XElement("Description", item.Description ?? string.Empty)));

        var campaigns = new XElement("Campaigns");
        foreach (var campaign in item.Campaigns.OrderBy(x => x.Key))
        {
            campaigns.Add(new XElement("Campaign",
                new XAttribute("Key", campaign.Key),
                _utmFields.Select(x => new XElement(x, GetUtm(campaign, x) ?? string.Empty))));
        }
        node.Add(campaigns);

        node.Add(ScoringXml.Serialize("PersonaScoring",
            item.PersonaScoring.Select(x => (_referenceMapper.GetPersonaKey(x.PersonaId), x.Score, x.IsLocked))));

        node.Add(ScoringXml.Serialize("CustomerJourneyScoring",
            item.CustomerJourneyScoring.Select(x => (_referenceMapper.GetCustomerJourneyStepKey(x.CustomerJourneyStepId), x.Score, x.IsLocked))));

        return Task.FromResult(SucceedExport(ItemAlias(item), node));
    }

    protected override async Task<SyncAttempt<CampaignGroup>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();
        var changes = new List<uSyncChange>();

        var existing = await FindItemAsync(node) ?? RestoreDeletedGroup(key);
        var info = node.Element("Info");

        var group = new CampaignGroup
        {
            Id = existing?.Id ?? 0,
            Key = key,
            Name = info?.Element("Name").ValueOrDefault(alias) ?? alias,
            Description = info?.Element("Description").ValueOrDefault(string.Empty),
            Created = existing?.Created ?? DateTime.UtcNow,
            CreatedByUmbracoUserKey = existing?.CreatedByUmbracoUserKey ?? ImportUserKey,
            Updated = DateTime.UtcNow,
            UpdatedByUmbracoUserKey = ImportUserKey,
        };

        foreach (var element in node.Element("Campaigns")?.Elements("Campaign") ?? [])
        {
            var campaignKey = element.Attribute("Key").ValueOrDefault(Guid.Empty);
            var match = existing?.Campaigns.FirstOrDefault(x => x.Key == campaignKey);

            var campaign = new EngageCampaign
            {
                Id = match?.Id ?? 0,
                Key = campaignKey == Guid.Empty ? Guid.NewGuid() : campaignKey,
                CampaignGroupId = group.Id,
                Created = match?.Created ?? DateTime.UtcNow,
                CreatedByUmbracoUserKey = match?.CreatedByUmbracoUserKey ?? ImportUserKey,
                Updated = DateTime.UtcNow,
                UpdatedByUmbracoUserKey = ImportUserKey,
            };

            foreach (var field in _utmFields)
            {
                var value = element.Element(field).ValueOrDefault(string.Empty);
                SetUtm(campaign, field, string.IsNullOrEmpty(value) ? null : value);
            }

            group.Campaigns.Add(campaign);
        }

        foreach (var score in ScoringXml.Deserialize(node.Element("PersonaScoring")))
        {
            var personaId = _referenceMapper.GetPersonaId(score.Key);
            if (personaId is null)
            {
                changes.Add(uSyncChange.Warning("Scoring/Persona", score.Key.ToString(), $"Persona {score.Key} not found, score not set"));
                continue;
            }

            var match = existing?.PersonaScoring.FirstOrDefault(x => x.PersonaId == personaId);
            group.PersonaScoring.Add(new CampaignGroupPersonaScoring
            {
                Id = match?.Id ?? 0,
                Key = match?.Key ?? Guid.NewGuid(),
                CampaignGroupId = group.Id,
                PersonaId = personaId.Value,
                Score = score.Value,
                IsLocked = score.IsLocked,
                Created = match?.Created ?? DateTime.UtcNow,
                Updated = DateTime.UtcNow,
            });
        }

        foreach (var score in ScoringXml.Deserialize(node.Element("CustomerJourneyScoring")))
        {
            var stepId = _referenceMapper.GetCustomerJourneyStepId(score.Key);
            if (stepId is null)
            {
                changes.Add(uSyncChange.Warning("Scoring/CustomerJourneyStep", score.Key.ToString(), $"Customer journey step {score.Key} not found, score not set"));
                continue;
            }

            var match = existing?.CustomerJourneyScoring.FirstOrDefault(x => x.CustomerJourneyStepId == stepId);
            group.CustomerJourneyScoring.Add(new CampaignGroupCustomerJourneyScoring
            {
                Id = match?.Id ?? 0,
                Key = match?.Key ?? Guid.NewGuid(),
                CampaignGroupId = group.Id,
                CustomerJourneyStepId = stepId.Value,
                Score = score.Value,
                IsLocked = score.IsLocked,
                Created = match?.Created ?? DateTime.UtcNow,
                Updated = DateTime.UtcNow,
            });
        }

        _repository.Save(group);

        return Succeed(alias, _repository.Get(key) ?? group, changes);
    }

    /// <summary>
    ///  Engage never removes a deleted campaign group, it only marks it invalid. the row keeps
    ///  its key, so a new group with that key can't be added - bring the old one back instead.
    /// </summary>
    private CampaignGroup? RestoreDeletedGroup(Guid key)
    {
        using (var context = _databaseContextProvider.CreateContext())
        {
            var restored = context.Database.Execute(
                "UPDATE [umbracoEngagePersonalizationCampaignGroup] SET [invalid] = 0 WHERE [key] = @0 AND [invalid] = 1", key);
            context.Complete();

            if (restored == 0) return null;
        }

        // the event is what makes Engage reset its campaign group cache.
        SystemEventService.Raise(new CampaignGroupSavingEvent());

        logger.LogInformation("Restored deleted Engage campaign group {key} so it can be imported", key);
        return _repository.Get(key);
    }

    private static string? GetUtm(EngageCampaign campaign, string field) => field switch
    {
        "UtmSource" => campaign.UtmSource,
        "UtmMedium" => campaign.UtmMedium,
        "UtmCampaign" => campaign.UtmCampaign,
        "UtmTerm" => campaign.UtmTerm,
        "UtmContent" => campaign.UtmContent,
        "UtmSourcePlatform" => campaign.UtmSourcePlatform,
        "UtmCreativeFormat" => campaign.UtmCreativeFormat,
        "UtmMarketingTactic" => campaign.UtmMarketingTactic,
        _ => null
    };

    private static void SetUtm(EngageCampaign campaign, string field, string? value)
    {
        switch (field)
        {
            case "UtmSource": campaign.UtmSource = value; break;
            case "UtmMedium": campaign.UtmMedium = value; break;
            case "UtmCampaign": campaign.UtmCampaign = value; break;
            case "UtmTerm": campaign.UtmTerm = value; break;
            case "UtmContent": campaign.UtmContent = value; break;
            case "UtmSourcePlatform": campaign.UtmSourcePlatform = value; break;
            case "UtmCreativeFormat": campaign.UtmCreativeFormat = value; break;
            case "UtmMarketingTactic": campaign.UtmMarketingTactic = value; break;
        }
    }
}
