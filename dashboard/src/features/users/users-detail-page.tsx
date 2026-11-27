import { useQuery } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { useApi } from '@/app-providers'
import { Button } from '@/components/ui/button'
import { ErrorState } from '@/components/page-state'
import type { SessionInfo } from '@/api'
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
  const { revokeSession } = useUserMutations()
  const navigate = useNavigate()

  const { data: user, isPending: userPending, error: userError } = useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: ({ signal }) => api.resources.users.get(id, { signal }),
    enabled: !!id,
  })

  const { data: sessions, isPending: sessionsPending, error: sessionsError } =
    useUserSessions(id ?? '')

  if (userPending || sessionsPending) {
    return null
  }

  if (userError) {
    return (
      <ErrorState
        title="User not found"
        message={
          userError instanceof Error ? userError.message : String(userError)
        }
      />
    )
  }

  // A 404 on sessions means the user simply has no active sessions — not an error.
  const sessionRows = sessions?.sessions ?? []

  const handleRevoke = (sessionId: string) => {
    revokeSession.mutate(sessionId)
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
            <dd className="font-mono text-muted-foreground truncate">
              {user.roleIds.join(', ') || '—'}
            </dd>
            <p className="text-xs text-muted-foreground mt-1">
              Role IDs shown. Role names will appear once role management UI is
              built (UI-05).
            </p>
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
