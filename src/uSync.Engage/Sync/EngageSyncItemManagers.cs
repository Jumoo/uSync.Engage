using Umbraco.Engage.Infrastructure.Analytics.Goals;
using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Models;
using Umbraco.Engage.Infrastructure.Personalization.Campaign;
using Umbraco.Engage.Infrastructure.Personalization.CustomerJourney;
using Umbraco.Engage.Infrastructure.Personalization.Personas;
using Umbraco.Engage.Infrastructure.Personalization.Segments;

using uSync.Core;
using uSync.Core.Sync;

namespace uSync.Engage.Sync;

// document type and user group permissions have no menu of their own in Engage's
// backoffice, so there is nothing to push them from.

[SyncItemManager(EngageConstants.UdiEntityType.PersonaGroup)]
public class PersonaGroupSyncItemManager : EngageSyncItemManagerBase<PersonaGroup>
{
    public PersonaGroupSyncItemManager(ISyncItemFactory itemFactory)
        : base(itemFactory)
    { }

    protected override string Icon => "icon-users";
}

[SyncItemManager(EngageConstants.UdiEntityType.CustomerJourneyGroup)]
public class CustomerJourneyGroupSyncItemManager : EngageSyncItemManagerBase<CustomerJourneyGroup>
{
    public CustomerJourneyGroupSyncItemManager(ISyncItemFactory itemFactory)
        : base(itemFactory)
    { }

    // ours first: the base class builds the item's udi from the first one.
    public override string[] EntityTypes => [
        EngageConstants.UdiEntityType.CustomerJourneyGroup,
        EngageConstants.ClientEntityType.CustomerJourneyGroup];

    protected override string Icon => "icon-footprints";
}

[SyncItemManager(EngageConstants.UdiEntityType.Goal)]
public class GoalSyncItemManager : EngageSyncItemManagerBase<IGoal>
{
    public GoalSyncItemManager(ISyncItemFactory itemFactory)
        : base(itemFactory)
    { }

    protected override string Icon => "icon-target";
}

[SyncItemManager(EngageConstants.UdiEntityType.CampaignGroup)]
public class CampaignGroupSyncItemManager : EngageSyncItemManagerBase<CampaignGroup>
{
    public CampaignGroupSyncItemManager(ISyncItemFactory itemFactory)
        : base(itemFactory)
    { }

    protected override string Icon => "icon-megaphone";
}

[SyncItemManager(EngageConstants.UdiEntityType.Segment)]
public class SegmentSyncItemManager : EngageSyncItemManagerBase<Segment>
{
    public SegmentSyncItemManager(ISyncItemFactory itemFactory)
        : base(itemFactory)
    { }

    protected override string Icon => "icon-filter";
}

[SyncItemManager(EngageConstants.UdiEntityType.TrafficFilter)]
public class TrafficFilterSyncItemManager : EngageSyncItemManagerBase<TrafficFilter>
{
    public TrafficFilterSyncItemManager(ISyncItemFactory itemFactory)
        : base(itemFactory)
    { }

    protected override string Icon => "icon-block";
}
