// Mirrors the backend contract in specs/001-nasa-neo-tracker/contracts/backend-api.md.

export interface Asteroid {
  id: string
  name: string
  estimatedDiameterMinMeters: number
  estimatedDiameterMaxMeters: number
  relativeVelocityKph: number
  missDistanceKm: number
  isPotentiallyHazardous: boolean
}

export type FallbackReason = 'no-results' | 'upstream-unavailable'

export interface DailyResultResponse {
  requestedDate: string
  displayedDate: string | null
  source: string
  receivedAtUtc: string
  isFallback: boolean
  fallbackReason: FallbackReason | null
  asteroids: Asteroid[]
  message?: string
}

export interface ApiErrorBody {
  code: string
  message: string
  cachedFallbackAvailable?: boolean
  cachedDate?: string
}
