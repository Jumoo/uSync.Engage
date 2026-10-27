using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Engage.Infrastructure.Permissions.DocumentTypePermissions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Engage.Services;

namespace uSync.Engage.Handlers;

/// <remarks>
///  Engage raises no events for permissions, so these are only written by a full export.
/// </remarks>
[SyncHandler("engageDocumentTypePermissionHandler", "Doc Perms", EngageConstants.Folders.DocumentTypePermissions, EngageConstants.Priorities.DocumentTypePermissions,
    Icon = "icon-lock", EntityType = EngageConstants.UdiEntityType.DocumentTypePermission)]
public class DocumentTypePermissionHandler : EngageSyncHandlerBase<DocumentTypePermission>, ISyncHandler
{
    private readonly IDocumentTypePermissionRepository _repository;
    private readonly EngageReferenceMapper _referenceMapper;

    public DocumentTypePermissionHandler(
        ILogger<SyncHandlerRoot<DocumentTypePermission, DocumentTypePermission>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory,
        IDocumentTypePermissionRepository repository,
        EngageReferenceMapper referenceMapper)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _repository = repository;
        _referenceMapper = referenceMapper;
    }

    // only the document types that have a row; the rest are on Engage's all-allowed default.
    protected override IEnumerable<DocumentTypePermission> GetAllItems()
        => _repository.GetAll()
            .Where(x => _referenceMapper.GetDocumentTypeKey(x.ContentTypeId) is not null)
            .GroupBy(x => x.ContentTypeId)
            .Select(x => x.OrderBy(p => p.Id).First());
}
