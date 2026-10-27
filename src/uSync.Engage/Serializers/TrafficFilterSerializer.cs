using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Models;
using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Repository;
using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Service;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Engage.Serializers;

[SyncSerializer("1F935F47-EA19-48F9-A3CD-3ED0E72F698F", "Engage Traffic Filter Serializer", EngageConstants.ItemTypes.TrafficFilter)]
public class TrafficFilterSerializer : EngageSerializerBase<TrafficFilter>
{
    private readonly ITrafficFilterRepository _repository;
    private readonly ITrafficFilterService _service;

    public TrafficFilterSerializer(
        ILogger<SyncSerializerRoot<TrafficFilter>> logger,
        ITrafficFilterRepository repository,
        ITrafficFilterService service)
        : base(logger)
    {
        _repository = repository;
        _service = service;
    }

    public override Guid ItemKey(TrafficFilter item) => item.Key;

    public override string ItemAlias(TrafficFilter item) => item.Name;

    protected override Task<TrafficFilter?> FindByKeyAsync(Guid key)
        => _repository.GetByKeyAsync(key);

    protected override async Task<TrafficFilter?> FindByNameAsync(string name)
        => (await _repository.GetAllAsync()).FirstOrDefault(x => name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));

    public override Task SaveItemAsync(TrafficFilter item)
        => _service.SaveAsync(item);

    public override Task DeleteItemAsync(TrafficFilter item)
        => _service.DeleteAsync(item.Key);

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(TrafficFilter item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, item.Name);

        node.Add(new XElement("Info",
            new XElement("Name", item.Name),
            new XElement("Description", item.Description),
            new XElement("Type", item.Type),
            new XElement("Mode", item.Mode),
            new XElement("Condition", item.Condition),
            new XElement("IsActive", item.IsActive)));

        node.Add(new XElement("Value", item.Value ?? string.Empty));
        node.Add(new XElement("Values", item.Values.Select(x => new XElement("Value", x))));

        return Task.FromResult(SucceedExport(item.Name, node));
    }

    protected override async Task<SyncAttempt<TrafficFilter>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();

        var existing = await FindItemAsync(node);
        var info = node.Element("Info");

        var value = node.Element("Value").ValueOrDefault(string.Empty);

        var filter = new TrafficFilter
        {
            Id = existing?.Id ?? 0,
            Key = key,
            Name = info?.Element("Name").ValueOrDefault(alias) ?? alias,
            Description = info?.Element("Description").ValueOrDefault(string.Empty) ?? string.Empty,
            Type = info?.Element("Type").ValueOrDefault(string.Empty) ?? string.Empty,
            Mode = GetNullableEnum<TrafficFilterMode>(info?.Element("Mode")) ?? TrafficFilterMode.Block,
            Condition = info?.Element("Condition").ValueOrDefault(string.Empty) ?? string.Empty,
            IsActive = info?.Element("IsActive").ValueOrDefault(true) ?? true,
            Value = string.IsNullOrEmpty(value) ? null : value,
            Values = node.Element("Values")?.Elements("Value").Select(x => x.Value).ToList() ?? [],
            Created = existing?.Created ?? DateTime.UtcNow,
            CreatedByUmbracoUserKey = existing?.CreatedByUmbracoUserKey ?? ImportUserKey,
            CreatedByUmbracoUserName = existing?.CreatedByUmbracoUserName,
            Updated = DateTime.UtcNow,
            UpdatedByUmbracoUserKey = ImportUserKey,
        };

        // the same checks the backoffice makes, so a bad file can't put a filter in that Engage can't use.
        var validation = _service.Validate(filter);
        if (validation.Success is false)
            return SyncAttempt<TrafficFilter>.Fail(alias, filter, ChangeType.Fail, $"Traffic filter is not valid: {validation.Status}", null);

        var result = await _service.SaveAsync(filter);
        if (result.Success is false)
            return SyncAttempt<TrafficFilter>.Fail(alias, filter, ChangeType.Fail, $"Engage failed to save the traffic filter: {result.Result}", null);

        return Succeed(alias, await _repository.GetByKeyAsync(key) ?? filter, []);
    }
}
