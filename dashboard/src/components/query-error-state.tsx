import { ApiError } from '@/api'
import { ErrorState, ForbiddenState } from '@/components/page-state'

export function QueryErrorState({
  error,
  title = 'Something went wrong',
}: {
  error: unknown
  title?: string
}) {
  if (error instanceof ApiError && error.status === 403) {
    return (
      <ForbiddenState
        title="Access denied"
        description={error.message || 'You do not have permission to view this page.'}
      />
    )
  }

  if (error instanceof ApiError) {
    return <ErrorState title={title} message={error.message} correlationId={error.correlationId} />
  }

  return <ErrorState title={title} message={error instanceof Error ? error.message : String(error)} />
}
