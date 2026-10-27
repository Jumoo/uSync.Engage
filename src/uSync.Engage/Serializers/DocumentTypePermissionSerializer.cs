using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Services;
using Umbraco.Engage.Infrastructure.Permissions.DocumentTypePermissions;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Engage.Services;

namespace uSync.Engage.Serializers;

/// <remarks>
///  a permission belongs to a document type, and Engage's own key for the row is different on
///  every site, so the file uses the document type's key and alias.
///
///  there is no delete in Engage: a document type without a row gets everything allowed,
///  so a delete puts all the permissions back on.
/// </remarks>
[SyncSerializer("D46C560A-E03F-4BDB-B907-6592490F7DA1", "Engage Document Type Permission Serializer", EngageConstants.ItemTypes.DocumentTypePermission)]
public class DocumentTypePermissionSerializer : EngageSerializerBase<DocumentTypePermission>
{
    private readonly IDocumentTypePermissionRepository _repository;
    private readonly IContentTypeService _contentTypeService;
    private readonly EngageReferenceMapper _referenceMapper;

    public DocumentTypePermissionSerializer(
        ILogger<SyncSerializerRoot<DocumentTypePermission>> logger,
        IDocumentTypePermissionRepository repository,
        IContentTypeService contentTypeService,
        EngageReferenceMapper referenceMapper)
        : base(logger)
    {
        _repository = repository;
        _contentTypeService = contentTypeService;
        _referenceMapper = referenceMapper;
    }

    public override Guid ItemKey(DocumentTypePermission item)
        => _referenceMapper.GetDocumentTypeKey(item.ContentTypeId) ?? Guid.Empty;

    public override string ItemAlias(DocumentTypePermission item)
        => _contentTypeService.Get(item.ContentTypeId)?.Alias ?? item.ContentTypeId.ToString();

    protected override Task<DocumentTypePermission?> FindByKeyAsync(Guid key)
    {
        var id = _referenceMapper.GetDocumentTypeId(key);
        return Task.FromResult(id is null ? null : _repository.GetPermissionForDocumentType(id.Value));
    }

    protected override Task<DocumentTypePermission?> FindByNameAsync(string name)
    {
        var contentType = _contentTypeService.Get(name);
        return Task.FromResult(contentType is null ? null : _repository.GetPermissionForDocumentType(contentType.Id));
    }

    public override Task SaveItemAsync(DocumentTypePermission item)
    {
        _repository.Save(item);
        return Task.CompletedTask;
    }

    public override Task DeleteItemAsync(DocumentTypePermission item)
    {
        item.ShowAnalytics = true;
        item.AllowAbTesting = true;
        item.AllowScorePersonalization = true;
        item.AllowApplyPersonalization = true;
        item.Updated = DateTime.UtcNow;
        item.UpdatedByUmbracoUserKey = ImportUserKey;
        _repository.Save(item);
        return Task.CompletedTask;
    }

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(DocumentTypePermission item, SyncSerializerOptions options)
    {
        var alias = ItemAlias(item);
        var node = InitializeBaseNode(item, alias);

        node.Add(new XElement("Permissions",
            new XElement("ShowAnalytics", item.ShowAnalytics),
            new XElement("AllowAbTesting", item.AllowAbTesting),
            new XElement("AllowScorePersonalization", item.AllowScorePersonalization),
            new XElement("AllowApplyPersonalization", item.AllowApplyPersonalization)));

        return Task.FromResult(SucceedExport(alias, node));
    }

    protected override Task<SyncAttempt<DocumentTypePermission>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var alias = node.GetAlias();

        var contentTypeId = _referenceMapper.GetDocumentTypeId(node.GetKey()) ?? _contentTypeService.Get(alias)?.Id;
        if (contentTypeId is null)
            return Task.FromResult(SyncAttempt<DocumentTypePermission>.Fail(alias, ChangeType.Fail, $"Document type {alias} not found"));

        var existing = _repository.GetPermissionForDocumentType(contentTypeId.Value);
        var permissions = node.Element("Permissions");

        var permission = new DocumentTypePermission
        {
            Id = existing?.Id ?? 0,
            Key = existing?.Key ?? Guid.NewGuid(),
            ContentTypeId = contentTypeId.Value,
            ShowAnalytics = permissions?.Element("ShowAnalytics").ValueOrDefault(true) ?? true,
            AllowAbTesting = permissions?.Element("AllowAbTesting").ValueOrDefault(true) ?? true,
            AllowScorePersonalization = permissions?.Element("AllowScorePersonalization").ValueOrDefault(true) ?? true,
            AllowApplyPersonalization = permissions?.Element("AllowApplyPersonalization").ValueOrDefault(true) ?? true,
            Updated = DateTime.UtcNow,
            UpdatedByUmbracoUserKey = ImportUserKey,
        };

        _repository.Save(permission);

        return Task.FromResult(Succeed(alias, _repository.GetPermissionForDocumentType(contentTypeId.Value) ?? permission, []));
    }
}
