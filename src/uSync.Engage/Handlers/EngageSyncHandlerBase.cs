using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace uSync.Engage.Handlers;

/// <summary>
///  base for the Engage handlers.
/// </summary>
/// <remarks>
///  Engage settings are flat lists (children such as personas live inside their group's file),
///  so there are no folders and everything is returned at the root.
/// </remarks>
public abstract class EngageSyncHandlerBase<TObject> : SyncHandlerRoot<TObject, TObject>, IEngageSyncHandler
    where TObject : class
{
    protected EngageSyncHandlerBase(
        ILogger<SyncHandlerRoot<TObject, TObject>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    { }

    public override string Group => EngageConstants.Group;

    /// <summary>
    ///  every item of this type in Engage.
    /// </summary>
    protected abstract IEnumerable<TObject> GetAllItems();

    protected override Task<IEnumerable<TObject>> GetChildItemsAsync(TObject? parent)
        => Task.FromResult(parent is null ? GetAllItems() : []);

    protected override Task<IEnumerable<TObject>> GetFoldersAsync(TObject? parent)
        => Task.FromResult(Enumerable.Empty<TObject>());

    protected override async Task<TObject?> GetFromServiceAsync(TObject? item)
        => item is null ? null : await GetFromServiceAsync(GetItemKey(item));

    protected override string GetItemName(TObject item)
        => GetItemAlias(item);

    /// <summary>
    ///  Engage has no way to tell us about everything that's been deleted, so 'clean' imports
    ///  don't remove items. (delete markers in the folder still work).
    /// </summary>
    protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(TObject parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => Task.FromResult(Enumerable.Empty<uSyncAction>());

    /// <summary>
    ///  'OneWay' or 'CreateOnly' in a handler's settings means only import items that don't exist yet.
    /// </summary>
    protected override async Task<bool> ShouldImportAsync(XElement node, HandlerSettings config)
    {
        if (config.GetSetting("OneWay", false) || config.GetSetting("CreateOnly", false))
        {
            var item = await GetFromServiceAsync(node.GetKey());
            return item is null;
        }

        return await base.ShouldImportAsync(node, config);
    }

    private Dictionary<Guid, TObject>? _snapshot;
    private readonly SemaphoreSlim _changeLock = new(1, 1);

    /// <summary>
    ///  remember what Engage has now, so the next change can spot deletes.
    /// </summary>
    public Task TakeSnapshotAsync()
    {
        _snapshot = ToKeyedItems(GetAllItems());
        return Task.CompletedTask;
    }

    /// <summary>
    ///  something of this type has been saved or deleted in Engage.
    /// </summary>
    /// <remarks>
    ///  Engage's events don't say which item changed, so everything of this type is
    ///  exported again (there are rarely more than a handful), and anything that has
    ///  gone since the last snapshot gets a delete marker.
    /// </remarks>
    public async Task EngageItemsChangedAsync()
    {
        await _changeLock.WaitAsync();
        try
        {
            var previous = _snapshot;
            var current = ToKeyedItems(GetAllItems());
            _snapshot = current;

            // also false while uSync is importing, so our own saves don't export again.
            if (!ShouldProcessEvent()) return;

            var folders = GetDefaultHandlerFolders();

            foreach (var item in current.Values)
            {
                var actions = await ExportAsync(item, folders, DefaultConfig);
                foreach (var action in actions.Where(x => x.Success && x.FileName is not null))
                {
                    await CleanUpAsync(item, action.FileName!, folders.Last());
                }
            }

            if (previous is null) return;

            foreach (var deleted in previous.Where(x => !current.ContainsKey(x.Key)))
            {
                await ExportDeletedItemAsync(deleted.Value, folders, DefaultConfig);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to export Engage {type} items after a change", ItemType);
        }
        finally
        {
            _changeLock.Release();
        }
    }

    private Dictionary<Guid, TObject> ToKeyedItems(IEnumerable<TObject> items)
        => items
            .GroupBy(GetItemKey)
            .Where(x => x.Key != Guid.Empty)
            .ToDictionary(x => x.Key, x => x.First());
}
