# TMS API Versioning Policy

## What Counts as a Breaking Change

Any of the following requires a new API version:

- Removing a field from a response
- Renaming a field in a request or response
- Changing the type of a field (e.g. int to string)
- Changing a status code for an existing scenario
- Tightening validation on an existing field (e.g. making an optional field required, narrowing an accepted range)
- Changing the default sort order or pagination behavior
- Removing an endpoint

## What Counts as Additive (Non-Breaking)

These can ship to an existing version without a bump:

- Adding a new optional field to a response
- Adding a new endpoint
- Adding a new optional query parameter
- Relaxing validation (e.g. widening an accepted range)
- Adding a new optional request field with a safe default

## Sunset Window

TMS commits to keeping a deprecated version running for **6 months minimum**
after its successor ships. This gives rural training centres on quarterly
maintenance schedules at least one full maintenance cycle, plus buffer, to
migrate before the old version is shut off.

## Communication

When a new version ships and an old one is marked for retirement:

1. The deprecated version immediately starts returning `Deprecation`,
   `Sunset`, and `Link` headers on every response, from day one — not
   later.
2. A `CHANGELOG.md` entry documents the new version and the sunset date
   for the old one.
3. Every team known to hold an API key receives a direct email with the
   sunset date and migration guidance.
4. A calendar invite is sent for the actual shutdown date, so it isn't a
   surprise for anyone tracking the migration.

## Skipping Versions

Clients are not required to migrate through every intermediate version.
A client on V1 may jump straight to V3 once V3 ships; V2 does not need
to be a mandatory waypoint. The only requirement is that the client
lands on a version that is still within its own support window.