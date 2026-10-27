# uSync.Engage

uSync for Umbraco Engage. Writes Engage's settings to disk as uSync files and imports
them on another site:

- persona groups and personas
- customer journeys and steps
- goals, with their scoring
- campaign groups, campaigns and their scoring
- segments and their rules
- traffic filters
- document type and user group permissions

References between them (personas, journey steps, goals, media, document types) are written
as keys and matched to the ids on each site when imported.

Analytics and visitor data, A/B tests, applied personalizations and content scoring are not
synced.

Requires Umbraco 17, uSync 17 and Umbraco Engage 17.5 or later.

https://github.com/Jumoo/uSync.Engage
