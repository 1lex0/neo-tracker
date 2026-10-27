# Tasks: NASA NEO Date Tracker

**Input**: Design documents from `/specs/001-nasa-neo-tracker/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`,
`contracts/backend-api.md`

**Tests**: xUnit tests are required by the specification and constitution. All tests
must run offline with `dotnet test` from the repository root.

**Organization**: Tasks are grouped by user story so each journey can be implemented
and verified as an incremental deliverable.

## Phase 1: Setup

**Purpose**: Create the application projects and runnable solution structure.

- [X] T001 [P] Create `NeoTracker.sln`, `src/NeoTracker.Api/NeoTracker.Api.csproj`, `src/NeoTracker.Core/NeoTracker.Core.csproj`, and `tests/NeoTracker.UnitTests/NeoTracker.UnitTests.csproj`; add both source projects and the xUnit test project to the root solution and reference Core from Api and UnitTests.
- [X] T002 [P] Scaffold the React + TypeScript Vite application under `frontend/` and configure `frontend/vite.config.ts` to proxy `/api` requests to the local ASP.NET Core API.
- [X] T003 Add the shared root `.gitignore` entries for .NET build outputs, `frontend/node_modules/`, generated Vite output, and local cache files in `.gitignore`.

---

## Phase 2: Foundational

**Purpose**: Implement shared domain contracts, configuration, persistence, and
test seams required by all user stories.

- [X] T004 Create `SelectedDate.cs`, `AsteroidObservation.cs`, `DailyResult.cs`, `CachedDailyResult.cs`, `ApiResponse.cs`, and `ApiError.cs` in `src/NeoTracker.Core/Models/`; preserve the data-model constraints: `value` is a real Gregorian date in exact `YYYY-MM-DD` form; asteroid `id` and `name` are required; diameters are non-negative with maximum greater than or equal to minimum; velocity and distance are non-negative; hazard status is required; daily observations may be empty; source and successful response receipt time are required.
- [X] T005 Define `INasaNeoClient.cs`, `IDailyResultCache.cs`, and `IDailyResultService.cs` in `src/NeoTracker.Core/Abstractions/` so HTTP transport, persistence, and business logic can be replaced in tests.
- [X] T006 Add backend configuration in `src/NeoTracker.Api/Configuration/NasaApiOptions.cs` and `src/NeoTracker.Api/Program.cs` to read `NASA_API_KEY` from server runtime configuration, use the public `DEMO_KEY` only when unset, and keep the key out of frontend settings and responses.
- [X] T007 Implement JSON-file cache loading and persistence in `src/NeoTracker.Api/Persistence/JsonDailyResultCache.cs`; retain successful empty and non-empty daily results, serialize concurrent in-process writers, and write through a unique same-directory temporary file before replacing the active cache file.
- [X] T008 Register Core services, `IDailyResultCache`, and the typed NASA HTTP client in `src/NeoTracker.Api/Program.cs`; configure the cache path from server configuration and set the NASA HTTP request timeout to 30 seconds.
- [X] T009 [P] Add `TestHttpMessageHandler.cs` and `TemporaryCacheDirectory.cs` fixtures in `tests/NeoTracker.UnitTests/Fixtures/` for injectable HTTP responses and isolated cache files in deterministic, offline tests.
- [X] T010 Add global JSON serialization settings, including stable camel-case API response names, in `src/NeoTracker.Api/Program.cs`.

**Checkpoint**: Solution builds, the API can start with runtime configuration, and
tests can substitute HTTP and file-cache dependencies.

---

## Phase 3: User Story 1 - View Asteroids for a Selected Date (Priority: P1)

**Goal**: Retrieve a valid selected day and show its asteroid details, source, and
response timestamp.

**Independent Test**: Return a deterministic non-empty NASA feed fixture for a valid
date; verify the normalized asteroid fields and metadata through the backend contract
and the rendered frontend view.

### Tests for User Story 1

- [X] T011 [P] [US1] Add the representative successful NASA NeoWs date-keyed feed fixture `tests/NeoTracker.UnitTests/Fixtures/nasa-feed-sample.json`.
- [X] T012 [US1] Add xUnit tests for NASA feed parsing, date-keyed observation selection, required asteroid values, and unit mapping in `tests/NeoTracker.UnitTests/Clients/NasaFeedParserTests.cs`; use the fixture created by T011.

### Implementation for User Story 1

