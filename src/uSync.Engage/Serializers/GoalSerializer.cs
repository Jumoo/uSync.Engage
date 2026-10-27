using System.Globalization;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Engage.Infrastructure.Analytics.Goals;
using Umbraco.Engage.Infrastructure.Analytics.Processed;
using Umbraco.Engage.Infrastructure.Personalization;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Engage.Services;

namespace uSync.Engage.Serializers;

/// <remarks>
///  the goal type config is kept as Engage stores it. the only reference in it is the
///  pageview goal's list of pages, which are already content keys.
/// </remarks>
[SyncSerializer("4F2965EB-FE49-481A-89E3-A4C2CB9CF56B", "Engage Goal Serializer", EngageConstants.ItemTypes.Goal)]
public class GoalSerializer : EngageSerializerBase<IGoal>
{
    private readonly IGoalService _goalService;
    private readonly EngageReferenceMapper _referenceMapper;

    public GoalSerializer(
        ILogger<SyncSerializerRoot<IGoal>> logger,
        IGoalService goalService,
        EngageReferenceMapper referenceMapper)
        : base(logger)
    {
        _goalService = goalService;
        _referenceMapper = referenceMapper;
    }

    public override Guid ItemKey(IGoal item) => item.Key;

    public override string ItemAlias(IGoal item) => item.Name;

    // GetById reads the database, and is the only lookup that includes the scoring.
    // it also returns deleted (invalid) goals, so importing one brings it back.
    protected override Task<IGoal?> FindByKeyAsync(Guid key)
        => Task.FromResult(_goalService.GetById(key));

    protected override Task<IGoal?> FindByNameAsync(string name)
    {
        var goal = _goalService.GetAllIncludingInvalid().FirstOrDefault(x => name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(goal is null ? null : _goalService.GetById(goal.Key));
    }

    public override Task SaveItemAsync(IGoal item)
    {
        _goalService.Save(item);
        return Task.CompletedTask;
    }

    /// <summary>
    ///  Engage never removes goals, deleting one marks it invalid.
    /// </summary>
    public override Task DeleteItemAsync(IGoal item)
    {
        var goal = Copy(item);
        goal.IsInvalid = true;
        goal.Updated = DateTime.UtcNow;
        goal.UpdatedByUmbracoUserKey = ImportUserKey;
        _goalService.Save(goal);
        return Task.CompletedTask;
    }

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(IGoal item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, item.Name);

        node.Add(new XElement("Info",
            new XElement("Name", item.Name),
            new XElement("Value", item.Value),
            new XElement("GoalTypeId", item.GoalTypeId),
            new XElement("IsMain", item.IsMain),
            new XElement("IsInverted", item.IsInverted),
            new XElement("IsActive", item.IsActive),
            new XElement("IsImplicitScoringEnabled", item.IsImplicitScoringEnabled)));

        node.Add(new XElement("Config", new XCData(item.GoalTypeConfig ?? string.Empty)));

        node.Add(ScoringXml.Serialize("PersonaScoring",
            (item.ImplicitPersonaScoring ?? []).Select(x => (_referenceMapper.GetPersonaKey(x.PersonaId), x.Score, x.IsLocked))));

        node.Add(ScoringXml.Serialize("CustomerJourneyScoring",
            (item.ImplicitCustomerJourneyStepScoring ?? []).Select(x => (_referenceMapper.GetCustomerJourneyStepKey(x.CustomerJourneyStepId), x.Score, x.IsLocked))));

        return Task.FromResult(SucceedExport(item.Name, node));
    }

    protected override async Task<SyncAttempt<IGoal>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();
        var changes = new List<uSyncChange>();

        var existing = await FindItemAsync(node);
        var info = node.Element("Info");

