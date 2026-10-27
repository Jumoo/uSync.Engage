# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

`uSync.Engage` bridges [uSync](https://github.com/KevinJump/uSync) and Umbraco Engage. It
serializes Engage's settings (persona groups, customer journeys, goals, campaign groups,
segments, traffic filters, document type and user group permissions) to uSync files and
imports them back. It is modelled on uSync.Forms and uSync.Umbraco.Commerce.

This is `v17/main`: Umbraco 17, `net10.0`, Engage 17.5+ (traffic filters arrived in 17.2,
the pageview goal config changed in 17.4). LTS only, per the version policy.

## Commands

```bash
dotnet build src/uSync.Engage/uSync.Engage.csproj -c Release
```

```bash
dotnet pack src/uSync.Engage/uSync.Engage.csproj -c Release
```

Build and pack the **project**, as CI does. There is no client build: the localization file
under `wwwroot/App_Plugins/uSync.Engage` is static, and the manifest is built in code
(`uSyncEngageManifestReader`). There are no automated tests; behaviour is checked against
`test/uSync.Engage.Site`.

**Engage refuses to start on SQLite**, so the test site uses SQL Server LocalDB
(`uSyncEngageSite`). Create the database before the first run, Umbraco won't on LocalDB.

If the site fails at boot with error 4060 and then "Cannot create file '...mdf' because it
already exists", it's reaching a LocalDB server that doesn't have the database attached.
Force-stopping the site can leave an orphaned `sqlservr.exe` answering to the same
`MSSQLLocalDB` name as the real one. Run `sqllocaldb info MSSQLLocalDB` to get the real pipe,
then either point the connection string at `np:\\.\pipe\LOCALDB#xxxx\tsql\query` or stop
LocalDB and the stray process.

## Engage has no public CRUD API in its docs

Everything here goes through Engage's own public repositories and services
(`IPersonaGroupRepository`, `ICustomerJourneyGroupRepository`, `IGoalService`,
`ISegmentRepository`, `ICampaignGroupRepository`, `ITrafficFilterService`,
`IDocumentTypePermissionRepository`, `IUserGroupPermissionRepository`), found by
decompiling 17.5.0. They aren't documented, so check them again on every Engage upgrade.
Things learned doing that:

- **Children are matched by numeric id, not key.** Saving a persona group, journey, segment
  or campaign group removes any child whose id isn't in the list (often with its scoring
  data). Serializers always copy this site's ids onto the children before saving.
- **Keys are unique and soft deletes keep them.** Inserting with a key that a deleted row
  still holds throws. Campaign groups are never hard deleted, so
  `CampaignGroupSerializer.RestoreDeletedGroup` un-deletes the row first. Goals are only
  ever marked invalid, and `GetById` still returns them, so an import revives them.
- **Repositories don't write new ids back.** Re-read by key after a save. Engage resets its
  caches through the local cache refresher, which Umbraco delivers synchronously.
- **Goal scores are upserted and never deleted.** A score that's gone from the file is set
  to 0 / unlocked, which is Engage's own 'removed'.
- **References are numeric ids** in goal and campaign scoring, segment rule config JSON
  (`Persona`, `CustomerJourney`, `Goal` rules), persona/step icons (media, when >= 1000) and
  document type permissions. `EngageReferenceMapper` swaps them for keys. Pageview goals and
  visited-pages rules already use content keys.
- **Engage writes on its own SQL connection**, not in Umbraco's scope.

## Export on save

Engage raises its own events (`Umbraco.Engage.Infrastructure.Events`), not Umbraco
notifications, and most carry no payload. `EngageEventBridge` registers with
`SystemEventService` once the site has started, and on each event the matching handler
re-exports all of its items (there are only ever a few) and writes delete markers for
anything missing since its last snapshot (`EngageSyncHandlerBase.EngageItemsChangedAsync`).

`SystemEventService.Register` replays the last event of each type raised since startup,
so the bridge ignores events while it registers. Permissions raise no events at all.

## Import order

Priorities are in `EngageConstants.Priorities`, in a band at `USYNC_RESERVED_UPPER + 100`,
after all core handlers and uSync.Forms. Personas and journeys come first because goals,
campaign groups and segments refer to them.

## Not synced (yet)

A/B tests, applied personalizations and content scoring: their content variants are named
after Engage's numeric ids (`engage_personalization_{id}`), which differ per site.
Referral groups point at analytics data.

## Lock files

Regenerate `packages.lock.json` with `--source https://api.nuget.org/v3/index.json`. A local
feed with locally built packages at the same version gives hashes that fail CI's
`--locked-mode` restore (NU1403).
