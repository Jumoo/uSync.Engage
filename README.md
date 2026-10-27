# uSync.Engage

uSync for [Umbraco Engage](https://umbraco.com/products/add-ons/engage/). Engage keeps its
settings in its own database tables, so they don't move between sites with the rest of your
Umbraco settings. uSync.Engage writes them to disk as uSync files, and imports them on
another site.

## What is synced

| Engage setting | uSync folder |
|---|---|
| Persona groups and their personas | `Engage/PersonaGroups` |
| Customer journeys and their steps | `Engage/CustomerJourneys` |
| Goals, with their persona and journey scoring | `Engage/Goals` |
| Campaign groups, their campaigns and scoring | `Engage/CampaignGroups` |
| Segments and their rules | `Engage/Segments` |
| Traffic filters | `Engage/TrafficFilters` |
| Document type permissions | `Engage/DocumentTypePermissions` |
| User group permissions | `Engage/UserGroupPermissions` |

Engage refers to personas, journey steps, goals, media and document types by numeric id,
and those ids are different on every site. uSync.Engage writes keys in their place, and
looks up this site's ids when it imports. The default customer journey Engage installs on
every site is matched by name.

### What isn't synced

- Analytics, visitor profiles, reporting data and anything else Engage collects.
- A/B tests, applied personalizations and content scoring. These are tied to content
  variants that Engage names after its own numeric ids, so they need more than a settings
  sync. They may follow in a later release.
- Referral groups, which point at pages from Engage's analytics data.
- Engage's `appsettings` configuration, which is already in your files.

## Export on save

Engage tells uSync.Engage when personas, journeys, goals, campaign groups, segments or
traffic filters are saved or deleted, so with uSync's `ExportOnSave` turned on these files
stay up to date. Engage doesn't raise events for permission changes, so those are only
written by a full export.

## Installing

```
dotnet add package uSync.Engage
```

Requires Umbraco 17, uSync 17 and Umbraco Engage 17.5 or later. The handlers are in the
`Engage` group and run after all of the core uSync handlers.

## Notes

- Engage saves through its own database connection, outside Umbraco's transaction, so a
  failed uSync import can't roll back the Engage changes that were already made.
- Deleting a goal in Engage only marks it as deleted. Importing it again brings it back.
  Campaign groups work the same way.
- Engage needs SQL Server, so the test site in this repo uses SQL Server LocalDB.

## Licence

MPL-2.0
