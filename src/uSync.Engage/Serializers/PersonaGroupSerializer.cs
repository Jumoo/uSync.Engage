using Microsoft.Extensions.Logging;

using Umbraco.Engage.Infrastructure.Personalization.Personas;

using uSync.Core.Serialization;
using uSync.Engage.Services;

namespace uSync.Engage.Serializers;

[SyncSerializer("9EB524A8-A423-4AF7-A04C-EE11F9975E30", "Engage Persona Group Serializer", EngageConstants.ItemTypes.PersonaGroup)]
public class PersonaGroupSerializer : PersonalizationGroupSerializerBase<PersonaGroup, Persona>
{
    private readonly IPersonaGroupRepository _repository;

    public PersonaGroupSerializer(
        ILogger<SyncSerializerRoot<PersonaGroup>> logger,
        EngageReferenceMapper referenceMapper,
        IPersonaGroupRepository repository)
        : base(logger, referenceMapper)
    {
        _repository = repository;
    }

    protected override string ItemsElementName => "Personas";
    protected override string ItemElementName => "Persona";

    protected override IReadOnlyList<PersonaGroup> GetAllGroups() => _repository.GetAll();
    protected override PersonaGroup? GetGroup(Guid key) => _repository.Get(key);
    protected override void SaveGroup(PersonaGroup group) => _repository.Save(group);
    protected override void DeleteGroup(Guid key) => _repository.Delete(key);

    protected override List<Persona> GetItems(PersonaGroup group) => group.Personas;
    protected override void SetItems(PersonaGroup group, List<Persona> items) => group.Personas = items;
}
