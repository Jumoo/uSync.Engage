using Umbraco.Engage.Infrastructure.Personalization.Personas;
using Umbraco.Engage.Infrastructure.Personalization.CustomerJourney;
using Umbraco.Engage.Infrastructure.Analytics.Goals;
using Umbraco.Engage.Infrastructure.Personalization.Campaign;
using Umbraco.Engage.Infrastructure.Personalization.Segments;
using Umbraco.Engage.Infrastructure.Analytics.TrafficFilters.Models;
using Umbraco.Engage.Infrastructure.Permissions.DocumentTypePermissions;
using Umbraco.Engage.Infrastructure.Permissions.UserGroupPermissions;

using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Engage.Trackers;

public class PersonaGroupTracker : SyncXmlTrackAndMerger<PersonaGroup>, ISyncTracker<PersonaGroup>
{
    public PersonaGroupTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}

public class CustomerJourneyGroupTracker : SyncXmlTrackAndMerger<CustomerJourneyGroup>, ISyncTracker<CustomerJourneyGroup>
{
    public CustomerJourneyGroupTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}

public class GoalTracker : SyncXmlTrackAndMerger<IGoal>, ISyncTracker<IGoal>
{
    public GoalTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}

public class CampaignGroupTracker : SyncXmlTrackAndMerger<CampaignGroup>, ISyncTracker<CampaignGroup>
{
    public CampaignGroupTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}

public class SegmentTracker : SyncXmlTrackAndMerger<Segment>, ISyncTracker<Segment>
{
    public SegmentTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}

public class TrafficFilterTracker : SyncXmlTrackAndMerger<TrafficFilter>, ISyncTracker<TrafficFilter>
{
    public TrafficFilterTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}

public class DocumentTypePermissionTracker : SyncXmlTrackAndMerger<DocumentTypePermission>, ISyncTracker<DocumentTypePermission>
{
    public DocumentTypePermissionTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}

public class UserGroupPermissionTracker : SyncXmlTrackAndMerger<UserGroupPermission>, ISyncTracker<UserGroupPermission>
{
    public UserGroupPermissionTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }
}
