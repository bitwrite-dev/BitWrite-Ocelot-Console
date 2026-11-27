import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { DataTable, type Column } from '@/components/data-table'
import { EmptyState, ErrorState } from '@/components/page-state'
import { Button } from '@/components/ui/button'
import type { RoleResponse } from '@/api'
import { useRoleMutations, useRoles } from './queries'

export function RolesPage() {
  const roles = useRoles()
  const mutations = useRoleMutations()
  const navigate = useNavigate()
  const [pendingId, setPendingId] = useState<string | null>(null)

  const rows = roles.data ?? []
  const anyPending = mutations.activate.isPending || mutations.deactivate.isPending

  const handleActivate = (role: RoleResponse) => {
    setPendingId(role.id)
    mutations.activate.mutate(role.id, { onSettled: () => setPendingId(null) })
  }

  const handleDeactivate = (role: RoleResponse) => {
    setPendingId(role.id)
    mutations.deactivate.mutate(role.id, { onSettled: () => setPendingId(null) })
  }

  const columns: Column<RoleResponse>[] = [
    {
      key: 'name',
      header: 'Name',
      cell: (row) => (
        <div className="flex items-center gap-2">
          <span className="font-medium">{row.name}</span>
          {row.isSystemRole ? (
            <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium uppercase text-muted-foreground">
              System
            </span>
          ) : null}
        </div>
      ),
    },
    {
      key: 'description',
      header: 'Description',
      cell: (row) =>
        row.description ? (
          <span className="text-sm text-muted-foreground">{row.description}</span>
        ) : (
          <span className="text-muted-foreground">—</span>
        ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => (
        <span
          className={`font-mono text-xs ${
            row.status.toLowerCase() === 'active' ? 'text-green-600' : 'text-red-600'
          }`}
        >
          {row.status}
        </span>
      ),
    },
    {
      key: 'permissions',
      header: 'Permissions',
      cell: (row) => (
        <span className="text-sm text-muted-foreground">{row.permissions.length}</span>
      ),
    },
    {
      key: 'actions',
      header: 'Actions',
      className: 'text-right',
      cell: (row) => (
        <div className="flex justify-end gap-2" onClick={(event) => event.stopPropagation()}>
          <Button variant="outline" size="sm" disabled={anyPending} onClick={() => navigate(`/roles/${row.id}/edit`)}>
            Edit
          </Button>
          {row.status.toLowerCase() === 'active' ? (
            <Button
              variant="outline"
              size="sm"
              disabled={anyPending || pendingId === row.id}
              onClick={() => handleDeactivate(row)}
            >
              Deactivate
            </Button>
          ) : (
            <Button
              variant="outline"
              size="sm"
              disabled={anyPending || pendingId === row.id}
              onClick={() => handleActivate(row)}
            >
              Activate
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Roles"
        description="Manage roles and permission assignments."
        actions={<Button onClick={() => navigate('/roles/new')} disabled={anyPending}>New role</Button>}
      />

      {roles.isError ? (
        <ErrorState
          title="Could not load roles"
          message={roles.error instanceof Error ? roles.error.message : String(roles.error)}
        />
      ) : null}

      {rows.length === 0 && !roles.isPending && !roles.isError ? (
        <EmptyState
          title="No roles yet"
          description="Roles will appear here once they are created."
          action={<Button onClick={() => navigate('/roles/new')}>New role</Button>}
        />
      ) : (
        <DataTable
          caption="Roles configured in the control plane"
          columns={columns}
          rows={rows}
          rowKey={(row) => row.id}
          totalCount={rows.length}
          page={1}
          pageSize={rows.length || 1}
          onPageChange={() => undefined}
          isLoading={roles.isPending}
        />
      )}

      {mutations.activate.isError || mutations.deactivate.isError ? (
        <div className="mt-4">
          <ErrorState
            title="Operation failed"
            message={
              (mutations.activate.error ?? mutations.deactivate.error) instanceof Error
                ? (mutations.activate.error ?? mutations.deactivate.error).message
                : String(mutations.activate.error ?? mutations.deactivate.error)
            }
          />
        </div>
      ) : null}
    </>
  )
}
