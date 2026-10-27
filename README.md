# NASA NEO Tracker

Web application that shows near-Earth asteroids for a selected date using the
[NASA NeoWs API](https://api.nasa.gov/): name, diameter range (m), speed (km/h), miss
distance (km) and a highlighted potential-hazard flag, together with the data source
and response time.

Successful NASA responses are cached in a local JSON file and survive restarts. When
NASA has no asteroids for a date, the most recently received non-empty cached day is
shown with a clear "fallback" label. When NASA is unreachable, the application reports
the failure and offers the cached day, showing it only after the user asks.

- Specification: [specs/001-nasa-neo-tracker/](specs/001-nasa-neo-tracker/)
- Spec deviations and decisions: [notes/spec-deviations.md](notes/spec-deviations.md)

## Structure

| Path | Contents |
|---|---|
| `src/NeoTracker.Core` | Domain models, date validation, NASA feed parsing, cache-fallback selection |
| `src/NeoTracker.Api` | ASP.NET Core minimal API, NASA HTTP client, JSON file cache |
| `tests/NeoTracker.UnitTests` | xUnit tests (offline; no live NASA calls) |
| `frontend` | React + TypeScript + Vite single-page UI |

## Prerequisites

- .NET 10 SDK
- Node.js 20.19+ or 22.12+ (tested with Node 24) and npm

## Run the tests

From the repository root:

```powershell
dotnet test
```

## Run the application

1. Start the API (listens on `http://localhost:5080`):

   ```powershell
   dotnet run --project src/NeoTracker.Api
   ```

2. In a second terminal, start the frontend:

   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

3. Open the URL Vite prints (by default `http://localhost:5173`). Vite proxies `/api`
   to the API; set `NEO_API_URL` before `npm run dev` if the API runs elsewhere.

## Configuration

### NASA API key

The backend uses NASA's public `DEMO_KEY` by default (30 requests/hour, 50/day per IP).
To use your own key, set `NASA_API_KEY` in the API process environment — never in
`appsettings.json` or frontend `VITE_*` variables:

```powershell
$env:NASA_API_KEY = "your-key"
dotnet run --project src/NeoTracker.Api
```

or keep it in .NET user secrets:

```powershell
dotnet user-secrets init --project src/NeoTracker.Api
dotnet user-secrets set NASA_API_KEY "your-key" --project src/NeoTracker.Api
```

The key is only used server-side; the browser never sees it.

### Cache location

By default the cache is `src/NeoTracker.Api/App_Data/neo-cache.json` (relative to the
API content root; ignored by git). Override it with `Cache:FilePath`, for example
`$env:Cache__FilePath = "D:\neo\cache.json"`. Delete the file to clear the cache. If
the file becomes unreadable, the API starts with an empty cache and keeps a copy as
`neo-cache.json.unreadable`.

## API

See [contracts/backend-api.md](specs/001-nasa-neo-tracker/contracts/backend-api.md).

- `GET /api/asteroids?date=YYYY-MM-DD` — fresh data, no-results fallback (`200`),
  `400 invalid_date`, or `503 upstream_unavailable` with cache metadata
- `GET /api/asteroids/cache/latest?requestedDate=YYYY-MM-DD` — latest cached non-empty
  day, requested after the user accepts the offer (`404 cached_results_unavailable`
  if none)
