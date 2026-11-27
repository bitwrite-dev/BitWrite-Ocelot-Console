import { useNavigate } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { RoleForm, type RoleDraft } from './role-form'
import { usePermissionCatalog, useRoleMutations } from './queries'

export function RolesCreatePage() {
  const navigate = useNavigate()
  const mutations = useRoleMutations()
  const catalog = usePermissionCatalog()

  const onSubmit = async (draft: RoleDraft) => {
    const role = await mutations.create.mutateAsync({
      name: draft.name,
      description: draft.description,
    })

    for (const permissionCode of draft.permissions) {
      await mutations.assignPermission.mutateAsync({ id: role.id, permissionCode })
    }

    navigate('/roles')
  }

  return (
    <section className="max-w-6xl">
      <PageHeader title="Create role" description="Define a role and assign catalog permissions." />
      <RoleForm
        initial={null}
        submitLabel="Create role"
        pending={mutations.create.isPending || mutations.assignPermission.isPending}
        error={mutations.create.error ?? mutations.assignPermission.error}
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
