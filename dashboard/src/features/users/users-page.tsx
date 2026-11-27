import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { DataTable, type Column } from '@/components/data-table'
import { EmptyState, ErrorState } from '@/components/page-state'
import { Button } from '@/components/ui/button'
import type { UserResponse } from '@/api'
import { useUserMutations, useUsers } from './queries'

const PAGE = 'page'
const PAGE_SIZE = 'pageSize'

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100]

export function UsersPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const page = Math.max(1, Number(searchParams.get(PAGE) ?? 1))
  const pageSize = Number(searchParams.get(PAGE_SIZE) ?? 20)

  const users = useUsers(page, pageSize)
  const mutations = useUserMutations()

  const [editingId, setEditingId] = useState<string | null>(null)
  const [disabling, setDisabling] = useState<string | null>(null)
  const [enableError, setEnableError] = useState<string | null>(null)
  const [disableError, setDisableError] = useState<string | null>(null)

  const updateParams = (patch: Record<string, string | number>) => {
    const next = new URLSearchParams(searchParams)
    for (const [key, value] of Object.entries(patch)) next.set(key, String(value))
    setSearchParams(next)
  }

  const rows = users.data?.users ?? []
  const anyPending =
    mutations.create.isPending || mutations.update.isPending || mutations.enable.isPending ||
    mutations.disable.isPending

  const startEdit = (user: UserResponse) => {
    setEditingId(user.id)
    navigate(`/users/${user.id}/edit`)
  }

  const handleEnable = (user: UserResponse) => {
    setDisabling(user.id)
    setEnableError(null)
    mutations.enable.mutate(user.id, {
      onSuccess: () => setDisabling(null),
      onError: (err) => {
        setDisabling(null)
        setEnableError(err instanceof Error ? err.message : String(err))
      },
    })
  }

  const handleDisable = (user: UserResponse) => {
    setDisabling(user.id)
    setDisableError(null)
    mutations.disable.mutate(user.id, {
      onSuccess: () => setDisabling(null),
      onError: (err) => {
        setDisabling(null)
        setDisableError(err instanceof Error ? err.message : String(err))
      },
    })
  }

  const columns: Column<UserResponse>[] = [
    {
      key: 'username',
      header: 'Username',
      cell: (row) => <span className="font-medium">{row.username}</span>,
    },
    {
      key: 'displayName',
      header: 'Display Name',
      cell: (row) =>
        row.displayName ? (
          <span className="text-sm text-muted-foreground">{row.displayName}</span>
        ) : (
          <span className="text-muted-foreground">—</span>
        ),
    },
    {
      key: 'email',
      header: 'Email',
      cell: (row) =>
        row.email ? (
          <span className="text-sm text-muted-foreground">{row.email}</span>
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
      key: 'roles',
      header: 'Roles',
      cell: (row) => (
        <span className="text-sm text-muted-foreground">{row.roleIds.join(', ') || '—'}</span>
      ),
    },
    {
      key: 'createdAt',
      header: 'Created',
      cell: (row) => <span className="text-sm text-muted-foreground">{row.createdAt}</span>,
    },
    {
      key: 'actions',
      header: 'Actions',
      className: 'text-right',
      cell: (row) => (
        <div
          className="flex justify-end gap-2"
          onClick={(event) => event.stopPropagation()}
        >
          <Button variant="outline" size="sm" disabled={anyPending} onClick={() => startEdit(row)}>
            Edit
          </Button>
          {row.status.toLowerCase() === 'active' ? (
            <Button
              variant="outline"
              size="sm"
              disabled={anyPending || disabling === row.id}
              onClick={() => handleDisable(row)}
            >
              Disable
            </Button>
          ) : (
            <Button
              variant="outline"
              size="sm"
              disabled={anyPending || disabling === row.id}
              onClick={() => handleEnable(row)}
            >
              Enable
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Users"
        description="Manage console users and their roles."
        actions={
          <Button onClick={() => navigate('/users/new')} disabled={anyPending}>
            New user
          </Button>
        }
      />

      {users.isError ? (
        <ErrorState
          title="Could not load users"
          message={
            users.error instanceof Error ? users.error.message : String(users.error)
          }
        />
      ) : null}

      {rows.length === 0 && !users.isPending && !users.isError ? (
        <EmptyState
          title="No users yet"
          description="There are no users in the control plane."
          action={
            <Button onClick={() => navigate('/users/new')}>
              New user
            </Button>
          }
        />
      ) : (
        <DataTable
          caption="Users known to the control plane"
          columns={columns}
          rows={rows}
          rowKey={(row) => row.id}
          totalCount={users.data?.totalCount ?? 0}
          page={users.data?.page ?? page}
          pageSize={users.data?.pageSize ?? pageSize}
          onPageChange={(next) => updateParams({ [PAGE]: next })}
          pageSizeOptions={PAGE_SIZE_OPTIONS}
          isLoading={users.isPending}
          onRowClick={(row) => navigate(`/users/${row.id}`)}
        />
      )}

      {mutations.create.isError || mutations.update.isError || mutations.enable.isError ||
      mutations.disable.isError ? (
        <div className="mt-4">
          <ErrorState
            title="Operation failed"
            message={
              (mutations.create.error ??
                mutations.update.error ??
                mutations.enable.error ??
                mutations.disable.error) instanceof Error
                ? (mutations.create.error ??
                    mutations.update.error ??
                    mutations.enable.error ??
                    mutations.disable.error).message
                : String(
                    mutations.create.error ??
                      mutations.update.error ??
                      mutations.enable.error ??
                      mutations.disable.error,
                  )
            }
          />
        </div>
      ) : null}
    </>
  )
}