        var goal = new Goal
        {
            Id = existing?.Id ?? 0,
            Key = key,
            Name = info?.Element("Name").ValueOrDefault(alias) ?? alias,
            Value = decimal.TryParse(info?.Element("Value")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0,
            GoalTypeId = info?.Element("GoalTypeId").ValueOrDefault(Guid.Empty) ?? Guid.Empty,
            GoalTypeConfig = node.Element("Config").ValueOrDefault(string.Empty),
            IsMain = info?.Element("IsMain").ValueOrDefault(false) ?? false,
            IsInverted = info?.Element("IsInverted").ValueOrDefault(false) ?? false,
            IsActive = info?.Element("IsActive").ValueOrDefault(true) ?? true,
            IsImplicitScoringEnabled = info?.Element("IsImplicitScoringEnabled").ValueOrDefault(false) ?? false,
            IsInvalid = false,
            Created = existing?.Created ?? DateTime.UtcNow,
            CreatedByUmbracoUserKey = existing?.CreatedByUmbracoUserKey ?? ImportUserKey,
            Updated = DateTime.UtcNow,
            UpdatedByUmbracoUserKey = ImportUserKey,
        };

        goal.ImplicitPersonaScoring = MergeScores(
            ScoringXml.Deserialize(node.Element("PersonaScoring")),
            existing?.ImplicitPersonaScoring?.Select(x => x.PersonaId),
            _referenceMapper.GetPersonaId,
            (id, score) => new PersonaScore { PersonaId = id, ScoreType = PersonalizationScoreType.GoalCompletion, Key = Guid.NewGuid(), Created = DateTime.UtcNow, Score = score.Value, IsLocked = score.IsLocked },
            "Persona", changes);

        goal.ImplicitCustomerJourneyStepScoring = MergeScores(
            ScoringXml.Deserialize(node.Element("CustomerJourneyScoring")),
            existing?.ImplicitCustomerJourneyStepScoring?.Select(x => x.CustomerJourneyStepId),
            _referenceMapper.GetCustomerJourneyStepId,
            (id, score) => new CustomerJourneyStepScore { CustomerJourneyStepId = id, ScoreType = PersonalizationScoreType.GoalCompletion, Key = Guid.NewGuid(), Created = DateTime.UtcNow, Score = score.Value, IsLocked = score.IsLocked },
            "CustomerJourneyStep", changes);

        _goalService.Save(goal);

        return Succeed(alias, _goalService.GetById(key) ?? goal, changes);
    }

    /// <summary>
    ///  Engage saves goal scores by goal + persona/step and never deletes a row, so a score
    ///  that's on this site but not in the file is set back to zero.
    /// </summary>
    private static TScore[] MergeScores<TScore>(
        IEnumerable<ScoringXml.Score> scores,
        IEnumerable<long>? existingIds,
        Func<Guid, long?> getId,
        Func<long, ScoringXml.Score, TScore> create,
        string type,
        List<uSyncChange> changes)
    {
        var result = new Dictionary<long, TScore>();

        foreach (var score in scores)
        {
            var id = getId(score.Key);
            if (id is null)
            {
                changes.Add(uSyncChange.Warning($"Scoring/{type}", score.Key.ToString(), $"{type} {score.Key} not found, score not set"));
                continue;
            }
            result[id.Value] = create(id.Value, score);
        }

        foreach (var id in existingIds ?? [])
        {
            if (!result.ContainsKey(id))
                result[id] = create(id, new ScoringXml.Score(Guid.Empty, 0, false));
        }

        return [.. result.Values];
    }

    private static Goal Copy(IGoal item) => new()
    {
        Id = item.Id,
        Key = item.Key,
        Name = item.Name,
        Value = item.Value,
        GoalTypeId = item.GoalTypeId,
        GoalTypeConfig = item.GoalTypeConfig,
        IsMain = item.IsMain,
        IsInverted = item.IsInverted,
        IsActive = item.IsActive,
        IsInvalid = item.IsInvalid,
        IsImplicitScoringEnabled = item.IsImplicitScoringEnabled,
        ImplicitPersonaScoring = item.ImplicitPersonaScoring,
        ImplicitCustomerJourneyStepScoring = item.ImplicitCustomerJourneyStepScoring,
        Created = item.Created,
        CreatedByUmbracoUserKey = item.CreatedByUmbracoUserKey,
        Updated = item.Updated,
        UpdatedByUmbracoUserKey = item.UpdatedByUmbracoUserKey,
    };
}
