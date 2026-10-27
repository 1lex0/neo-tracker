# Data Model: NASA NEO Date Tracker

## SelectedDate

The Gregorian calendar date requested by the user.

| Field | Type | Constraints |
|---|---|---|
| value | date | Required; exact input representation `YYYY-MM-DD`; must be a real Gregorian date |

Validation rejects empty input, strings not matching the exact format, and impossible
dates before any NASA request is issued.

## AsteroidObservation

One near-Earth object from a daily NASA NeoWs feed response.

| Field | Type | Constraints |
|---|---|---|
| id | string | NASA object identifier; required for identity within a feed |
| name | string | Required display name |
| estimatedDiameterMinMeters | number | Required; non-negative |
| estimatedDiameterMaxMeters | number | Required; greater than or equal to minimum |
| relativeVelocityKph | number | Required; non-negative |
| missDistanceKm | number | Required; non-negative |
| isPotentiallyHazardous | boolean | Required |

An observation belongs to one `DailyResult`. Measurements are normalized to the units
shown in the interface.

## DailyResult

The normalized result for one selected date.

| Field | Type | Constraints |
|---|---|---|
| date | date | Required; date key from the feed, equal to requested date for an upstream result |
| observations | list of AsteroidObservation | Required; may be empty |
| source | string | `NASA NeoWs API` for results originating from NASA |
| receivedAtUtc | timestamp | Required for successful upstream responses; UTC response receipt time |

A successful empty response is a valid daily result and may be persisted. It does not
qualify as a fallback display.

## CachedDailyResult

A successfully received `DailyResult` stored locally across application restarts.

| Field | Type | Constraints |
|---|---|---|
| result | DailyResult | Required |
| cacheReceivedAtUtc | timestamp | Required; used to order cached results by receipt time |

The cache retains successful empty and non-empty days. Fallback selection considers
only entries with at least one observation and chooses the entry with the latest
`cacheReceivedAtUtc`, regardless of its date relative to the selected date.

## Display State

The user interface presents one of the following states:

- **Fresh results**: non-empty response for the requested date, labelled with requested
  date, source, and response time.
- **No results fallback**: latest eligible cached non-empty day is displayed
  automatically, with requested date, fallback date, and reason.
- **Service failure offer**: failure message and an explicit action to view the latest
  eligible cached day; cached observations are not displayed before the action.
- **No cached results**: explanatory message that no eligible cached observations are
  available.
- **Validation error**: actionable date message; no external request is made.
