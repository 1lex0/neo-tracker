# Quickstart: NASA NEO Date Tracker

## Prerequisites

- .NET 10 SDK
- Node.js and npm supported by the selected Vite release
- Optional: a NASA API key. The backend defaults to NASA's public `DEMO_KEY`; set
  `NASA_API_KEY` in the backend process environment to override it.

## Validate

From the repository root:

```powershell
dotnet test
```

Expected result: the solution builds and all xUnit tests pass without making live NASA
requests.

## Run locally

1. Start the ASP.NET Core API from `src/NeoTracker.Api`.
2. Start the Vite development server from `frontend`.
3. Open the Vite local URL. Its `/api` requests are proxied to the API.
4. Enter a real date in `YYYY-MM-DD` format and select **Get Data**.

Expected result: the UI displays either the requested date's NASA observations, the
documented empty-result fallback, or a clear error with an explicit user action to view
cached results during upstream failure.

## Manual scenarios

1. **Fresh data**: request a date with observations; confirm units, hazard indicator,
   source, and received timestamp.
2. **Invalid input**: submit an empty, malformed, or impossible date; confirm a
   validation message and no request to NASA.
3. **Empty day**: provide an empty NASA feed response with a previously persisted
   non-empty cache; confirm the latest-received non-empty day is displayed and labelled
   as fallback with its date.
4. **Upstream outage**: make NASA unavailable with a non-empty cache present; confirm
   the error and cache offer appear, and the cached observations appear only after the
   user chooses the offer.
5. **Restart persistence**: retrieve a successful day, restart the API, and confirm the
   result remains available in the JSON cache.

See [backend-api.md](./contracts/backend-api.md) and [data-model.md](./data-model.md)
for response and data details.
