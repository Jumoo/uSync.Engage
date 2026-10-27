# Changelog

## Unreleased

## [17.0.0-beta01] - 2026-10-27

First beta, for Umbraco 17, uSync 17.3.5+ and Umbraco Engage 17.5+.

### Added

- Syncs Umbraco Engage persona groups, customer journeys, goals, campaign groups, segments,
  traffic filters, and document type and user group permissions.
- References between them (personas, journey steps, goals, media and document types) are written
  as keys and matched to each site's own ids on import.
- Export on save for everything Engage raises events for (all but the permissions).
