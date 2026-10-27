using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Personalization.Segments;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Engage.Handlers;

[SyncHandler("engageSegmentHandler", "Engage Segments", EngageConstants.Folders.Segments, EngageConstants.Priorities.Segments,
    Icon = "icon-filter", EntityType = EngageConstants.UdiEntityType.Segment)]
public class SegmentHandler : EngageSyncHandlerBase<Segment>, ISyncHandler
{
    private readonly ISegmentRepository _repository;

    public SegmentHandler(
        ILogger<SyncHandlerRoot<Segment, Segment>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        ISegmentRepository repository)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _repository = repository;
    }

    protected override IEnumerable<Segment> GetAllItems() => _repository.GetAll();
}
