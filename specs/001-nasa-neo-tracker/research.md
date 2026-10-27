# Research: NASA NEO Date Tracker

## Decisions

### .NET Web API and Service Boundaries

- **Decision**: Use ASP.NET Core Web API on .NET 10. Register a typed NASA client with
  `IHttpClientFactory`; keep endpoint handlers thin and place date validation, response
  interpretation, and cache selection behind Core interfaces.
- **Rationale**: .NET 10 is an LTS release, and typed clients provide dependency
  injection and managed HTTP handler lifetimes. Injected interfaces make business rules
  and HTTP behavior testable without live NASA calls.
- **Alternatives considered**: A single unstructured API project, which would mix
  parsing and UI transport; a separate infrastructure project, which adds project
  complexity without a demonstrated need. Minimal API endpoints are preferred for the
  small surface, while controllers remain viable if the surface grows.
- **Sources**:
  - [.NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
  - [ASP.NET Core Web APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0)
  - [`IHttpClientFactory`](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
  - [ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

### NASA NeoWs Feed Contract and Rate Limits

- **Decision**: Request one selected date by setting `start_date` and `end_date` to that
  same ISO date. Interpret `near_earth_objects` as a date-keyed map; map required
  asteroid fields and the first relevant close-approach measurements into the
  application's domain response. Use `NASA_API_KEY` at runtime or the expressly
  permitted NASA public `DEMO_KEY` default.
- **Rationale**: NeoWs feed results are grouped by date, and feed ranges have a maximum
  span of seven days. Per-day requests stay within that boundary. The shared `DEMO_KEY`
  has lower quotas than an individual key, so persistent caching prevents unnecessary
  repeat requests.
- **Alternatives considered**: Requesting a multi-day range would add paging/range
  handling without serving this feature. Requiring a private key would contradict the
  user's specified default.
- **Operational note**: NASA documents `DEMO_KEY` limits of 30 requests/hour per IP
  and 50/day per IP, versus 1,000 requests/hour for a standard key. Treat rate limiting
  as an upstream failure and preserve cached results.
- **Sources**:
  - [NASA API authentication and rate limits](https://github.com/nasa/api-docs/blob/e05c7649a26976da6c0d0248afc92932751c7982/assets/html/authentication.html)
  - [NASA API definitions, including NeoWs](https://github.com/nasa/api-docs/blob/e05c7649a26976da6c0d0248afc92932751c7982/assets/json/apis.json)
  - [NeoWs feed example](https://api.nasa.gov/neo/rest/v1/feed?start_date=2026-10-20&end_date=2026-10-26&api_key=DEMO_KEY)

### JSON File Persistence

- **Decision**: Serialize cache access behind an injected cache interface. Write updates
  to a unique temporary file in the same directory, flush and close it, then replace or
  move it into place. Serialize writers within the process.
- **Rationale**: This preserves the required no-database design and reduces the chance
  that interruption leaves a partially written cache. Keeping the temporary file on the
  same volume supports file replacement semantics; filesystem behavior must still be
  validated for the target environment.
- **Alternatives considered**: Directly overwriting the active file risks corrupting
  persisted results if the process stops mid-write. SQLite would offer transactional
  writes but violates the current simplicity constraint.
- **Sources**:
  - [System.Text.Json serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to)
  - [`File.Move`](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0)
  - [`File.Replace`](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=net-10.0)
  - [`FileStream.Flush`](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0)

### React, TypeScript, and Vite

- **Decision**: Use Vite's `react-ts` template, typed frontend DTOs, and relative `/api`
  paths. Proxy `/api` to ASP.NET Core in development; use same-origin hosting or a
  reverse proxy in production.
- **Rationale**: The application is a small client-side tracker with a dedicated API.
  Same-origin paths avoid putting NASA credentials in the client and avoid broad CORS
  configuration.
- **Alternatives considered**: A full-stack React framework is not required for this
  single-screen client. Hosting on a separate origin remains possible but requires
  origin-specific CORS policy.
- **Security note**: Never place NASA credentials in `VITE_*` variables; Vite exposes
  those values to browser code.
- **Sources**:
  - [Vite guide and templates](https://vite.dev/guide/)
  - [Vite server proxy](https://vite.dev/config/server-options#server-proxy)
  - [React and TypeScript](https://react.dev/learn/typescript)
  - [Vite environment variables](https://vite.dev/guide/env-and-mode)
  - [ASP.NET Core CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0)

### Testing

- **Decision**: Use xUnit and `dotnet test` on the root solution. Test date validation,
  parsing, empty/malformed NASA payloads, fallback selection, and JSON persistence with
  isolated inputs and replaceable HTTP handlers. Do not rely on live NASA calls in
  normal tests.
- **Rationale**: Deterministic unit tests cover failure modes without network access or
  API quota consumption.
- **Alternatives considered**: Live API calls during the default test run are
  nondeterministic and can exhaust the shared demonstration quota.
- **Sources**:
  - [.NET unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
  - [ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

## Resolved Unknowns

- Stack, API key source/default, local persistence format, and test runner were
  explicitly selected by the user.
- NASA feed date grouping, request range limit, and rate-limit behavior are documented
  above; the application requests a single day and caches successful responses.
