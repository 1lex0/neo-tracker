# Backend API Contract

The React application calls only this backend. NASA credentials and upstream requests
remain server-side. Dates use the exact `YYYY-MM-DD` representation.

## `GET /api/asteroids?date={date}`

Requests data for one selected calendar date.

### Successful fresh result — `200 OK`

```json
{
  "requestedDate": "2026-10-27",
  "displayedDate": "2026-10-27",
  "source": "NASA NeoWs API",
  "receivedAtUtc": "2026-10-27T21:40:00Z",
  "isFallback": false,
  "fallbackReason": null,
  "asteroids": [
    {
      "id": "123456",
      "name": "(2026 AB)",
      "estimatedDiameterMinMeters": 10.0,
      "estimatedDiameterMaxMeters": 22.0,
      "relativeVelocityKph": 45000.0,
      "missDistanceKm": 1000000.0,
      "isPotentiallyHazardous": false
    }
  ]
}
```

### Empty upstream result with a cached fallback — `200 OK`

Return the latest-received cached non-empty day automatically. Set `isFallback` to
`true`, `displayedDate` to the cached date, and `fallbackReason` to `no-results`.
Identify the requested date separately from the displayed cache date.

### Empty upstream result without a cached fallback — `200 OK`

```json
{
  "requestedDate": "2026-10-27",
  "displayedDate": null,
  "source": "NASA NeoWs API",
  "receivedAtUtc": "2026-10-27T21:40:00Z",
  "isFallback": false,
  "fallbackReason": null,
  "asteroids": [],
  "message": "No asteroid results or cached non-empty results are available."
}
```

### Invalid date — `400 Bad Request`

Return a stable machine-readable error code and a clear message. Do not call NASA or
read/select fallback data for invalid input.

```json
{
  "code": "invalid_date",
  "message": "Enter a real date in YYYY-MM-DD format."
}
```

### Upstream failure — `503 Service Unavailable`

Do not include cached observations in this response. Indicate whether cached non-empty
data can be offered and identify its date, if available.

```json
{
  "code": "upstream_unavailable",
  "message": "NASA data is currently unavailable.",
  "cachedFallbackAvailable": true,
  "cachedDate": "2026-10-26"
}
```

## `GET /api/asteroids/cache/latest`

Called only after the user chooses the cache offer shown for an upstream failure.
Returns the latest-received cached non-empty daily result using the successful result
shape with `isFallback: true` and `fallbackReason: "upstream-unavailable"`. Returns
`404 Not Found` with code `cached_results_unavailable` if the eligible cache entry is
no longer available.

## Cross-cutting behavior

- The backend validates date query values as real Gregorian dates in `YYYY-MM-DD`.
- Unexpected NASA payload shapes and non-success upstream status codes are treated as
  upstream failures, not successful empty results.
- All API error responses use JSON with a stable `code` and user-readable `message`.
- The frontend never calls NASA directly and never receives the NASA API key.
