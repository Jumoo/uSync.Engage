using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Engage.Serializers;

/// <summary>
///  base for the Engage serializers.
/// </summary>
/// <remarks>
///  items are matched on key first then name (the base FindItemAsync(XElement)), so things
///  Engage seeds on install, with a different key on every site, still line up by name.
///
///  nothing site specific (numeric ids, created/updated dates, user keys) goes in the xml.
///  uSync compares files by hash, so anything that differs between sites would show as a change.
///
///  Engage writes to its own database connection, not inside an Umbraco scope, so a failed
///  uSync import can't roll back what Engage has already saved.
/// </remarks>
public abstract class EngageSerializerBase<TObject> : SyncSerializerRoot<TObject>, ISyncSerializer<TObject>
    where TObject : class
{
    protected EngageSerializerBase(ILogger<SyncSerializerRoot<TObject>> logger)
        : base(logger)
    { }

    protected abstract Task<TObject?> FindByKeyAsync(Guid key);

    protected abstract Task<TObject?> FindByNameAsync(string name);

    public override Task<TObject?> FindItemAsync(Guid key)
        => key == Guid.Empty ? Task.FromResult<TObject?>(null) : FindByKeyAsync(key);

    public override Task<TObject?> FindItemAsync(string alias)
        => string.IsNullOrWhiteSpace(alias) ? Task.FromResult<TObject?>(null) : FindByNameAsync(alias);

    protected static SyncAttempt<TObject> Succeed(string name, TObject item, IList<uSyncChange> changes)
        => SyncAttempt<TObject>.Succeed(name, item, ChangeType.Import, string.Empty, true, changes);

    protected static SyncAttempt<XElement> SucceedExport(string name, XElement node)
        => SyncAttempt<XElement>.Succeed(name, node, ChangeType.Export, Array.Empty<uSyncChange>());

    /// <summary>
    ///  stamp for the created/updated user columns, Engage's own default when no user is known.
    /// </summary>
    protected static Guid ImportUserKey => Umbraco.Cms.Core.Constants.Security.SuperUserKey;

    protected static int? GetNullableInt(XElement? element)
        => int.TryParse(element?.Value, out var value) ? value : null;

    protected static TEnum? GetNullableEnum<TEnum>(XElement? element) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(element?.Value, true, out var value) ? value : null;
}
