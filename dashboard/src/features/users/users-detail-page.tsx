import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { useApi } from '@/app-providers'
import { Button } from '@/components/ui/button'
import { QueryErrorState } from '@/components/query-error-state'
import { useRoles } from '@/features/roles/queries'
import { useUserSessions, useUserMutations } from './queries'

/** Formats an ISO date string, returning '—' for zero/empty dates. */
function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const d = new Date(value)
  if (isNaN(d.getTime()) || d.getFullYear() < 1000) return '—'
  return value
}

export function UsersDetailPage() {
  const { id } = useParams<{ id: string }>()
  const api = useApi()
  const mutations = useUserMutations()
  const navigate = useNavigate()
  const rolesQuery = useRoles()
  const [roleToAdd, setRoleToAdd] = useState('')

  const { data: user, isPending: userPending, error: userError } = useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: ({ signal }) => api.resources.users.get(id, { signal }),
    enabled: !!id,
  })

  const { data: sessions, isPending: sessionsPending } = useUserSessions(id ?? '')

  const roleNameById = useMemo(() => {
    const map = new Map<string, string>()
    for (const role of rolesQuery.data ?? []) map.set(role.id, role.name)
    return map
  }, [rolesQuery.data])

  if (userPending || sessionsPending) {
    return null
  }

  if (userError || !user) {
    return <QueryErrorState error={userError ?? new Error('User not found')} title="User not found" />
  }

  const sessionRows = sessions?.sessions ?? []
  const assignedRoles = user.roleIds ?? []
  const availableRoles = (rolesQuery.data ?? []).filter((r) => !assignedRoles.includes(r.id) && r.status.toLowerCase() === 'active')

  const handleRevoke = (sessionId: string) => {
    mutations.revokeSession.mutate(sessionId)
  }

  return (
    <section className="max-w-4xl">
      <PageHeader
        title="User detail"
        description={`User ${user.username}`}
        actions={
          <Button onClick={() => navigate('/users')}>
            Back to users
          </Button>
        }
      />

      <div className="mb-6 rounded-lg border bg-background p-5">
        <h2 className="font-heading mb-4 text-lg font-semibold">User</h2>
        <dl className="grid grid-cols-2 gap-x-6 gap-y-4 text-sm">
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Username</dt>
            <dd className="font-medium">{user.username}</dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Display name</dt>
            <dd className="font-medium">{user.displayName ?? '—'}</dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Email</dt>
            <dd className="font-medium">{user.email ?? '—'}</dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Status</dt>
            <dd className={`font-medium ${
              user.status.toLowerCase() === 'active' ? 'text-green-600' : 'text-red-600'
            }`}>
              {user.status}
            </dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">User type</dt>
            <dd className="font-medium text-muted-foreground">{user.userType}</dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Created</dt>
            <dd className="font-mono text-muted-foreground">
              {formatDate(user.createdAt)}
            </dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Updated</dt>
            <dd className="font-mono text-muted-foreground">
              {formatDate(user.updatedAt)}
            </dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Roles</dt>
            <dd className="space-y-2">
              <div className="flex flex-wrap gap-2">
                {assignedRoles.length === 0 ? <span className="text-muted-foreground">—</span> : null}
                {assignedRoles.map((roleId) => (
                  <span key={roleId} className="inline-flex items-center gap-2 rounded-md border px-2 py-1 text-xs">
                    <span className="font-medium">{roleNameById.get(roleId) ?? roleId}</span>
                    <button
                      type="button"
                      className="text-muted-foreground hover:text-foreground"
                      disabled={mutations.removeRole.isPending}
                      onClick={() => mutations.removeRole.mutate({ id: user.id, roleId })}
                    >
                      Remove
                    </button>
                  </span>
                ))}
              </div>
              <div className="flex gap-2">
                <select
                  className="h-9 rounded-md border bg-background px-2 text-sm"
                  value={roleToAdd}
                  onChange={(e) => setRoleToAdd(e.target.value)}
                >
                  <option value="">Select role…</option>
                  {availableRoles.map((role) => (
                    <option key={role.id} value={role.id}>{role.name}</option>
                  ))}
                </select>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!roleToAdd || mutations.assignRole.isPending}
                  onClick={() => {
                    if (!roleToAdd) return
                    mutations.assignRole.mutate({ id: user.id, roleId: roleToAdd }, { onSuccess: () => setRoleToAdd('') })
                  }}
                >
                  Assign
                </Button>
              </div>
            </dd>
          </div>
          <div className="col-span-2">
            <dt className="font-semibold text-muted-foreground">Last authenticated</dt>
            <dd className="font-mono text-muted-foreground">
              {formatDate(user.lastAuthenticatedAt)}
            </dd>
          </div>
        </dl>
      </div>

      <div className="rounded-lg border border-border bg-background p-5">
        <h2 className="font-heading mb-4 text-lg font-semibold">Sessions</h2>
        <p className="text-sm text-muted-foreground mb-4">
          Active sessions for this user. Revoke a session to immediately end it.
        </p>

        {sessionRows.length === 0 ? (
          <div className="rounded-lg border border-dashed p-4 text-center">
            <p className="text-sm text-muted-foreground">No active sessions.</p>
          </div>
        ) : (
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-border">
                <th className="pb-2 text-sm font-semibold text-muted-foreground">
                  Session ID
                </th>
                <th className="pb-2 text-sm font-semibold text-muted-foreground">
                  Created
                </th>
                <th className="pb-2 text-sm font-semibold text-muted-foreground">
                  Expires
                </th>
                <th className="pb-2 text-sm font-semibold text-muted-foreground">
                  Last accessed
                </th>
                <th className="pb-2 text-sm font-semibold text-muted-foreground text-right">
                  Action
                </th>
              </tr>
            </thead>
            <tbody>
              {sessionRows.map((session) => (
                <tr
                  key={session.sessionId}
                  className="border-b border-border last:border-0"
                >
                  <td className="py-3 font-mono text-sm">
                    {session.sessionId}
                  </td>
                  <td className="py-3 text-sm text-muted-foreground">
                    {formatDate(session.createdAt)}
                  </td>
                  <td className="py-3 text-sm text-muted-foreground">
                    {formatDate(session.expiresAt)}
                  </td>
                  <td className="py-3 text-sm text-muted-foreground">
                    {formatDate(session.lastAccessedAt)}
                  </td>
                  <td className="py-3 text-sm text-right">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleRevoke(session.sessionId)}
                    >
                      Revoke
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </section>
  )
}
