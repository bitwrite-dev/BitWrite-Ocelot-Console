import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import { useApi } from '@/app-providers'
import type { CreateRoleRequest, RoleResponse, UpdateRoleRequest } from '@/api'

export const roleKeys = {
  all: ['roles'] as const,
  detail: (id: string) => ['roles', 'detail', id] as const,
  catalog: ['roles', 'permissions', 'catalog'] as const,
}

export function useRoles() {
  const api = useApi()

  return useQuery({
    queryKey: roleKeys.all,
    queryFn: ({ signal }) => api.resources.roles.list({ signal }) as Promise<RoleResponse[]>,
  })
}

export function useRole(id: string) {
  const api = useApi()
  return useQuery({
    queryKey: roleKeys.detail(id),
    queryFn: ({ signal }) => api.resources.roles.get(id, { signal }) as Promise<RoleResponse>,
    enabled: id.length > 0,
  })
}

export function usePermissionCatalog() {
  const api = useApi()
  return useQuery({
    queryKey: roleKeys.catalog,
    queryFn: ({ signal }) => api.resources.roles.catalog({ signal }),
  })
}

export function useRoleMutations() {
  const api = useApi()
  const queryClient = useQueryClient()

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: roleKeys.all })
  }

  return {
    create: useMutation({
      mutationFn: (body: CreateRoleRequest) => api.resources.roles.create(body),
      onSuccess: invalidate,
    }),
    update: useMutation({
      mutationFn: ({ id, body }: { id: string; body: UpdateRoleRequest }) =>
        api.resources.roles.update(id, body),
      onSuccess: async (_data, variables) => {
        await invalidate()
        await queryClient.invalidateQueries({ queryKey: roleKeys.detail(variables.id) })
      },
    }),
    activate: useMutation({
      mutationFn: (id: string) => api.resources.roles.activate(id),
      onSuccess: invalidate,
    }),
    deactivate: useMutation({
      mutationFn: (id: string) => api.resources.roles.deactivate(id),
      onSuccess: invalidate,
    }),
    assignPermission: useMutation({
      mutationFn: ({ id, permissionCode }: { id: string; permissionCode: string }) =>
        api.resources.roles.assignPermission(id, { permissionCode }),
    }),
    removePermission: useMutation({
      mutationFn: ({ id, permissionCode }: { id: string; permissionCode: string }) =>
        api.resources.roles.removePermission(id, permissionCode),
    }),
  }
}
