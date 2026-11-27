import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import { useApi } from '@/app-providers'
import type { RoleResponse } from '@/api'

export const roleKeys = {
  all: ['roles'] as const,
  detail: (id: string) => ['roles', 'detail', id] as const,
}

export function useRoles() {
  const api = useApi()

  return useQuery({
    queryKey: roleKeys.all,
    queryFn: ({ signal }) => api.resources.roles.list({ signal }) as Promise<RoleResponse[]>,
  })
}

export function useRoleMutations() {
  const api = useApi()
  const queryClient = useQueryClient()

  const invalidate = () => queryClient.invalidateQueries({ queryKey: roleKeys.all })

  return {
    activate: useMutation({
      mutationFn: (id: string) => api.resources.roles.activate(id),
      onSuccess: invalidate,
    }),
    deactivate: useMutation({
      mutationFn: (id: string) => api.resources.roles.deactivate(id),
      onSuccess: invalidate,
    }),
  }
}
