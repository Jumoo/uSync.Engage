using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Models;
using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Repository;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Engage.Handlers;

[SyncHandler("engageTrafficFilterHandler", "Engage Traffic Filters", EngageConstants.Folders.TrafficFilters, EngageConstants.Priorities.TrafficFilters,
    Icon = "icon-block", EntityType = EngageConstants.UdiEntityType.TrafficFilter)]
public class TrafficFilterHandler : EngageSyncHandlerBase<TrafficFilter>, ISyncHandler
{
    private readonly ITrafficFilterRepository _repository;

    public TrafficFilterHandler(
        ILogger<SyncHandlerRoot<TrafficFilter, TrafficFilter>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        ITrafficFilterRepository repository)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _repository = repository;
    }

    protected override IEnumerable<TrafficFilter> GetAllItems()
        => _repository.GetAllAsync().GetAwaiter().GetResult();
}
