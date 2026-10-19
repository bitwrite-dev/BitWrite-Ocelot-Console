/**
 * The environment the dashboard operates on, as the sidebar selector stores it.
 *
 * One module owns the storage key and the offered names, because three callers
 * have to agree on them: the selector that writes the choice, the HTTP client
 * that sends it as `X-Environment`, and the publish dialog that has to name the
 * same environment in its request body. A selector that only persists a choice
 * nothing reads leaves every request speaking for whatever the build was
 * configured with, which is how a publication ends up naming an environment
 * nobody chose.
 */

const STORAGE_KEY = 'dashboard:environment'

/**
 * The environments the selector offers.
 *
 * The control plane keeps routes, services and snapshots per environment, and
 * these are the ones it runs with. Widening the list is a deliberate act: an
 * environment the API has no data for answers with an empty configuration
 * rather than an error, which reads as "configured" instead of "wrong name".
 */
export const ENVIRONMENTS = ['development', 'staging', 'production'] as const

export type EnvironmentOption = (typeof ENVIRONMENTS)[number]

/**
 * The stored choice, or '' when there is none.
 *
 * Read per request rather than captured once, so a client built before the
 * operator chose does not keep sending the stale value. localStorage throws in
 * some privacy configurations; that reads as "not selected" and leaves the
 * configured environment in charge rather than breaking every request.
 */
export function readSelectedEnvironment(): string {
  try {
    return window.localStorage.getItem(STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}

/** The operator's choice, for later requests to pick up. */
export function storeSelectedEnvironment(value: string): void {
  try {
    window.localStorage.setItem(STORAGE_KEY, value)
  } catch {
    // A refused write leaves the selector showing a choice the next request
    // will not send, but throwing here would break the select itself, which
    // is the operator's only way to act at all.
  }
}
