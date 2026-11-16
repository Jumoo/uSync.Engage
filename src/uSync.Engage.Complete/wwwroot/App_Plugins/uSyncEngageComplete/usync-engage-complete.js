// src/menus/manifest.ts
var e = [
	"engage-persona-group",
	"engage-customer-journey-group",
	"engage-goal",
	"engage-campaign-group",
	"engage-segment",
	"engage-traffic-filter"
], t = [{
	type: "entityAction",
	kind: "publisher-push",
	alias: "usync.engage.push.action",
	name: "Push Engage item",
	forEntityTypes: e
}, {
	type: "entityAction",
	kind: "publisher-pull",
	alias: "usync.engage.pull.action",
	name: "Pull Engage item",
	forEntityTypes: e
}];
export { t as manifests };

//# sourceMappingURL=usync-engage-complete.js.map