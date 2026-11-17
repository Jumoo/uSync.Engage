using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Engage.Infrastructure.Analytics.Goals;
using Umbraco.Engage.Infrastructure.Personalization.CustomerJourney;
using Umbraco.Engage.Infrastructure.Personalization.Personas;

namespace uSync.Engage.Services;

/// <summary>
///  swaps the numeric ids Engage and Umbraco use for references with keys (and back),
///  ids are different on every site, keys are not.
/// </summary>
public class EngageReferenceMapper
{
    private readonly IPersonaGroupRepository _personaGroupRepository;
    private readonly ICustomerJourneyGroupRepository _customerJourneyGroupRepository;
    private readonly IGoalService _goalService;
    private readonly IIdKeyMap _idKeyMap;
    private readonly IEntityService _entityService;

    public EngageReferenceMapper(
        IPersonaGroupRepository personaGroupRepository,
        ICustomerJourneyGroupRepository customerJourneyGroupRepository,
        IGoalService goalService,
        IIdKeyMap idKeyMap,
        IEntityService entityService)
    {
        _personaGroupRepository = personaGroupRepository;
        _customerJourneyGroupRepository = customerJourneyGroupRepository;
        _goalService = goalService;
        _idKeyMap = idKeyMap;
        _entityService = entityService;
    }

    // personas, journey steps and goals are read from Engage's caches, which Engage resets
    // (synchronously, through the local cache refresher) every time one is saved, so items
    // imported earlier in the same run are found here.

    public Guid? GetPersonaKey(long id)
        => _personaGroupRepository.GetAll().SelectMany(x => x.Personas).FirstOrDefault(x => x.Id == id)?.Key;

    public long? GetPersonaId(Guid key)
        => _personaGroupRepository.GetAll().SelectMany(x => x.Personas).FirstOrDefault(x => x.Key == key)?.Id;

    public Guid? GetCustomerJourneyStepKey(long id)
        => _customerJourneyGroupRepository.GetAll().SelectMany(x => x.Steps).FirstOrDefault(x => x.Id == id)?.Key;

    public long? GetCustomerJourneyStepId(Guid key)
        => _customerJourneyGroupRepository.GetAll().SelectMany(x => x.Steps).FirstOrDefault(x => x.Key == key)?.Id;

    public Guid? GetGoalKey(long id)
        => _goalService.GetAllIncludingInvalid().FirstOrDefault(x => x.Id == id)?.Key;

    public long? GetGoalId(Guid key)
        => _goalService.GetAllIncludingInvalid().FirstOrDefault(x => x.Key == key)?.Id;

    public Guid? GetMediaKey(int? id)
        => id is > 0 ? AttemptToNullable(_idKeyMap.GetKeyForId(id.Value, UmbracoObjectTypes.Media)) : null;

    public int? GetMediaId(Guid? key)
        => key.HasValue ? AttemptToNullable(_idKeyMap.GetIdForKey(key.Value, UmbracoObjectTypes.Media)) : null;

    /// <summary>
    ///  the media item's path by name ("Engage/Persona/Male 3"), or null if it's missing or in the bin.
    /// </summary>
    /// <remarks>
    ///  Engage creates its persona and journey step icons on every site, with the same names in
    ///  the same folders but new keys, so the path finds them where the key can't.
    /// </remarks>
    public string? GetMediaPath(int? id)
    {
        if (id is not > 0) return null;

        var entity = _entityService.Get(id.Value, UmbracoObjectTypes.Media);
        if (entity is null || entity.Trashed) return null;

        var ids = entity.Path.Split(',')
            .Select(x => int.TryParse(x, out var i) ? i : 0)
            .Where(x => x > 0)
            .ToArray();

        var names = _entityService.GetAll(UmbracoObjectTypes.Media, ids)
            .ToDictionary(x => x.Id, x => x.Name);

        var path = ids.Select(x => names.GetValueOrDefault(x)).ToList();
        return path.Any(x => string.IsNullOrEmpty(x) || x.Contains('/')) ? null : string.Join('/', path);
    }

    /// <summary>
    ///  the id of the media item at a path from <see cref="GetMediaPath"/>, first match by sort order.
    /// </summary>
    public int? GetMediaIdByPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        int? parentId = null;
        foreach (var name in path.Split('/'))
        {
            var children = parentId is null
                ? _entityService.GetRootEntities(UmbracoObjectTypes.Media)
                : _entityService.GetChildren(parentId.Value, UmbracoObjectTypes.Media);

            var match = children
                .Where(x => !x.Trashed)
                .OrderBy(x => x.SortOrder)
                .FirstOrDefault(x => name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));

            if (match is null) return null;
            parentId = match.Id;
        }

        return parentId;
    }

    public Guid? GetDocumentTypeKey(int id)
        => AttemptToNullable(_idKeyMap.GetKeyForId(id, UmbracoObjectTypes.DocumentType));

    public int? GetDocumentTypeId(Guid key)
        => AttemptToNullable(_idKeyMap.GetIdForKey(key, UmbracoObjectTypes.DocumentType));

    private static T? AttemptToNullable<T>(Umbraco.Cms.Core.Attempt<T> attempt) where T : struct
        => attempt.Success ? attempt.Result : null;
}
