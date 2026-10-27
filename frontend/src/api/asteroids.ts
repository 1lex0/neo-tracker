import type { ApiErrorBody, DailyResultResponse } from '../models/asteroid'

// The browser talks only to the application backend; NASA and its key stay server-side.

export class AsteroidApiError extends Error {
  readonly status: number | null
  readonly code: string
  readonly cachedFallbackAvailable: boolean
  readonly cachedDate: string | null

  constructor(status: number | null, body: ApiErrorBody) {
    super(body.message)
    this.name = 'AsteroidApiError'
    this.status = status
    this.code = body.code
    this.cachedFallbackAvailable = body.cachedFallbackAvailable ?? false
    this.cachedDate = body.cachedDate ?? null
  }
}

export const NETWORK_ERROR_CODE = 'network_error'
export const UNEXPECTED_RESPONSE_CODE = 'unexpected_response'

export function fetchAsteroids(date: string, signal?: AbortSignal): Promise<DailyResultResponse> {
  return getJson(`/api/asteroids?date=${encodeURIComponent(date)}`, signal)
}

/** Only called after the user explicitly chooses to view cached data during an outage. */
export function fetchLatestCached(requestedDate: string, signal?: AbortSignal): Promise<DailyResultResponse> {
  return getJson(`/api/asteroids/cache/latest?requestedDate=${encodeURIComponent(requestedDate)}`, signal)
}

async function getJson(url: string, signal?: AbortSignal): Promise<DailyResultResponse> {
  let response: Response
  try {
    response = await fetch(url, { headers: { Accept: 'application/json' }, signal })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new AsteroidApiError(null, {
      code: NETWORK_ERROR_CODE,
      message: 'The application server could not be reached.',
    })
  }

  const body: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    // The dev proxy (or a reverse proxy) answers 502/504 without our JSON body when the API is down.
    if (!isApiErrorBody(body) && [502, 504].includes(response.status)) {
      throw new AsteroidApiError(response.status, {
        code: NETWORK_ERROR_CODE,
        message: 'The application server could not be reached.',
      })
    }
    throw new AsteroidApiError(
      response.status,
      isApiErrorBody(body)
        ? body
        : { code: UNEXPECTED_RESPONSE_CODE, message: `Unexpected server response (HTTP ${response.status}).` },
    )
  }

  if (!isDailyResultResponse(body)) {
    throw new AsteroidApiError(response.status, {
      code: UNEXPECTED_RESPONSE_CODE,
      message: 'The server returned data in an unexpected format.',
    })
  }

  return body
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as ApiErrorBody).code === 'string' &&
    typeof (value as ApiErrorBody).message === 'string'
  )
}

function isDailyResultResponse(value: unknown): value is DailyResultResponse {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as DailyResultResponse
  return (
    (typeof candidate.requestedDate === 'string' || candidate.requestedDate === null) &&
    typeof candidate.source === 'string' &&
    typeof candidate.receivedAtUtc === 'string' &&
    typeof candidate.isFallback === 'boolean' &&
    Array.isArray(candidate.asteroids)
  )
}
