import { useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { ErrorState, LoadingState } from '@/components/page-state'
import { RoleForm, type RoleDraft } from './role-form'
import { usePermissionCatalog, useRole, useRoleMutations } from './queries'

export function RolesEditPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const roleQuery = useRole(id)
  const catalog = usePermissionCatalog()
  const mutations = useRoleMutations()

  const initial = useMemo<RoleDraft | null>(() => {
    if (!roleQuery.data) return null
    return {
      name: roleQuery.data.name,
      description: roleQuery.data.description,
      permissions: roleQuery.data.permissions,
    }
  }, [roleQuery.data])

  if (roleQuery.isPending) return <LoadingState />
  if (roleQuery.isError || !roleQuery.data || !initial) {
    return (
      <ErrorState
        title="Could not load role"
        message={roleQuery.error instanceof Error ? roleQuery.error.message : String(roleQuery.error ?? 'Role not found')}
      />
    )
  }

  const role = roleQuery.data

  const onSubmit = async (draft: RoleDraft) => {
    await mutations.update.mutateAsync({
      id: role.id,
      body: { name: draft.name, description: draft.description },
    })

    const current = new Set(role.permissions)
    const next = new Set(draft.permissions)
    for (const permissionCode of next) {
      if (!current.has(permissionCode)) {
        await mutations.assignPermission.mutateAsync({ id: role.id, permissionCode })
      }
    }
    for (const permissionCode of current) {
      if (!next.has(permissionCode)) {
        await mutations.removePermission.mutateAsync({ id: role.id, permissionCode })
      }
    }

    navigate('/roles')
  }

  const pending =
    mutations.update.isPending ||
    mutations.assignPermission.isPending ||
    mutations.removePermission.isPending

  return (
    <section className="max-w-6xl">
      <PageHeader title="Edit role" description={`Update ${role.name} and its permission assignments.`} />
      <RoleForm
        initial={initial}
        submitLabel="Save changes"
        pending={pending}
        error={mutations.update.error ?? mutations.assignPermission.error ?? mutations.removePermission.error}
        permissions={catalog.data ?? []}
        permissionsPending={catalog.isPending}
        permissionsError={catalog.error}
        onSubmit={(draft) => {
          void onSubmit(draft)
        }}
        onCancel={() => navigate('/roles')}
      />
    </section>
  )
}
