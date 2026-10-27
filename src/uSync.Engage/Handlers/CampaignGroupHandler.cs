using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Personalization.Campaign;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Engage.Handlers;

[SyncHandler("engageCampaignGroupHandler", "Campaign Groups", EngageConstants.Folders.CampaignGroups, EngageConstants.Priorities.CampaignGroups,
    Icon = "icon-megaphone", EntityType = EngageConstants.UdiEntityType.CampaignGroup)]
public class CampaignGroupHandler : EngageSyncHandlerBase<CampaignGroup>, ISyncHandler
{
    private readonly ICampaignGroupRepository _repository;

    public CampaignGroupHandler(
        ILogger<SyncHandlerRoot<CampaignGroup, CampaignGroup>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        ICampaignGroupRepository repository)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _repository = repository;
    }

    protected override IEnumerable<CampaignGroup> GetAllItems() => _repository.GetAll();
}
