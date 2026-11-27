import { useMemo, useState } from 'react'

import { ErrorState } from '@/components/page-state'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { PermissionDefinitionResponse } from '@/api'

export interface RoleDraft {
  name: string
  description: string | null
  permissions: string[]
}

export function validateRoleDraft(draft: RoleDraft): Record<string, string[]> {
  const errors: Record<string, string[]> = {}
  if (draft.name.trim().length === 0) errors.name = ['Name is required']
  if (draft.name.trim().length > 100) errors.name = ['Name must be 100 characters or fewer']
  if (draft.description !== null && draft.description.length > 500) {
    errors.description = ['Description must be 500 characters or fewer']
  }
  return errors
}

function hasErrors(errors: Record<string, string[]>): boolean {
  return Object.keys(errors).length > 0
}

function normalizeStatus(status: string): string {
  return status.toLowerCase()
}

export function RoleForm({
  initial,
  submitLabel,
  pending,
  error,
  permissions,
  permissionsPending,
  permissionsError,
  onSubmit,
  onCancel,
}: {
  initial: RoleDraft | null
  submitLabel: string
  pending: boolean
  error: unknown
  permissions: PermissionDefinitionResponse[]
  permissionsPending: boolean
  permissionsError: unknown
  onSubmit: (draft: RoleDraft) => void
  onCancel: () => void
}) {
  const [draft, setDraft] = useState<RoleDraft>(
    initial ?? { name: '', description: null, permissions: [] },
  )
  const [showErrors, setShowErrors] = useState(false)
  const clientErrors = validateRoleDraft(draft)
  const errors = showErrors ? clientErrors : {}

  const grouped = useMemo(() => {
    const map = new Map<string, PermissionDefinitionResponse[]>()
    for (const permission of permissions) {
      const category = permission.category ?? 'Uncategorized'
      const list = map.get(category) ?? []
      list.push(permission)
      map.set(category, list)
    }
    return Array.from(map.entries()).sort(([a], [b]) => a.localeCompare(b))
  }, [permissions])

  const togglePermission = (code: string) => {
    setDraft((current) => {
      const has = current.permissions.includes(code)
      return {
        ...current,
        permissions: has
          ? current.permissions.filter((p) => p !== code)
          : [...current.permissions, code],
      }
    })
  }

  return (
    <form
      className="space-y-5"
      onSubmit={(event) => {
        event.preventDefault()
        setShowErrors(true)
        if (hasErrors(clientErrors)) return
        onSubmit({ ...draft, name: draft.name.trim() })
      }}
    >
      <div className="space-y-1.5">
        <Label htmlFor="role-name">Name</Label>
        <Input
          id="role-name"
          value={draft.name}
          onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
        />
        {errors.name?.map((message) => (
          <p key={message} className="text-xs text-destructive">{message}</p>
        ))}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="role-description">Description</Label>
        <Input
          id="role-description"
          value={draft.description ?? ''}
          onChange={(event) =>
            setDraft((current) => ({
              ...current,
              description: event.target.value === '' ? null : event.target.value,
            }))
          }
        />
        {errors.description?.map((message) => (
          <p key={message} className="text-xs text-destructive">{message}</p>
        ))}
      </div>

      <div className="space-y-2">
        <Label>Permissions</Label>
        {permissionsError ? (
          <ErrorState
            title="Could not load permission catalog"
            message={permissionsError instanceof Error ? permissionsError.message : String(permissionsError)}
          />
        ) : null}
        {permissionsPending ? (
          <p className="text-sm text-muted-foreground">Loading permissions…</p>
        ) : (
          <div className="max-h-96 space-y-4 overflow-y-auto rounded-md border p-3">
            {grouped.map(([category, items]) => (
              <div key={category}>
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">{category}</p>
                <div className="space-y-2">
                  {items.map((permission) => {
                    const checked = draft.permissions.includes(permission.code)
                    const assignable = permission.assignable && normalizeStatus(permission.status) === 'active'
                    return (
                      <label key={permission.code} className="flex items-start gap-2 text-sm">
                        <input
                          type="checkbox"
                          className="mt-0.5"
                          checked={checked}
                          disabled={!assignable && !checked}
                          onChange={() => togglePermission(permission.code)}
                        />
                        <span className="min-w-0">
                          <span className="font-mono text-xs">{permission.code}</span>
                          {!assignable ? (
                            <span className="ml-2 text-xs text-muted-foreground">({permission.status}{permission.assignable ? '' : ', not assignable'})</span>
                          ) : null}
                          {permission.description ? (
                            <span className="block text-xs text-muted-foreground">{permission.description}</span>
                          ) : null}
                        </span>
                      </label>
                    )
                  })}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {error ? (
        <Alert variant="destructive" role="alert">
          <AlertTitle>Operation failed</AlertTitle>
          <AlertDescription>{error instanceof Error ? error.message : String(error)}</AlertDescription>
        </Alert>
      ) : null}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={pending}>Cancel</Button>
        <Button type="submit" disabled={pending}>{pending ? 'Saving…' : submitLabel}</Button>
      </div>
    </form>
  )
}
