using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Engage.Infrastructure.Personalization.Segments;
using Umbraco.Engage.Infrastructure.Personalization.Segments.Rules;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Engage.Services;

namespace uSync.Engage.Serializers;

/// <remarks>
///  each rule's config is the JSON the backoffice sent Engage. the persona, journey step and
///  goal rules hold numeric ids, which are swapped for keys in the file. every other rule type
///  (visited pages already uses content keys) is written as it is.
/// </remarks>
[SyncSerializer("97A45F45-A357-49F2-A30B-DAF86E63F6B3", "Engage Segment Serializer", EngageConstants.ItemTypes.Segment)]
public class SegmentSerializer : EngageSerializerBase<Segment>
{
    private readonly ISegmentRepository _repository;
    private readonly IEnumerable<ISegmentRuleFactory> _ruleFactories;
    private readonly EngageReferenceMapper _referenceMapper;

    public SegmentSerializer(
        ILogger<SyncSerializerRoot<Segment>> logger,
        ISegmentRepository repository,
        IEnumerable<ISegmentRuleFactory> ruleFactories,
        EngageReferenceMapper referenceMapper)
        : base(logger)
    {
        _repository = repository;
        _ruleFactories = ruleFactories;
        _referenceMapper = referenceMapper;
    }

    public override Guid ItemKey(Segment item) => item.Key;

    public override string ItemAlias(Segment item) => item.Name;

    protected override Task<Segment?> FindByKeyAsync(Guid key)
        => Task.FromResult(_repository.GetById(key));

    protected override Task<Segment?> FindByNameAsync(string name)
        => Task.FromResult(_repository.GetAll().FirstOrDefault(x => name.Equals(x.Name, StringComparison.OrdinalIgnoreCase)));

    public override Task SaveItemAsync(Segment item)
    {
        _repository.Save(item);
        return Task.CompletedTask;
    }

    public override Task DeleteItemAsync(Segment item)
    {
        _repository.Delete(item.Key);
        return Task.CompletedTask;
    }

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(Segment item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, item.Name);

        node.Add(new XElement("Info",
            new XElement("Name", item.Name),
            new XElement("Description", item.Description ?? string.Empty),
            new XElement("IsTemporary", item.IsTemporary),
            new XElement("EndTime", item.IsTemporary ? item.EndTime : null),
            new XElement("SortOrder", item.SortOrder),
            new XElement("ControlGroupSize", item.ControlGroupSize)));

        var rules = new XElement("Rules");
        foreach (var rule in item.Rules.OrderBy(x => x.Key))
        {
            rules.Add(new XElement("Rule",
                new XAttribute("Key", rule.Key),
                new XAttribute("Type", rule.Type),
                new XAttribute("IsNegation", rule.IsNegation),
                new XCData(ConvertConfig(rule.Type, rule.Config, toKeys: true, [], rule.Key))));
        }
        node.Add(rules);

