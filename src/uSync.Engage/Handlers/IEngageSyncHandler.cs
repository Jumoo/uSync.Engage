namespace uSync.Engage.Handlers;

/// <summary>
///  an Engage handler that exports when Engage tells us something has changed.
/// </summary>
public interface IEngageSyncHandler
{
    Task TakeSnapshotAsync();

    Task EngageItemsChangedAsync();
}
