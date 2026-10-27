using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Engage.Infrastructure.Personalization;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Engage.Services;

namespace uSync.Engage.Serializers;

/// <summary>
///  persona groups and customer journeys are the same shape: a group of scoring settings
///  holding an ordered list of personas or journey steps, all saved in one go.
/// </summary>
/// <remarks>
///  Engage matches the children by their numeric id when it saves, and removes (along with
///  all their scoring data) any that aren't in the list. so on import every child is matched
///  to the one on this site, by key then title, and given that one's id.
/// </remarks>
public abstract class PersonalizationGroupSerializerBase<TGroup, TItem> : EngageSerializerBase<TGroup>
    where TGroup : PersonalizationGroupBase, new()
    where TItem : PersonalizationEntityBase, new()
{
    protected readonly EngageReferenceMapper _referenceMapper;

    protected PersonalizationGroupSerializerBase(
        ILogger<SyncSerializerRoot<TGroup>> logger,
        EngageReferenceMapper referenceMapper)
        : base(logger)
    {
        _referenceMapper = referenceMapper;
    }

    protected abstract string ItemsElementName { get; }
    protected abstract string ItemElementName { get; }

    protected abstract IReadOnlyList<TGroup> GetAllGroups();
    protected abstract TGroup? GetGroup(Guid key);
    protected abstract void SaveGroup(TGroup group);
    protected abstract void DeleteGroup(Guid key);

    protected abstract List<TItem> GetItems(TGroup group);
    protected abstract void SetItems(TGroup group, List<TItem> items);

    public override Guid ItemKey(TGroup item) => item.Key;

    public override string ItemAlias(TGroup item) => item.Title ?? item.Key.ToString();

    protected override Task<TGroup?> FindByKeyAsync(Guid key)
        => Task.FromResult(GetGroup(key));

    protected override Task<TGroup?> FindByNameAsync(string name)
        => Task.FromResult(GetAllGroups().FirstOrDefault(x => name.Equals(x.Title, StringComparison.OrdinalIgnoreCase)));

    public override Task SaveItemAsync(TGroup item)
    {
        SaveGroup(item);
        return Task.CompletedTask;
    }

    public override Task DeleteItemAsync(TGroup item)
    {
        DeleteGroup(item.Key);
        return Task.CompletedTask;
    }

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(TGroup item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        node.Add(new XElement("Info",
            new XElement("Title", item.Title ?? string.Empty),
            new XElement("Description", item.Description ?? string.Empty),
            new XElement("MinimumParticipationScoreThreshold", item.MinimumParticipationScoreThreshold),
            new XElement("MinimumDeviationType", item.MinimumDeviationType),
            new XElement("MinimumDeviation", item.MinimumDeviation),
            new XElement("ExpirationType", item.ExpirationType),
            new XElement("Expiration", item.Expiration),
            new XElement("UpperScoreLimit", item.UpperScoreLimit)));

        var items = new XElement(ItemsElementName);
        foreach (var child in GetItems(item))
        {
            items.Add(new XElement(ItemElementName,
                new XAttribute("Key", child.Key),
                new XElement("Title", child.Title ?? string.Empty),
                new XElement("Description", child.Description ?? string.Empty),
                new XElement("Icon", GetIconValue(child.Icon)),
                new XElement("Color", child.Color ?? string.Empty)));
        }
        node.Add(items);

        return Task.FromResult(SucceedExport(ItemAlias(item), node));
    }

    protected override async Task<SyncAttempt<TGroup>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();
        var changes = new List<uSyncChange>();

        var existing = await FindItemAsync(node);

        var group = new TGroup
        {
            Id = existing?.Id ?? 0,
            Key = key,
            Created = existing?.Created ?? DateTime.UtcNow,
            CreatedByUmbracoUserKey = existing?.CreatedByUmbracoUserKey ?? ImportUserKey,
            Updated = DateTime.UtcNow,
            UpdatedByUmbracoUserKey = ImportUserKey,
        };

        var info = node.Element("Info");
        group.Title = info?.Element("Title").ValueOrDefault(alias) ?? alias;
        group.Description = info?.Element("Description").ValueOrDefault(string.Empty);
        group.MinimumParticipationScoreThreshold = GetNullableInt(info?.Element("MinimumParticipationScoreThreshold"));
        group.MinimumDeviationType = GetNullableEnum<MinimumDeviationType>(info?.Element("MinimumDeviationType"));
        group.MinimumDeviation = GetNullableInt(info?.Element("MinimumDeviation"));
        group.ExpirationType = GetNullableEnum<ExpirationType>(info?.Element("ExpirationType"));
        group.Expiration = GetNullableInt(info?.Element("Expiration"));
        group.UpperScoreLimit = GetNullableInt(info?.Element("UpperScoreLimit"));

        var existingItems = existing is null ? [] : GetItems(existing);
        var items = new List<TItem>();

        foreach (var element in node.Element(ItemsElementName)?.Elements(ItemElementName) ?? [])
        {
            var itemKey = element.Attribute("Key").ValueOrDefault(Guid.Empty);
            var title = element.Element("Title").ValueOrDefault(string.Empty);

            var match = existingItems.FirstOrDefault(x => x.Key == itemKey)
                ?? existingItems.FirstOrDefault(x => !string.IsNullOrEmpty(title) && title.Equals(x.Title, StringComparison.OrdinalIgnoreCase));

            items.Add(new TItem
            {
                Id = match?.Id ?? 0,
                Key = itemKey == Guid.Empty ? Guid.NewGuid() : itemKey,
                Title = title,
                Description = element.Element("Description").ValueOrDefault(string.Empty),
                Icon = GetIconId(element.Element("Icon").ValueOrDefault(string.Empty), title, changes),
                Color = element.Element("Color").ValueOrDefault(string.Empty),
                Created = match?.Created ?? DateTime.UtcNow,
                CreatedByUmbracoUserKey = match?.CreatedByUmbracoUserKey ?? ImportUserKey,
                Updated = DateTime.UtcNow,
                UpdatedByUmbracoUserKey = ImportUserKey,
            });
        }

        SetItems(group, items);
        SaveGroup(group);

        // Engage doesn't write the new ids back, so read it again.
        return Succeed(alias, GetGroup(key) ?? group, changes);
    }

    /// <summary>
    ///  icons of 1000 or more are media ids (Engage's own rule), stored here as the media key.
    /// </summary>
    private string GetIconValue(int? icon)
    {
        if (icon is null) return string.Empty;
        if (icon >= 1000) return _referenceMapper.GetMediaKey(icon)?.ToString() ?? string.Empty;
        return icon.Value.ToString();
    }

    private int? GetIconId(string value, string itemName, List<uSyncChange> changes)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (Guid.TryParse(value, out var mediaKey))
        {
            var id = _referenceMapper.GetMediaId(mediaKey);
            if (id is null)
                changes.Add(uSyncChange.Warning($"{ItemElementName}/{itemName}", "Icon", $"Media {mediaKey} not found, icon not set"));
            return id;
        }

        return int.TryParse(value, out var icon) ? icon : null;
    }
}
