// the entity types Engage's own backoffice gives its items. Push and pull appear in the
// actions menu next to Engage's edit and delete. The item managers in uSync.Engage
// (src/uSync.Engage/Sync) turn these into uSync items.
//
// document type and user group permissions have no actions menu in Engage, so there's
// nowhere to push them from.
const engageEntityTypes = [
  "engage-persona-group",
  "engage-customer-journey-group",
  "engage-goal",
  "engage-campaign-group",
  "engage-segment",
  "engage-traffic-filter",
];

const actions: Array<UmbExtensionManifest> = [
  {
    type: "entityAction",
    kind: "publisher-push",
    alias: "usync.engage.push.action",
    name: "Push Engage item",
    forEntityTypes: engageEntityTypes,
  },
  {
    type: "entityAction",
    kind: "publisher-pull",
    alias: "usync.engage.pull.action",
    name: "Pull Engage item",
    forEntityTypes: engageEntityTypes,
  },
];

export const manifests: Array<UmbExtensionManifest> = [...actions];
