import { useState } from 'react'

import { ErrorState } from '@/components/page-state'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
  toUserFieldErrors,
  userErrorTitle,
  userErrorMessage,
  type UserResponse,
} from './queries'

export type UserFormMode = 'create' | 'edit'

/** Editable form state shared by create and edit. */
export interface UserDraft {
  username: string
  displayName: string | null
  email: string | null
  password: string | null
}

/** Client-side validation for the user form. */
export function validateUserDraft(draft: UserDraft, mode: UserFormMode): Record<string, string[]> {
  const errors: Record<string, string[]> = {}

  if (mode === 'create') {
    if (draft.username.trim().length === 0) {
      errors.username = ['Username is required']
    } else if (draft.username.trim().length > 100) {
      errors.username = ['Username must be 100 characters or fewer']
    }

    if (draft.password?.trim().length === 0) {
      errors.password = ['Password is required']
    } else if (draft.password?.trim().length < 8) {
      errors.password = ['Password must be at least 8 characters']
    }
  }

  if (draft.displayName !== null && draft.displayName.length > 200) {
    errors.displayName = ['Display name must be 200 characters or fewer']
  }

  if (draft.email !== null && draft.email.length > 0) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(draft.email)) {
      errors.email = ['Enter a valid email address']
    }
  }

  return errors
}

export function hasErrors(errors: Record<string, string[]>): boolean {
  return Object.keys(errors).length > 0
}

/**
 * Create or edit form for a user.
 *
 * Create accepts username, display name and email. Edit keeps username
 * read-only (the API only allows display name and email to change) and lets
 * the operator edit those two fields.
 */
export function UserForm({
  mode,
  initial,
  submitLabel,
  pending,
  error,
  onSubmit,
  onCancel,
}: {
  mode: UserFormMode
  initial: UserDraft | null
  submitLabel: string
  pending: boolean
  error: unknown
  onSubmit: (draft: UserDraft) => void
  onCancel: () => void
}) {
  const [draft, setDraft] = useState<UserDraft>(
    initial ?? { username: '', displayName: null, email: null, password: null },
  )
  const [showErrors, setShowErrors] = useState(false)

  const clientErrors = validateUserDraft(draft, mode)
  // Server errors win: they are the authority on what was actually rejected.
  const serverErrors = toUserFieldErrors(error)
  const errors = hasErrors(serverErrors) ? serverErrors : showErrors ? clientErrors : {}

  const usernameReadOnly = mode === 'edit'

  return (
    <form
      className="space-y-5"
      onSubmit={(event) => {
        event.preventDefault()
        setShowErrors(true)
        if (hasErrors(clientErrors)) return
        onSubmit(draft)
      }}
    >
      {mode === 'create' ? (
        <>
          <div className="space-y-1.5">
            <Label htmlFor="user-username">Username</Label>
            <Input
              id="user-username"
              value={draft.username}
              onChange={(event) =>
                setDraft((current) => ({ ...current, username: event.target.value }))
              }
            />
            {errors.username?.map((message) => (
              <p key={message} className="text-xs text-destructive">{message}</p>
            ))}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="user-password">Password</Label>
            <Input
              id="user-password"
              type="password"
              value={draft.password ?? ''}
              onChange={(event) =>
                setDraft((current) => ({ ...current, password: event.target.value }))
              }
            />
            {errors.password?.map((message) => (
              <p key={message} className="text-xs text-destructive">{message}</p>
            ))}
          </div>
        </>
      ) : null}

      <div className="space-y-1.5">
        <Label htmlFor="user-display-name">Display name</Label>
        <Input
          id="user-display-name"
          value={draft.displayName ?? ''}
          onChange={(event) =>
            setDraft((current) => ({
              ...current,
              displayName: event.target.value === '' ? null : event.target.value,
            }))
          }
        />
        {errors.displayName?.map((message) => (
          <p key={message} className="text-xs text-destructive">{message}</p>
        ))}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="user-email">Email</Label>
        <Input
          id="user-email"
          type="email"
          value={draft.email ?? ''}
          onChange={(event) =>
            setDraft((current) => ({
              ...current,
              email: event.target.value === '' ? null : event.target.value,
            }))
          }
        />
        {errors.email?.map((message) => (
          <p key={message} className="text-xs text-destructive">{message}</p>
        ))}
      </div>

      {usernameReadOnly && (
        <div className="space-y-1.5">
          <Label htmlFor="user-username-readonly">Username</Label>
          <Input
            id="user-username-readonly"
            value={draft.username}
            readOnly
          />
          <p className="text-xs text-muted-foreground">
            Username cannot be changed after creation.
          </p>
        </div>
      )}

      {error && !hasErrors(serverErrors) ? (
        <ErrorState title={userErrorTitle(error)} message={userErrorMessage(error)} />
      ) : null}

      {hasErrors(serverErrors) ? (
        <Alert variant="destructive" role="alert">
          <AlertTitle>{userErrorTitle(error)}</AlertTitle>
          <AlertDescription>
            <ul className="list-inside list-disc">
              {Object.entries(serverErrors).flatMap(([field, messages]) =>
                messages.map((message) => (
                  <li key={`${field}-${message}`}>{message}</li>
                ))
              )}
            </ul>
          </AlertDescription>
        </Alert>
      ) : null}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={pending}>
          Cancel
        </Button>
        <Button type="submit" disabled={pending}>
          {pending ? 'Saving…' : submitLabel}
        </Button>
      </div>
    </form>
  )
}

/** Reads a stored user into the form state (edit mode). */
export function userFormDraftFrom(user: UserResponse): UserDraft {
  return {
    username: user.username,
    displayName: user.displayName ?? null,
    email: user.email ?? null,
    password: null,
  }
}
