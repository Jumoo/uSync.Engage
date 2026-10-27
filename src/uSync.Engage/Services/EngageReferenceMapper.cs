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

    public EngageReferenceMapper(
        IPersonaGroupRepository personaGroupRepository,
        ICustomerJourneyGroupRepository customerJourneyGroupRepository,
        IGoalService goalService,
        IIdKeyMap idKeyMap)
    {
        _personaGroupRepository = personaGroupRepository;
        _customerJourneyGroupRepository = customerJourneyGroupRepository;
        _goalService = goalService;
        _idKeyMap = idKeyMap;
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

    public Guid? GetDocumentTypeKey(int id)
        => AttemptToNullable(_idKeyMap.GetKeyForId(id, UmbracoObjectTypes.DocumentType));

    public int? GetDocumentTypeId(Guid key)
        => AttemptToNullable(_idKeyMap.GetIdForKey(key, UmbracoObjectTypes.DocumentType));

    private static T? AttemptToNullable<T>(Umbraco.Cms.Core.Attempt<T> attempt) where T : struct
        => attempt.Success ? attempt.Result : null;
}
