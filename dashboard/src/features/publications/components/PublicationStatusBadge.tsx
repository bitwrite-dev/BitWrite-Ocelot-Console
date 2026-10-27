import { Badge } from '@/components/ui/badge'

/**
 * Publication status badge with consistent styling.
 */
export function PublicationStatusBadge({ status }: { status: string }) {
  const statusLower = status.toLowerCase()

  if (statusLower === 'published' || statusLower.includes('complete') || statusLower.includes('success')) {
    return <Badge variant="default">Completed</Badge>
  }
  if (statusLower === 'pending' || statusLower.includes('progress') || statusLower.includes('started')) {
    return <Badge variant="secondary">In Progress</Badge>
  }
  if (statusLower === 'failed' || statusLower.includes('fail') || statusLower.includes('error')) {
    return <Badge variant="destructive">Failed</Badge>
  }
  if (statusLower === 'rolledback') {
    return <Badge variant="outline">Rolled Back</Badge>
  }
  if (statusLower === 'pending') {
    return <Badge variant="secondary">Pending Review</Badge>
  }
  return <Badge variant="outline">Unknown</Badge>
}