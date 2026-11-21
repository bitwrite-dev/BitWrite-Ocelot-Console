import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'

import { createApiClient, type ApiClient } from '@/api'
import { createMemoryAccessTokenProvider, type MemoryAccessTokenProvider } from '@/api/auth'

/**
 * The API client and query cache are wired once, here, and provided through
 * context so components never import a module-level singleton.
 *
 * The client is created per QueryClient so tests can build an isolated cache and
 * inject a stub `fetchImpl`.
 *
 * Authentication uses an in-memory token provider (UI-09): tokens live in
 * memory only and are cleared on page reload.
 */
export function createAppProviders({
  client,
  children,
}: {
  client?: ApiClient
  children: ReactNode
}) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        // Overview polls nothing, but gateway and publication state change on the
        // order of seconds once gateways report in, so a short stale window keeps
        // the dashboard feeling live without hammering Redis.
        staleTime: 10_000,
        retry: 1,
        refetchOnWindowFocus: true,
      },
    },
  })

  const tokenProvider = createMemoryAccessTokenProvider()
  const api = client ?? createApiClient({ tokenProvider })

  return (
    <QueryClientProvider client={queryClient}>
      <ApiClientContext.Provider value={api}>
        <AuthProviderContext.Provider value={tokenProvider}>{children}</AuthProviderContext.Provider>
      </ApiClientContext.Provider>
    </QueryClientProvider>
  )
}

import React, { createContext, useContext } from 'react'

/**
 * Exported so tests can inject a client with a stubbed `fetchImpl`. Application
 * code should use the `useApi` hook rather than reaching for the context.
 */
export const ApiClientContext = createContext<ApiClient | null>(null)

/** The shared API client. Throws if used outside `createAppProviders`. */
export function useApi(): ApiClient {
  const api = useContext(ApiClientContext)
  if (!api) {
    throw new Error('useApi must be used within createAppProviders')
  }
  return api
}

/**
 * Auth context: exposes the in-memory token provider so the login page can
 * call setToken/clearToken and components can react to login/logout via
 * onTokenChange.
 */
export const AuthProviderContext = createContext<MemoryAccessTokenProvider | null>(null)

export function useAuth() {
  const provider = useContext(AuthProviderContext)
  if (!provider) {
    return { login: () => {}, logout: () => {}, isAuthenticated: () => false, setUsername: () => {}, getUsername: () => null }
  }
  const navigate = useNavigate()

  const login = (token: string) => provider.setToken(token)
  const logout = () => {
    provider.clearToken()
    navigate('/login', { replace: true })
  }
  const isAuthenticated = () => provider.isAuthenticated()
  const setUsername = (username: string | null) => provider.setUsername(username)
  const getUsername = () => provider.getUsername()

  return { login, logout, isAuthenticated, setUsername, getUsername }
}

/**
 * Auth guard: redirects to /login when no bearer token is present.
 * Wraps shell routes so unauthenticated users never see the dashboard.
 */
export function AuthGuard({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()

  useEffect(() => {
    if (!isAuthenticated()) {
      navigate('/login', { replace: true })
    }
  }, [isAuthenticated, navigate])

  if (!isAuthenticated()) return null

  return <>{children}</>
}
