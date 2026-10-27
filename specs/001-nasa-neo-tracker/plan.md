# Implementation Plan: NASA NEO Date Tracker

**Branch**: `001-nasa-neo-tracker` | **Date**: 2026-10-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-nasa-neo-tracker/spec.md`

## Summary

Build a date-based near-Earth-object tracker with an ASP.NET Core Web API on .NET 10
and a React + TypeScript frontend served during development by Vite. The backend alone
calls NASA NeoWs, parses and validates its feed response, and persists successful daily
responses in a JSON file. When NASA returns no observations, serve the most recently
received cached non-empty day with an explicit fallback label. On transport or API
failure, report the error and offer (but do not automatically display) cached data until
the user chooses it. Keep business logic behind interfaces and cover it with xUnit tests
runnable from the repository root using `dotnet test`.

NASA authentication uses the public `DEMO_KEY` only as the default when `NASA_API_KEY`
is not configured; a configured runtime value overrides it. The exception for this
public NASA key is explicitly allowed by constitution v1.1.0.

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript with React

**Primary Dependencies**: ASP.NET Core Web API; `IHttpClientFactory`; React; Vite;
`System.Text.Json`; xUnit

**Storage**: Local JSON file, persisted across process restarts; no database

**Testing**: xUnit unit tests for business rules and parsing; `dotnet test` from the
repository root

**Target Platform**: .NET 10 web server and modern web browsers; Vite development server

**Project Type**: Web application (backend API and single-page frontend)

**Performance Goals**: Aspirational, non-acceptance targets: complete an upstream
request or return an error/fallback within 30 seconds; render a returned daily result
within 2 seconds after the backend response. These targets are guidance, not required
acceptance criteria.

**Constraints**: Frontend communicates only with the application backend. NASA requests
use a single selected calendar date (`start_date` equals `end_date`). The NASA key is
server-side configuration only; default to the public `DEMO_KEY`. Cache all successful
responses, but only non-empty cached days qualify for fallback. NASA HTTP requests MUST
have a 30-second timeout. No database or user authentication. Test logic through
injected interfaces and replaceable HTTP handlers.

**Scale/Scope**: Single-user local or small deployment; one date search flow, one daily
result view, one persistent JSON cache file, one NASA upstream.

## Constitution Check

### Pre-design Gate

- Reliable external handling: PASS — service errors, empty responses, and invalid
  payloads have explicit user-visible outcomes and cache fallback behavior.
- Durable local cache: PASS — successful NASA responses persist in a local JSON file.
- Separated and tested business logic: PASS — parsing, date validation, and cache
  selection stay outside UI code and are covered by xUnit tests.
- Secret-free source: PASS — only the expressly permitted public NASA `DEMO_KEY` is a
  default; `NASA_API_KEY` overrides it at runtime, and no private key enters source or
  the browser bundle.
- Simplicity: PASS — no database, authentication, or additional service is introduced.
- Reliability diagnostics: structured logging for external API failures is included
  because it enables diagnosis of failures under the constitution's reliability
  principle; logs MUST NOT contain API key values.

### Post-design Gate

PASS — the two-project backend (API and Core), one frontend, one xUnit test project,
JSON-file cache, and narrow HTTP contracts preserve all constitutional requirements.

## Project Structure

### Documentation (this feature)

```text
specs/001-nasa-neo-tracker/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── backend-api.md
└── tasks.md
```

### Source Code (repository root)

```text
NeoTracker.sln
README.md
src/
├── NeoTracker.Api/
│   ├── Program.cs
│   ├── Configuration/
│   ├── Endpoints/
│   ├── Clients/
│   ├── Persistence/
│   └── appsettings.json
└── NeoTracker.Core/
    ├── Models/
    ├── Services/
    ├── Validation/
    └── Abstractions/
tests/
└── NeoTracker.UnitTests/
    ├── Validation/
    ├── Services/
    ├── Clients/
    └── Persistence/
frontend/
├── package.json
├── vite.config.ts
├── tsconfig.json
└── src/
    ├── api/
    ├── components/
    ├── models/
    └── App.tsx
```

**Structure Decision**: Use two .NET projects to separate API/transport and persistence
from date validation, NASA data interpretation, and cache-selection business rules.
The third .NET project is the requested xUnit test suite. Keep the React/Vite SPA in
`frontend/`; it calls only same-origin `/api` endpoints, with Vite proxying those paths
to the API during development.

## Complexity Tracking

No constitution violations. No database, authentication, or additional service is
introduced.
