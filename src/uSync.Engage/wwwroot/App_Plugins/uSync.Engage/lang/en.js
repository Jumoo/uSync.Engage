// names shown for Engage items in the uSync (and uSync.Publisher) reports,
// keyed by the handler's item type (the Engage class name).
const names = {
  PersonaGroup: "Engage Persona Group",
  CustomerJourneyGroup: "Engage Customer Journey",
  IGoal: "Engage Goal",
  CampaignGroup: "Engage Campaign Group",
  Segment: "Engage Segment",
  TrafficFilter: "Engage Traffic Filter",
  DocumentTypePermission: "Engage Document Type Permission",
  UserGroupPermission: "Engage User Group Permission",
};

export default {
  uSync: {
    ...names,
    // title of the Engage box on the uSync dashboard (uSync_group{handler group}).
    groupEngage: "Engage",
  },
  usyncpublish: names,
  // the handler labels uSync's progress view shows while it runs, keyed by the
  // camelCased handler name (treeHeaders_{name}). Without these it shows the raw
  // camelCased names (customerJourneys, docPerms, ...).
  treeHeaders: {
    personas: "Personas",
    customerJourneys: "Customer Journeys",
    goals: "Goals",
    campaignGroups: "Campaign Groups",
    segments: "Segments",
    trafficFilters: "Traffic Filters",
    docPerms: "Doc Perms",
    userPerms: "User Perms",
  },
};
