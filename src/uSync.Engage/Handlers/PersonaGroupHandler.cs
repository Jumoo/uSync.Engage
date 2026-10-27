using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Personalization.Personas;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Engage.Handlers;

[SyncHandler("engagePersonaGroupHandler", "Personas", EngageConstants.Folders.PersonaGroups, EngageConstants.Priorities.PersonaGroups,
    Icon = "icon-users", EntityType = EngageConstants.UdiEntityType.PersonaGroup)]
public class PersonaGroupHandler : EngageSyncHandlerBase<PersonaGroup>, ISyncHandler
{
    private readonly IPersonaGroupRepository _repository;

    public PersonaGroupHandler(
        ILogger<SyncHandlerRoot<PersonaGroup, PersonaGroup>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        IPersonaGroupRepository repository)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _repository = repository;
    }

    protected override IEnumerable<PersonaGroup> GetAllItems() => _repository.GetAll();
}
