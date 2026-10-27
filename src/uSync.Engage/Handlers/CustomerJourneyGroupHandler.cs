using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Personalization.CustomerJourney;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Engage.Handlers;

[SyncHandler("engageCustomerJourneyHandler", "Customer Journeys", EngageConstants.Folders.CustomerJourneyGroups, EngageConstants.Priorities.CustomerJourneyGroups,
    Icon = "icon-footprints", EntityType = EngageConstants.UdiEntityType.CustomerJourneyGroup)]
public class CustomerJourneyGroupHandler : EngageSyncHandlerBase<CustomerJourneyGroup>, ISyncHandler
{
    private readonly ICustomerJourneyGroupRepository _repository;

    public CustomerJourneyGroupHandler(
        ILogger<SyncHandlerRoot<CustomerJourneyGroup, CustomerJourneyGroup>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        ICustomerJourneyGroupRepository repository)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _repository = repository;
    }

    protected override IEnumerable<CustomerJourneyGroup> GetAllItems() => _repository.GetAll();
}