- [X] T013 [US1] Implement NASA feed parsing in `src/NeoTracker.Core/Services/NasaFeedParser.cs`; map `estimated_diameter.kilometers` bounds to meters, use NASA `relative_velocity.kilometers_per_hour` and `miss_distance.kilometers`, and preserve the potential-hazard flag and object identity.
- [X] T014 [US1] Implement `INasaNeoClient` in `src/NeoTracker.Api/Clients/NasaNeoClient.cs` using the injected `HttpClient`; request the single selected day with equal `start_date` and `end_date`, URL-encode the configured key, and return parsed data plus UTC receipt time.
- [X] T015 [US1] Implement the fresh-result orchestration path in `src/NeoTracker.Core/Services/DailyResultService.cs` and expose `GET /api/asteroids?date={date}` in `src/NeoTracker.Api/Endpoints/AsteroidEndpoints.cs` using the response schema in `specs/001-nasa-neo-tracker/contracts/backend-api.md`.
- [X] T016 [P] [US1] Define matching frontend asteroid and daily-result types in `frontend/src/models/asteroid.ts`.
- [X] T017 [P] [US1] Implement the backend-only request wrapper for `GET /api/asteroids?date=...` in `frontend/src/api/asteroids.ts`; convert non-success responses into typed application errors without calling NASA from the browser.
- [X] T018 [US1] Implement the date entry form and request submission state in `frontend/src/components/DateSearchForm.tsx`; keep submission unavailable when the date field is empty and call the frontend API wrapper only for a valid user action.
- [X] T019 [P] [US1] Implement the asteroid row/card view in `frontend/src/components/AsteroidList.tsx`; show name, min-max diameter in meters, speed in km/h, distance in km, and a visually distinct hazard status.
- [X] T020 [US1] Connect search, result list, source attribution, and response timestamp in `frontend/src/App.tsx`; show loading, successful result, and no-result states without presenting stale data as fresh.

**Checkpoint**: A fresh non-empty day can be retrieved through the backend and viewed
in the frontend; parsing is tested without live network access.

---

## Phase 4: User Story 2 - Use Cached Data When Current Data Is Unavailable (Priority: P1)

**Goal**: Persist every successful NASA response and apply the distinct empty-result
and upstream-failure fallback behaviors.

**Independent Test**: With a seeded cache containing multiple successful days, test an
empty NASA response and a transport/API failure. Confirm empty results display the
latest-received non-empty day automatically, while failure only offers the cache until
the user selects it.

### Tests for User Story 2

- [X] T021 [P] [US2] Add xUnit tests for a valid empty NASA feed, malformed JSON, missing required response fields, and non-success upstream HTTP status in `tests/NeoTracker.UnitTests/Clients/NasaFeedParserTests.cs` and `tests/NeoTracker.UnitTests/Clients/NasaNeoClientTests.cs`.
- [X] T022 [P] [US2] Add xUnit tests proving fallback selection returns the non-empty cached result with the latest receipt time, even when its date is later than the requested date, and ignores empty cached days in `tests/NeoTracker.UnitTests/Services/DailyResultFallbackTests.cs`.
- [X] T023 [P] [US2] Add xUnit persistence tests proving successful empty and non-empty daily results survive cache reload and failed writes do not replace the previous cache in `tests/NeoTracker.UnitTests/Persistence/JsonDailyResultCacheTests.cs`.
- [X] T024 [US2] Add xUnit orchestration tests for empty-result fallback, no eligible cache, upstream failure offers, and absence of cached observations from failure responses in `tests/NeoTracker.UnitTests/Services/DailyResultServiceTests.cs`.

### Implementation for User Story 2

- [X] T025 [US2] Implement cache selection in `src/NeoTracker.Core/Services/DailyResultService.cs`; persist every successful upstream result, but choose the latest-received result with at least one observation for fallback regardless of calendar-date ordering.
- [X] T026 [US2] Update `GET /api/asteroids?date={date}` in `src/NeoTracker.Api/Endpoints/AsteroidEndpoints.cs` to automatically return the selected cached non-empty day with `isFallback=true`, `fallbackReason=no-results`, requested date, and displayed date when NASA returns an empty list.
- [X] T027 [US2] Add explicit upstream-failure handling in `src/NeoTracker.Api/Endpoints/AsteroidEndpoints.cs`; return `503` with a clear error code/message and cached availability/date metadata, but never include cached asteroid observations in that response.
- [X] T028 [US2] Add `GET /api/asteroids/cache/latest` in `src/NeoTracker.Api/Endpoints/AsteroidEndpoints.cs` to return the latest-received cached non-empty day only after the user requests it, or `404 cached_results_unavailable` if none exists.
- [X] T029 [US2] Implement the service-error message and explicit “view cached data” action in `frontend/src/App.tsx`; request `/api/asteroids/cache/latest` only after the user activates the action and label the cached date and failure reason.

**Checkpoint**: Empty upstream results use the latest-received non-empty cache; upstream
failures report the issue and offer cached data without displaying it before user
confirmation; persisted data survives restart.

---

## Phase 5: User Story 3 - Understand and Recover from Invalid Input (Priority: P2)

**Goal**: Reject empty, malformed, and impossible dates before any NASA request and
give the user actionable feedback.

**Independent Test**: Submit an empty value, malformed date, February 30, and a
non-leap-year February 29; verify validation messages, disabled empty submission, and
zero NASA client calls.

