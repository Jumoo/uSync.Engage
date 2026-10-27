using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Services;
using Umbraco.Engage.Infrastructure.Permissions.UserGroupPermissions;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Engage.Serializers;

/// <remarks>
///  a permission belongs to a user group, so the file uses the group's key and alias.
///
///  there is no delete in Engage: a group without a row gets access to everything,
///  so a delete puts all the access back on.
/// </remarks>
[SyncSerializer("5D47EDD1-C342-4C19-8165-7AE983D34BC1", "Engage User Group Permission Serializer", EngageConstants.ItemTypes.UserGroupPermission)]
public class UserGroupPermissionSerializer : EngageSerializerBase<UserGroupPermission>
{
    private readonly IUserGroupPermissionRepository _repository;
    private readonly IUserGroupService _userGroupService;

    public UserGroupPermissionSerializer(
        ILogger<SyncSerializerRoot<UserGroupPermission>> logger,
        IUserGroupPermissionRepository repository,
        IUserGroupService userGroupService)
        : base(logger)
    {
        _repository = repository;
        _userGroupService = userGroupService;
    }

    public override Guid ItemKey(UserGroupPermission item) => item.UserGroupKey;

    public override string ItemAlias(UserGroupPermission item)
        => _userGroupService.GetAsync(item.UserGroupKey).GetAwaiter().GetResult()?.Alias
            ?? item.UserGroupKey.ToString();

    protected override Task<UserGroupPermission?> FindByKeyAsync(Guid key)
        => Task.FromResult(_repository.GetPermissionForUserGroup(key));

    protected override async Task<UserGroupPermission?> FindByNameAsync(string name)
    {
        var group = await _userGroupService.GetAsync(name);
        return group is null ? null : _repository.GetPermissionForUserGroup(group.Key);
    }

    // the single item save is an upsert on the user group key.
    public override Task SaveItemAsync(UserGroupPermission item)
    {
        _repository.Save(item);
        return Task.CompletedTask;
    }

    public override Task DeleteItemAsync(UserGroupPermission item)
    {
        item.AccessToAnalytics = true;
        item.AccessToAbTesting = true;
        item.AccessToPersonalization = true;
        item.AccessToSettings = true;
        item.AccessToProfiles = true;
        item.AccessToReporting = true;
        item.Updated = DateTime.UtcNow;
        item.UpdatedByUmbracoUserKey = ImportUserKey;
        _repository.Save(item);
        return Task.CompletedTask;
    }

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(UserGroupPermission item, SyncSerializerOptions options)
    {
        var alias = ItemAlias(item);
        var node = InitializeBaseNode(item, alias);

        node.Add(new XElement("Permissions",
            new XElement("AccessToAnalytics", item.AccessToAnalytics),
            new XElement("AccessToAbTesting", item.AccessToAbTesting),
            new XElement("AccessToPersonalization", item.AccessToPersonalization),
            new XElement("AccessToSettings", item.AccessToSettings),
            new XElement("AccessToProfiles", item.AccessToProfiles),
            new XElement("AccessToReporting", item.AccessToReporting)));

        return Task.FromResult(SucceedExport(alias, node));
    }

    protected override async Task<SyncAttempt<UserGroupPermission>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var alias = node.GetAlias();

        var group = await _userGroupService.GetAsync(node.GetKey()) ?? await _userGroupService.GetAsync(alias);
        if (group is null)
            return SyncAttempt<UserGroupPermission>.Fail(alias, ChangeType.Fail, $"User group {alias} not found");

        var existing = _repository.GetPermissionForUserGroup(group.Key);
        var permissions = node.Element("Permissions");

        var permission = new UserGroupPermission
        {
            Id = existing?.Id ?? 0,
            Key = existing?.Key ?? Guid.NewGuid(),
            UserGroupKey = group.Key,
            AccessToAnalytics = permissions?.Element("AccessToAnalytics").ValueOrDefault(true) ?? true,
            AccessToAbTesting = permissions?.Element("AccessToAbTesting").ValueOrDefault(true) ?? true,
            AccessToPersonalization = permissions?.Element("AccessToPersonalization").ValueOrDefault(true) ?? true,
            AccessToSettings = permissions?.Element("AccessToSettings").ValueOrDefault(true) ?? true,
            AccessToProfiles = permissions?.Element("AccessToProfiles").ValueOrDefault(true) ?? true,
            AccessToReporting = permissions?.Element("AccessToReporting").ValueOrDefault(true) ?? true,
            Updated = DateTime.UtcNow,
            UpdatedByUmbracoUserKey = ImportUserKey,
        };

        _repository.Save(permission);

        return Succeed(alias, _repository.GetPermissionForUserGroup(group.Key) ?? permission, []);
    }
}
