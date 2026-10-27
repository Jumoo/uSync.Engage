using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Permissions.UserGroupPermissions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

namespace uSync.Engage.Handlers;

/// <remarks>
///  Engage raises no events for permissions, so these are only written by a full export.
/// </remarks>
[SyncHandler("engageUserGroupPermissionHandler", "User Perms", EngageConstants.Folders.UserGroupPermissions, EngageConstants.Priorities.UserGroupPermissions,
    Icon = "icon-users-alt", EntityType = EngageConstants.UdiEntityType.UserGroupPermission)]
public class UserGroupPermissionHandler : EngageSyncHandlerBase<UserGroupPermission>, ISyncHandler
{
    private readonly IUserGroupPermissionRepository _repository;

    public UserGroupPermissionHandler(
        ILogger<SyncHandlerRoot<UserGroupPermission, UserGroupPermission>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        IUserGroupPermissionRepository repository)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _repository = repository;
    }

    protected override IEnumerable<UserGroupPermission> GetAllItems() => _repository.GetAll();
}
