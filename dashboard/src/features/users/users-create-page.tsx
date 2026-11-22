import { useNavigate } from 'react-router-dom'

import { PageHeader } from '@/components/app-layout'
import { Button } from '@/components/ui/button'
import { UserForm, type UserFormMode, type UserDraft } from './user-form'
import { useUserMutations } from './queries'

export function UsersCreatePage() {
  const { create } = useUserMutations()
  const navigate = useNavigate()

  const mode: UserFormMode = 'create'
  const submitLabel = 'Create user'

  const onSubmit = (draft: UserDraft) => {
    create.mutate(
      {
        username: draft.username,
        displayName: draft.displayName,
        email: draft.email,
        password: draft.password,
      },
      {
        onSuccess: () => {
          navigate('/users')
        },
      },
    )
  }

  const onCancel = () => {
    navigate('/users')
  }

  return (
    <section className="max-w-2xl">
      <PageHeader title="Create user" description="Add a new user to the control plane." />
      <UserForm
        mode={mode}
        initial={null}
        submitLabel={submitLabel}
        pending={create.isPending}
        error={create.error}
        onSubmit={onSubmit}
        onCancel={onCancel}
      />
    </section>
  )
}
