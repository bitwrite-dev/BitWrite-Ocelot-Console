import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import { useApi } from '@/app-providers'
import { ApiError } from '@/api'
import type {
  CreateUserRequest,
  GetUserSessionsResponse,
  ListUsersResponse,
  RevokeSessionResponse,
  SessionInfo,
  UpdateUserRequest,
  UserResponse,
} from '@/api'

/**
 * Query hooks for the Users page.
 *
 * `GET /api/v1/users` takes only `page` and `pageSize`, so paging is
 * server-side and nothing else is filtered here.
 */

export const userKeys = {
  list: (page: number, pageSize: number) => ['users', 'list', page, pageSize] as const,
  detail: (id: string) => ['users', 'detail', id] as const,
  sessions: (userId: string) => ['users', 'sessions', userId] as const,
}

export function useUsers(page: number, pageSize: number) {
  const api = useApi()

  return useQuery({
    queryKey: userKeys.list(page, pageSize),
    queryFn: ({ signal }) =>
      api.resources.users.list({ page, pageSize, signal }) as Promise<ListUsersResponse>,
    placeholderData: (previous) => previous,
  })
}

/**
 * Fetch all active sessions for a given user.
 */
export function useUserSessions(userId: string) {
  const api = useApi()

  return useQuery({
    queryKey: userKeys.sessions(userId),
    queryFn: ({ signal }) =>
      api.resources.sessions.list(userId, { signal }) as Promise<GetUserSessionsResponse>,
    enabled: !!userId,
  })
}

/**
 * Mutations for user administration. Any change invalidates the users list so
 * derived views reflect the updated state.
 */
export function useUserMutations() {
  const api = useApi()
  const queryClient = useQueryClient()

  const invalidate = () =>
    queryClient.refetchQueries({ queryKey: ['users'], exact: false })

  return {
    create: useMutation({
      mutationFn: (body: CreateUserRequest) => api.resources.users.create(body),
      onSuccess: invalidate,
    }),
    update: useMutation({
      mutationFn: ({ id, body }: { id: string; body: UpdateUserRequest }) =>
        api.resources.users.update(id, body),
      onSuccess: invalidate,
    }),
    enable: useMutation({
      mutationFn: (id: string) => api.resources.users.enable(id),
      onSuccess: invalidate,
    }),
    disable: useMutation({
      mutationFn: (id: string) => api.resources.users.disable(id),
      onSuccess: invalidate,
    }),
    revokeSession: useMutation({
      mutationFn: (sessionId: string) => api.resources.sessions.revoke(sessionId),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['users', 'sessions'] })
      },
    }),
  }
}

/**
 * Reads a stored user into the form state.
 */
export function userDraftFrom(user: UserResponse) {
  return {
    username: user.username,
    displayName: user.displayName ?? null,
    email: user.email ?? null,
  }
}

/**
 * Per-field errors from a rejected user request.
 *
 * The API answers with a `ValidationProblemDetails`, so the errors are keyed by
 * field and the form can point at the input at fault.
 */
export function toUserFieldErrors(error: unknown): Record<string, string[]> {
  if (error instanceof ApiError && error.fieldErrors) {
    return error.fieldErrors
  }
  return {}
}

export function userErrorTitle(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.isNetworkError) return 'Cannot reach the control plane API'
    return error.isValidation ? 'The user was rejected' : `Request failed (${error.status})`
  }
  return 'Something went wrong'
}

export function userErrorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  if (error instanceof Error) return error.message
  return String(error)
}
