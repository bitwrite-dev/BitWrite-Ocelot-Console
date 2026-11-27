import { useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { LoadingState } from '@/components/page-state'
import { QueryErrorState } from '@/components/query-error-state'
import { Button } from '@/components/ui/button'
import { usePermissionCatalog, useRole, useRoleMutations } from './queries'

function formatCategory(category: string): string {
  if (category === 'GlobalConfiguration') return 'Global Configuration'
  return category.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function RolesDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const roleQuery = useRole(id)
  const catalog = usePermissionCatalog()
  const mutations = useRoleMutations()

  const permissionDetails = useMemo(() => {
    const byCode = new Map((catalog.data ?? []).map((p) => [p.code, p]))
    return (roleQuery.data?.permissions ?? []).map((code) => {
      const def = byCode.get(code)
      return {
        code,
        name: def?.name ?? code,
        description: def?.description ?? null,
        category: formatCategory(def?.category ?? 'Uncategorized'),
        status: def?.status ?? 'Unknown',
      }
    })
  }, [catalog.data, roleQuery.data?.permissions])

  if (roleQuery.isPending) return <LoadingState />
  if (roleQuery.isError || !roleQuery.data) {
    return <QueryErrorState error={roleQuery.error ?? new Error('Role not found')} title="Could not load role" />
  }

  const role = roleQuery.data
  const isActive = role.status.toLowerCase() === 'active'
  const pending = mutations.activate.isPending || mutations.deactivate.isPending

  return (
    <section className="max-w-6xl space-y-6">
      <PageHeader
        title={role.name}
        description="Role details and assigned permissions."
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate('/roles')}>Back</Button>
            <Button variant="outline" onClick={() => navigate(`/roles/${role.id}/edit`)} disabled={pending}>Edit</Button>
            {isActive ? (
              <Button variant="outline" disabled={pending} onClick={() => mutations.deactivate.mutate(role.id)}>Deactivate</Button>
            ) : (
              <Button variant="outline" disabled={pending} onClick={() => mutations.activate.mutate(role.id)}>Activate</Button>
            )}
          </div>
        }
      />

      <div className="grid gap-4 rounded-md border p-4 md:grid-cols-2 xl:grid-cols-4">
        <div><p className="text-xs text-muted-foreground">Status</p><p className="font-medium">{role.status}</p></div>
        <div><p className="text-xs text-muted-foreground">System role</p><p className="font-medium">{role.isSystemRole ? 'Yes' : 'No'}</p></div>
        <div><p className="text-xs text-muted-foreground">Created</p><p className="font-medium">{role.createdAt}</p></div>
        <div><p className="text-xs text-muted-foreground">Updated</p><p className="font-medium">{role.updatedAt ?? '—'}</p></div>
        <div className="md:col-span-2 xl:col-span-4"><p className="text-xs text-muted-foreground">Description</p><p className="font-medium">{role.description || '—'}</p></div>
      </div>

      <section className="space-y-3 rounded-md border p-4">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-sm font-semibold">Permissions</h2>
          <span className="text-xs text-muted-foreground">{permissionDetails.length} assigned</span>
        </div>
        {permissionDetails.length === 0 ? (
          <p className="text-sm text-muted-foreground">No permissions assigned.</p>
        ) : (
          <div className="grid gap-3 md:grid-cols-2 2xl:grid-cols-3">
            {permissionDetails.map((permission) => (
              <div key={permission.code} className="rounded-md border bg-muted/10 p-3">
                <p className="text-xs text-muted-foreground">{permission.category}</p>
                <p className="text-sm font-medium">{permission.name}</p>
                {permission.description ? <p className="text-xs text-muted-foreground">{permission.description}</p> : null}
                <p className="mt-2 text-xs text-muted-foreground">Status: {permission.status}</p>
              </div>
            ))}
          </div>
        )}
      </section>

      {mutations.activate.isError || mutations.deactivate.isError ? (
        <QueryErrorState
          error={mutations.activate.error ?? mutations.deactivate.error}
          title="Operation failed"
        />
      ) : null}
    </section>
  )
}
