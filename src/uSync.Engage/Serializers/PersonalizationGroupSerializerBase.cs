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
                GetIconElement(child.Icon),
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
                Icon = GetIconId(element.Element("Icon"), title, changes),
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
    ///  icons of 1000 or more are media ids (Engage's own rule), stored here as the media key
    ///  with the media's path by name alongside.
    /// </summary>
    /// <remarks>
    ///  Engage creates its own icon library on every site with new keys, so on another site the
    ///  key is only there if the media has been synced. the path finds Engage's copy when it isn't.
    /// </remarks>
    private XElement GetIconElement(int? icon)
    {
        // string.Empty keeps empty icons as <Icon></Icon>, as files from earlier versions have them.
        var element = new XElement("Icon", string.Empty);
        if (icon is null) return element;

        if (icon >= 1000)
        {
            element.Value = _referenceMapper.GetMediaKey(icon)?.ToString() ?? string.Empty;
            var path = _referenceMapper.GetMediaPath(icon);
            if (path is not null) element.SetAttributeValue("Path", path);
        }
        else
        {
            element.Value = icon.Value.ToString();
        }

        return element;
    }

    private int? GetIconId(XElement? element, string itemName, List<uSyncChange> changes)
    {
        var value = element.ValueOrDefault(string.Empty);
        if (string.IsNullOrWhiteSpace(value)) return null;

        var id = ResolveIcon(element!);
        if (id is null && Guid.TryParse(value, out var mediaKey))
        {
            var path = element!.Attribute("Path")?.Value;
            changes.Add(uSyncChange.Warning($"{ItemElementName}/{itemName}", "Icon",
                string.IsNullOrWhiteSpace(path)
                    ? $"Media {mediaKey} not found, icon not set"
                    : $"Media {mediaKey} ({path}) not found, icon not set"));
        }

        return id;
    }

    /// <summary>
    ///  the icon this site would use for an Icon element: the media with that key, else the
    ///  media at that path, or a plain (pre-media) icon number.
    /// </summary>
    private int? ResolveIcon(XElement element)
    {
        var value = element.Value;
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (Guid.TryParse(value, out var mediaKey))
        {
            return _referenceMapper.GetMediaId(mediaKey)
                ?? _referenceMapper.GetMediaIdByPath(element.Attribute("Path")?.Value);
        }

        return int.TryParse(value, out var icon) ? icon : null;
    }

    /// <summary>
    ///  icons are compared by the media they point to on this site, not by key and path.
    /// </summary>
    /// <remarks>
    ///  an icon found by path (or a copy of Engage's media in another folder) serializes with this
    ///  site's key or path, so comparing the xml as written would report a change on every run.
    /// </remarks>
    protected override XElement CleanseNode(XElement node)
    {
        var icons = node.Element(ItemsElementName)?.Elements(ItemElementName)
            .Select(x => x.Element("Icon"))
            .OfType<XElement>()
            .ToList();

        if (icons is null || icons.Count == 0) return base.CleanseNode(node);

        var copy = new XElement(node);
        foreach (var icon in copy.Element(ItemsElementName)!.Elements(ItemElementName).Select(x => x.Element("Icon")).OfType<XElement>())
        {
            var id = ResolveIcon(icon);
            icon.RemoveAttributes();
            icon.Value = id?.ToString() ?? string.Empty;
        }

        return base.CleanseNode(copy);
    }
}
