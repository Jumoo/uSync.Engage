using uSync.BackOffice;

namespace uSync.Engage;

public static class EngageConstants
{
    public const string Group = "Engage";

    /// <summary>
    ///  handler priorities.
    /// </summary>
    /// <remarks>
    ///  Engage settings point at document types, media and content, so they import after
    ///  all of the core handlers, in their own band above Forms (USYNC_RESERVED_UPPER + 1..10).
    ///  the order encodes the references between them: segment rules and goal scoring point
    ///  at personas and journey steps, so those come first.
    /// </remarks>
    public static class Priorities
    {
        private const int Base = uSyncConstants.Priorites.USYNC_RESERVED_UPPER + 100;

        public const int PersonaGroups = Base + 10;
        public const int CustomerJourneyGroups = Base + 20;
        public const int Goals = Base + 30;
        public const int CampaignGroups = Base + 40;
        public const int Segments = Base + 50;
        public const int TrafficFilters = Base + 60;
        public const int DocumentTypePermissions = Base + 70;
        public const int UserGroupPermissions = Base + 75;
    }

    public static class Folders
    {
        public const string PersonaGroups = "Engage\\PersonaGroups";
        public const string CustomerJourneyGroups = "Engage\\CustomerJourneys";
        public const string Goals = "Engage\\Goals";
        public const string CampaignGroups = "Engage\\CampaignGroups";
        public const string Segments = "Engage\\Segments";
        public const string TrafficFilters = "Engage\\TrafficFilters";
        public const string DocumentTypePermissions = "Engage\\DocumentTypePermissions";
        public const string UserGroupPermissions = "Engage\\UserGroupPermissions";
    }

    /// <summary>
    ///  the root element name of each item type in the uSync files.
    /// </summary>
    public static class ItemTypes
    {
        public const string PersonaGroup = "EngagePersonaGroup";
        public const string CustomerJourneyGroup = "EngageCustomerJourneyGroup";
        public const string Goal = "EngageGoal";
        public const string CampaignGroup = "EngageCampaignGroup";
        public const string Segment = "EngageSegment";
        public const string TrafficFilter = "EngageTrafficFilter";
        public const string DocumentTypePermission = "EngageDocumentTypePermission";
        public const string UserGroupPermission = "EngageUserGroupPermission";
    }

    public static class UdiEntityType
    {
        public const string PersonaGroup = "engage-persona-group";
        public const string CustomerJourneyGroup = "engage-journey-group";
        public const string Goal = "engage-goal";
        public const string CampaignGroup = "engage-campaign-group";
        public const string Segment = "engage-segment";
        public const string TrafficFilter = "engage-traffic-filter";
        public const string DocumentTypePermission = "engage-doctype-permission";
        public const string UserGroupPermission = "engage-usergroup-permission";

        public static readonly string[] All = [
            PersonaGroup, CustomerJourneyGroup, Goal, CampaignGroup,
            Segment, TrafficFilter, DocumentTypePermission, UserGroupPermission];
    }
}
