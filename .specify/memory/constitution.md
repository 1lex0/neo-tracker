<!--
Sync Impact Report
Version change: 1.0.0 → 1.1.0 (material expansion of Secret-Free Source)
Modified principles:
- Secret-Free Source → Secret-Free Source (narrow exception for NASA's public DEMO_KEY)
Added sections: None
Removed sections: None
Follow-up TODO: Record the original ratification date when known; remove this report before commit.
-->

# Neo Tracker Constitution

## Core Principles

### I. Reliable External API Handling
The application MUST remain usable when the network is unavailable or an external API
returns an error, an empty response, or data that cannot be parsed. It MUST show the user
a clear, actionable message instead of crashing or presenting a successful result for a
failed request. External responses MUST be validated before the application uses them.
This keeps failures understandable and prevents invalid data from leaking into the UI.

### II. Durable Local Cache
All successfully received external data MUST be cached locally and remain available after
the application restarts. Cache selection and fallback behavior MUST be implemented
outside the UI so that network failures can use valid cached data consistently.

### III. Separated, Tested Business Logic
API response parsing, input validation, and cache selection MUST be separated from UI
code. These business rules MUST have unit tests. Keeping the logic independent makes
failure behavior predictable and lets it be verified without rendering the UI.

### IV. Secret-Free Source
Private API keys and other secrets MUST NOT be stored in source code or committed
repository files. They MUST be supplied through an appropriate local or deployment
configuration.

The publicly documented NASA `DEMO_KEY` MAY be used as a default only when explicitly
requested for a feature. The application MUST allow `NASA_API_KEY` from runtime
configuration to override this default, and MUST NOT treat `DEMO_KEY` as a private
credential or extend this exception to any other API key.

### V. Simplicity
Implementations MUST prefer the simplest design that satisfies user needs. The
application MUST NOT introduce a database or authentication unless the constitution is
amended to authorize that complexity.

## Technical Constraints

The application MUST communicate API and network failures to the user in understandable
language. A valid cached result MAY be shown when a request fails, but the application
MUST NOT represent stale or missing data as a successful fresh response. Persistent
caching MUST use a local mechanism that survives application restarts; a database is
not permitted.

## Quality Workflow

Changes to API parsing, input validation, or cache selection MUST include or update
unit tests for the affected behavior, including relevant failure and empty-response
cases. The README MUST document one command that runs the project's tests. Changes MUST
preserve the principles in this constitution, and reviewers MUST check that behavior and
tests comply before integration.

## Governance

This constitution governs application design and implementation. Amendments MUST
document their rationale, be reviewed and approved by the project maintainers, and update
the version and last-amended date. Version changes follow semantic versioning: MAJOR for
removing or redefining a principle, MINOR for adding a principle or materially expanding
requirements, and PATCH for clarifications that do not change requirements.

Every change review MUST consider reliability at external API boundaries, durable cache
behavior, separation and unit coverage of business logic, and protection of secrets.
Any proposed exception to a principle requires an approved constitution amendment;
implementation changes MUST NOT silently weaken these requirements.

**Version**: 1.1.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date unknown | **Last Amended**: 2026-10-27
