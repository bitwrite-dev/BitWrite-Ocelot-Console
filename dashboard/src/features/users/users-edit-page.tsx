import { useQuery } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { useApi } from '@/app-providers'
import { Button } from '@/components/ui/button'
import { ErrorState } from '@/components/page-state'
import { UserForm, type UserFormMode, type UserDraft } from './user-form'
import { useUserMutations } from './queries'

export function UsersEditPage() {
  const { id } = useParams<{ id: string }>()
  const api = useApi()
  const { update } = useUserMutations()
  const navigate = useNavigate()

  const { data: user, isPending, error } = useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: ({ signal }) => api.resources.users.get(id, { signal }),
    enabled: !!id,
  })

  const mode: UserFormMode = 'edit'
  const submitLabel = 'Save changes'

  const draft = user
    ? {
        username: user.username,
        displayName: user.displayName ?? null,
        email: user.email ?? null,
      }
    : null

  const onSubmit = (draft: UserDraft) => {
    update.mutate(
      {
        id,
        body: {
          displayName: draft.displayName,
          email: draft.email,
        },
      },
      {
        onSuccess: () => {
          navigate('/users')
        },
      },
    )
  }

  const onCancel = () => {
    navigate(`/users/${id}`)
  }

  if (isPending) {
    return null
  }

  if (error || !user) {
    return (
      <ErrorState
        title="Could not load user"
        message={
          error instanceof Error ? error.message : String(error)
        }
      />
    )
  }

  return (
    <section className="max-w-2xl">
      <PageHeader title="Edit user" description={`Editing user ${user.username}.`} />
      <UserForm
        mode={mode}
        initial={draft}
        submitLabel={submitLabel}
        pending={update.isPending}
        error={update.error}
        onSubmit={onSubmit}
        onCancel={onCancel}
      />
    </section>
  )
}
