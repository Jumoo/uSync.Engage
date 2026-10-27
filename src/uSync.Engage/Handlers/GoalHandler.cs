using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Analytics.Goals;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Engage.Handlers;

[SyncHandler("engageGoalHandler", "Engage Goals", EngageConstants.Folders.Goals, EngageConstants.Priorities.Goals,
    Icon = "icon-target", EntityType = EngageConstants.UdiEntityType.Goal)]
public class GoalHandler : EngageSyncHandlerBase<IGoal>, ISyncHandler
{
    private readonly IGoalService _goalService;

    public GoalHandler(
        ILogger<SyncHandlerRoot<IGoal, IGoal>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        IGoalService goalService)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _goalService = goalService;
    }

    // GetAll doesn't load the scoring, GetById does.
    protected override IEnumerable<IGoal> GetAllItems()
        => _goalService.GetAll()
            .Select(x => _goalService.GetById(x.Key))
            .OfType<IGoal>();
}
