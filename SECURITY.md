# Security Policy

## Supported versions

uSync.Engage ships one release line per Umbraco major, matching the uSync release it
plugs into.

| Version | Branch | Supported |
| --- | --- | --- |
| 17.x | `v17/main` | Yes |

## Reporting a vulnerability

Please **do not** open a public issue for a security problem.

Email **info@jumoo.co.uk** with a description of the issue, the version affected, and
steps to reproduce it. We'll acknowledge within a few working days and keep you
updated as we work on it.

uSync.Engage reads and writes Umbraco Engage settings to and from disk, and re-imports
those files (including on server start), so if the issue involves file paths or XML/JSON
parsing of a file that isn't fully trusted, say so - those get sequenced first.
