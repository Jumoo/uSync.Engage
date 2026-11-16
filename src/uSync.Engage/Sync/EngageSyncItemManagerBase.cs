using Umbraco.Cms.Core;

using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Sync;

namespace uSync.Engage.Sync;

/// <summary>
///  tells uSync.Complete what an Engage item is when someone pushes or pulls it.
/// </summary>
/// <remarks>
///  only uSync.Complete loads item managers, so without it these do nothing.
///
///  Engage's backoffice gives each item its key as the entity's unique. Engage settings have
///  no children (personas and steps live inside their group's file), so an item is only ever
///  itself.
/// </remarks>
public abstract class EngageSyncItemManagerBase<TObject> : SyncItemManagerBase, ISyncItemManager
    where TObject : class
{
    private readonly ISyncItemFactory _itemFactory;

    protected EngageSyncItemManagerBase(ISyncItemFactory itemFactory)
    {
        _itemFactory = itemFactory;
    }

    protected abstract string Icon { get; }

    public async Task<SyncEntity?> GetSyncEntityAsync(string key)
    {
        if (Guid.TryParse(key, out var guid) is false) return null;

        var serializer = _itemFactory.GetSerializers<TObject>().FirstOrDefault();
        if (serializer is null) return null;

        var item = await serializer.FindItemAsync(guid);
        if (item is null) return null;

        return new SyncEntity
        {
            Icon = Icon,
            Name = serializer.ItemAlias(item),
            Udi = Udi.Create(EntityType, serializer.ItemKey(item))
        };
    }

    protected override Task<IEnumerable<SyncItem>> GetDescendantsAsync(SyncItem item, DependencyFlags flags)
        => Task.FromResult(Enumerable.Empty<SyncItem>());
}
