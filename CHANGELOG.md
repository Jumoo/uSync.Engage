# Changelog

## [17.0.0] - 2026-11-17

First stable release, for Umbraco 17, uSync 17.3.5+ and Umbraco Engage 17.5+.

### Fixed

- A new segment took the bottom of Engage's list on its first import instead of the sort order
  in its uSync file.
- Persona and journey step icons from Engage's own image library were lost when an item moved
  to another site, because Engage gives those images new keys on every site. Icons now carry
  the image's media path too, and are found by path when the key isn't there.
- Export results on the uSync dashboard were all grouped under "Other" instead of by Engage
  item type.

## [17.0.0-beta02] - 2026-11-16

### Added

- `uSync.Engage.Complete` package: uSync.Complete push and pull for persona groups, customer
  journeys, goals, campaign groups, segments and traffic filters, from Engage's own actions
  menus (#2).
- uSync.Complete item managers and dependency checkers for those types in `uSync.Engage`.

### Fixed

- The uSync dashboard's progress view showed raw handler names (`customerJourneys`,
  `docPerms`) for the Engage handlers.

## [17.0.0-beta01] - 2026-10-27

First beta, for Umbraco 17, uSync 17.3.5+ and Umbraco Engage 17.5+.

### Added

- Syncs Umbraco Engage persona groups, customer journeys, goals, campaign groups, segments,
  traffic filters, and document type and user group permissions.
- References between them (personas, journey steps, goals, media and document types) are written
  as keys and matched to each site's own ids on import.
- Export on save for everything Engage raises events for (all but the permissions).