### Tests for User Story 3

- [ ] T030 [P] [US3] Add xUnit cases for empty values, malformed format, February 30, non-leap-year February 29, and valid leap-day dates in `tests/NeoTracker.UnitTests/Validation/SelectedDateValidatorTests.cs`.
- [ ] T031 [P] [US3] Add an xUnit endpoint/service test proving invalid dates return `400 invalid_date` and do not invoke the NASA client in `tests/NeoTracker.UnitTests/Services/DailyResultServiceTests.cs`.

### Implementation for User Story 3

- [ ] T032 [US3] Implement exact `YYYY-MM-DD` Gregorian date validation in `src/NeoTracker.Core/Validation/SelectedDateValidator.cs`; reject empty input, extra whitespace, invalid separators, out-of-range values, and impossible calendar dates.
- [ ] T033 [US3] Apply `SelectedDateValidator` before cache selection or NASA calls in `src/NeoTracker.Core/Services/DailyResultService.cs` and map validation failures to `400 invalid_date` in `src/NeoTracker.Api/Endpoints/AsteroidEndpoints.cs`.
- [ ] T034 [US3] Update `frontend/src/components/DateSearchForm.tsx` to show required, format, and impossible-date messages; keep the request button disabled for an empty field and prevent invalid submissions from calling the backend.

**Checkpoint**: Invalid values are rejected consistently in UI and backend, with no
upstream request.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Document and verify the complete vertical slice.

- [ ] T035 [P] Document prerequisites, `NASA_API_KEY` override, `DEMO_KEY` default, cache location, frontend startup, and the root-level `dotnet test` command in `README.md`.
- [ ] T036 [P] Add structured server-side logging for NASA transport failures, unsuccessful status codes, JSON parse failures, and cache read/write failures in `src/NeoTracker.Api/Clients/NasaNeoClient.cs` and `src/NeoTracker.Api/Persistence/JsonDailyResultCache.cs`; do not log API key values.
- [ ] T037 Verify all unit tests run offline from the repository root with `dotnet test` and correct failures in `NeoTracker.sln` and `tests/NeoTracker.UnitTests/`.
- [ ] T038 Execute the manual scenarios in `specs/001-nasa-neo-tracker/quickstart.md` and record any required corrections in the affected source files.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No prerequisites; T001 and T002 may run in parallel.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user story phases.
- **User Story 1 (Phase 3)**: Depends on the shared models, service interfaces, and
  registered HTTP client from Phase 2.
- **User Story 2 (Phase 4)**: Depends on the NASA feed client and normalized daily
  result contract from User Story 1; its cache and failure behavior is independently
  testable with fakes.
- **User Story 3 (Phase 5)**: Depends on shared models and service interfaces from
  Phase 2; can be developed independently of User Story 2.
- **Polish (Phase 6)**: Depends on all selected stories.

### User Story Dependencies

- **US1 (P1)**: First vertical slice; no dependency on another story.
- **US2 (P1)**: Requires the NASA client and result model established in US1.
- **US3 (P2)**: Requires only the shared Phase 2 abstractions; can proceed in parallel
  with US1 after foundation.

### Parallel Opportunities

- T001 and T002 create separate backend and frontend project trees.
- T009 is independent of T008 and T010 after project scaffolding.
- In US1, T011 must complete before T012 because the parser tests consume its fixture;
  T016, T017, and T019 touch separate frontend files and can proceed while backend
  parsing is implemented.
- In US2, T021-T023 are independent test files/areas and can be authored in parallel;
  frontend work T029 is independent after the error and cache endpoint contracts are
  agreed.
- In US3, T031 and T032 can be written in parallel before validator implementation.
- T035 documentation and T036 logging touch distinct files and can run in parallel.

### Parallel Example: User Story 1

```text
Task: T016 Define frontend DTOs in frontend/src/models/asteroid.ts
Task: T019 Implement asteroid list display in frontend/src/components/AsteroidList.tsx
```

Complete T011 before T012; the parser tests use the fixture created by T011.

### Parallel Example: User Story 3

```text
Task: T030 Add date validator unit tests in tests/NeoTracker.UnitTests/Validation/SelectedDateValidatorTests.cs
Task: T031 Add invalid-date no-request test in tests/NeoTracker.UnitTests/Services/DailyResultServiceTests.cs
```

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Setup and Foundational phases.
2. Implement the fresh-data path and frontend result view in User Story 1.
3. Run focused xUnit tests and verify the UI against the successful fixture.
4. Validate the first independently usable vertical slice before proceeding.

### Incremental Delivery

1. Add User Story 1 for valid-date NASA data retrieval and display.
2. Add User Story 2 for durable cache, empty-result fallback, and user-confirmed outage
   fallback.
3. Add User Story 3 for strict date validation and no-request guarantees.
4. Finish documentation, error logging, full `dotnet test`, and quickstart validation.