        return Task.FromResult(SucceedExport(item.Name, node));
    }

    protected override async Task<SyncAttempt<Segment>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();
        var changes = new List<uSyncChange>();

        var existing = await FindItemAsync(node);
        var segmentId = existing?.Id ?? 0;
        var info = node.Element("Info");

        var rules = new List<ISegmentRule>();
        foreach (var element in node.Element("Rules")?.Elements("Rule") ?? [])
        {
            var ruleKey = element.Attribute("Key").ValueOrDefault(Guid.Empty);
            var type = element.Attribute("Type").ValueOrDefault(string.Empty);

            var factory = _ruleFactories.FirstOrDefault(x => type.Equals(x.RuleType, StringComparison.OrdinalIgnoreCase));
            if (factory is null)
            {
                changes.Add(uSyncChange.Warning($"Rules/{type}", ruleKey.ToString(), $"There is no '{type}' segment rule on this site, rule not imported"));
                continue;
            }

            var existingRule = existing?.Rules.FirstOrDefault(x => x.Key == ruleKey);
            var config = ConvertConfig(type, element.Value, toKeys: false, changes, ruleKey);

            rules.Add(factory.CreateRule(
                config,
                element.Attribute("IsNegation").ValueOrDefault(false),
                existingRule?.Id ?? 0,
                ruleKey == Guid.Empty ? Guid.NewGuid() : ruleKey,
                segmentId,
                existingRule?.Created ?? DateTime.UtcNow,
                existingRule is null ? null : DateTime.UtcNow));
        }

        var isTemporary = info?.Element("IsTemporary").ValueOrDefault(false) ?? false;
        var sortOrder = info?.Element("SortOrder").ValueOrDefault(existing?.SortOrder ?? 0) ?? 0;

        var segment = new Segment(
            segmentId,
            key,
            info?.Element("Name").ValueOrDefault(alias) ?? alias,
            info?.Element("Description").ValueOrDefault(string.Empty) ?? string.Empty,
            existing?.Created ?? DateTime.UtcNow,
            existing?.CreatedByUmbracoUserKey ?? ImportUserKey,
            DateTime.UtcNow,
            ImportUserKey,
            isTemporary ? info?.Element("EndTime").ValueOrDefault<DateTime?>(null) : null,
            isTemporary,
            sortOrder,
            decimal.TryParse(info?.Element("ControlGroupSize")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var size) ? size : 0,
            rules);

        _repository.Save(segment);

        // a new segment always goes to the bottom of the list, put it where the file says.
        if (segmentId == 0 && segment.Id > 0 && segment.SortOrder != sortOrder)
            _repository.UpdatePriority(segment.Id, sortOrder);

        return Succeed(alias, _repository.GetById(key) ?? segment, changes);
    }

    /// <summary>
    ///  rule types with references, and the property in their config that holds them.
    /// </summary>
    private enum ReferenceType { Persona, CustomerJourneyStep, Goal }

    private static readonly Dictionary<string, (string Property, ReferenceType Type)> _referenceRules
        = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Persona", ("Personas", ReferenceType.Persona) },
            { "CustomerJourney", ("CustomerJourneySteps", ReferenceType.CustomerJourneyStep) },
            { "Goal", ("Goals", ReferenceType.Goal) },
        };

    /// <summary>
    ///  swap the ids in a rule's config for keys (export), or keys for ids (import).
    /// </summary>
    /// <remarks>
    ///  only the reference array is rewritten, the rest of the config is left as it is.
    ///  on import anything that can't be found on this site is dropped from the rule, with a warning.
    /// </remarks>
    private string ConvertConfig(string type, string config, bool toKeys, List<uSyncChange> changes, Guid ruleKey)
    {
        if (!_referenceRules.TryGetValue(type, out var reference) || string.IsNullOrWhiteSpace(config))
            return config;

        JsonObject? json;
        try
        {
            json = JsonNode.Parse(config) as JsonObject;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to read the config for {type} segment rule {key}", type, ruleKey);
            return config;
        }

        if (json is null) return config;

        // the backoffice sends whatever casing it likes, Engage reads it case-insensitively,
        // and a config saved in the backoffice can keep an older casing alongside the new one.
        var properties = json
            .Where(x => x.Key.Equals(reference.Property, StringComparison.OrdinalIgnoreCase) && x.Value is JsonArray)
            .Select(x => x.Key)
            .ToList();
        if (properties.Count == 0) return config;

        foreach (var property in properties)
            json[property] = ConvertValues(type, (JsonArray)json[property]!, reference.Type, toKeys, changes, ruleKey);

        return json.ToJsonString();
    }

    private JsonArray ConvertValues(string type, JsonArray values, ReferenceType referenceType, bool toKeys, List<uSyncChange> changes, Guid ruleKey)
    {
        var converted = new JsonArray();
        foreach (var value in values)
        {
            if (value is null) continue;

            if (toKeys)
            {
                if (long.TryParse(value.ToString(), out var id) && GetKey(referenceType, id) is Guid itemKey)
                    converted.Add(itemKey.ToString());
                else
                    converted.Add(value.DeepClone());
            }
            else
            {
                if (Guid.TryParse(value.ToString(), out var itemKey) && GetId(referenceType, itemKey) is long id)
                    converted.Add(id);
                else
                    changes.Add(uSyncChange.Warning($"Rules/{type}", ruleKey.ToString(), $"{referenceType} {value} not found on this site, removed from the rule"));
            }
        }

        return converted;
    }

    private Guid? GetKey(ReferenceType type, long id) => type switch
    {
        ReferenceType.Persona => _referenceMapper.GetPersonaKey(id),
        ReferenceType.CustomerJourneyStep => _referenceMapper.GetCustomerJourneyStepKey(id),
        ReferenceType.Goal => _referenceMapper.GetGoalKey(id),
        _ => null
    };

    private long? GetId(ReferenceType type, Guid key) => type switch
    {
        ReferenceType.Persona => _referenceMapper.GetPersonaId(key),
        ReferenceType.CustomerJourneyStep => _referenceMapper.GetCustomerJourneyStepId(key),
        ReferenceType.Goal => _referenceMapper.GetGoalId(key),
        _ => null
    };
}
