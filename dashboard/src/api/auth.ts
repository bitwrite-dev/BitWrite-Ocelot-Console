/**
 * Access-token seam.
 *
 * The token strategy is **not yet decided** — see #433. The API configures JWT
 * bearer validation against an external authority but exposes no token-issuance
 * endpoint, so there is currently nothing for the dashboard to log in against.
 *
 * Deliberately, this module picks a side. The HTTP client depends only on
 * `AccessTokenProvider`, so whichever strategy is chosen slots in here without
 * touching `http.ts` or the resource modules.
 *
 * Two options on record:
 *  - A) add a local token endpoint (`POST /api/v1/auth/login`) to the API
 *  - B) implement OIDC/PKCE in the dashboard against an external IdP
 *
 * Whichever is chosen, implement the interface below and pass it to `createApiClient`.
 */

/** Supplies the bearer token for outgoing requests, if any. */
export interface AccessTokenProvider {
  /**
   * Returns the current access token, or `null` when unauthenticated.
   * Called before every request so a refreshed token is picked up.
   */
  getAccessToken(): string | null
}

/**
 * In-memory token store for UI-09. Keeps the bearer token in memory only — it is
 * cleared on page reload and is never written to `localStorage`.
 *
 * Consumers call `setToken` after a successful login and `clearToken` on logout.
 * The optional `onTokenChange` callback fires whenever the token changes, so
 * React components can react (e.g. redirect to `/login` on logout).
 */
export interface MemoryAccessTokenProvider extends AccessTokenProvider {
  /** Replaces the stored token with `token`, or clears it when `null`. */
  setToken(token: string | null): void
  /** Clears the stored token. Alias for `setToken(null)`. */
  clearToken(): void
  /** True when a token is currently stored. */
  isAuthenticated(): boolean
  /** Subscribe to token changes. Returns an unsubscribe function. */
  onTokenChange(callback: (token: string | null) => void): () => void
}

/**
 * Default provider: sends no Authorization header.
 *
 * Fine while the API allows anonymous access, which is the case in development
 * today.
 */
export const anonymousTokenProvider: AccessTokenProvider = {
  getAccessToken: () => null,
}

/**
 * Creates an in-memory access token provider.
 *
 * Tokens live in memory only and vanish on page reload — by design, since the
 * auth strategy (#433) has not been selected and no persistent credential store
 * exists yet.
 */
export function createMemoryAccessTokenProvider(): MemoryAccessTokenProvider {
  let token: string | null = null
  const listeners = new Set<(token: string | null) => void>()

  function notify() {
    for (const cb of listeners) cb(token)
  }

  return {
    getAccessToken: () => token,
    setToken: (next) => {
      token = next
      notify()
    },
    clearToken: () => {
      token = null
      notify()
    },
    isAuthenticated: () => token !== null,
    onTokenChange: (cb) => {
      listeners.add(cb)
      return () => listeners.delete(cb)
    },
  }
}
