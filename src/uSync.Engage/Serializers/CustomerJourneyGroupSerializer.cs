using Microsoft.Extensions.Logging;

using Umbraco.Engage.Infrastructure.Personalization.CustomerJourney;

using uSync.Core.Serialization;
using uSync.Engage.Services;

namespace uSync.Engage.Serializers;

[SyncSerializer("42DF7366-B08A-4350-A1A7-C2354B5E5C71", "Engage Customer Journey Serializer", EngageConstants.ItemTypes.CustomerJourneyGroup)]
public class CustomerJourneyGroupSerializer : PersonalizationGroupSerializerBase<CustomerJourneyGroup, CustomerJourneyStep>
{
    private readonly ICustomerJourneyGroupRepository _repository;

    public CustomerJourneyGroupSerializer(
        ILogger<SyncSerializerRoot<CustomerJourneyGroup>> logger,
        EngageReferenceMapper referenceMapper,
        ICustomerJourneyGroupRepository repository)
        : base(logger, referenceMapper)
    {
        _repository = repository;
    }

    protected override string ItemsElementName => "Steps";
    protected override string ItemElementName => "Step";

    protected override IReadOnlyList<CustomerJourneyGroup> GetAllGroups() => _repository.GetAll();
    protected override CustomerJourneyGroup? GetGroup(Guid key) => _repository.Get(key);
    protected override void SaveGroup(CustomerJourneyGroup group) => _repository.Save(group);
    protected override void DeleteGroup(Guid key) => _repository.Delete(key);

    protected override List<CustomerJourneyStep> GetItems(CustomerJourneyGroup group) => group.Steps;
    protected override void SetItems(CustomerJourneyGroup group, List<CustomerJourneyStep> items) => group.Steps = items;
}
