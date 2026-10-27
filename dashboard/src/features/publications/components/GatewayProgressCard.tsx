import { Badge } from '@/components/ui/badge'

interface GatewayProgressCardProps {
  gatewayId: string
  gatewayName?: string
  status: string
  receivedAt?: string | null
  validatedAt?: string | null
  appliedAt?: string | null
  healthyAt?: string | null
  failedAt?: string | null
  isValid?: boolean | null
  failureReason?: string | null
}

/**
 * Card showing individual gateway deployment progress.
 */
export function GatewayProgressCard({
  gatewayId,
  gatewayName,
  status,
  receivedAt,
  validatedAt,
  appliedAt,
  healthyAt,
  failedAt,
  isValid,
  failureReason,
}: GatewayProgressCardProps) {
  const statusLower = status.toLowerCase()

  return (
    <div className="border rounded-lg p-4 space-y-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="font-mono text-xs text-muted-foreground">{gatewayId.slice(0, 8)}</span>
          {gatewayName && <span className="text-sm font-medium">{gatewayName}</span>}
        </div>
        <Badge
          variant={
            statusLower === 'healthy' || statusLower === 'completed' ? 'default' :
            statusLower === 'failed' || statusLower.includes('fail') ? 'destructive' :
            statusLower === 'pending' || statusLower === 'received' || statusLower === 'validated' ? 'secondary' :
            'outline'
          }
        >
          {status}
        </Badge>
      </div>

      <dl className="grid gap-1 sm:grid-cols-2 text-xs text-muted-foreground">
        <dt>Received</dt>
        <dd>{receivedAt ? new Date(receivedAt).toLocaleTimeString() : '—'}</dd>
        <dt>Validated</dt>
        <dd>{validatedAt ? new Date(validatedAt).toLocaleTimeString() : '—'}</dd>
        <dt>Applied</dt>
        <dd>{appliedAt ? new Date(appliedAt).toLocaleTimeString() : '—'}</dd>
        <dt>Healthy</dt>
        <dd>{healthyAt ? new Date(healthyAt).toLocaleTimeString() : '—'}</dd>
        {failedAt && (
          <>
            <dt className="text-destructive">Failed</dt>
            <dd className="text-destructive">{new Date(failedAt!).toLocaleTimeString()}</dd>
          </>
        )}
        {failureReason && (
          <>
            <dt className="col-span-2 text-destructive">Failure</dt>
            <dd className="col-span-2 text-destructive text-xs">{failureReason}</dd>
          </>
        )}
        {isValid !== null && (
          <>
            <dt>Validation</dt>
            <dd>{isValid ? 'Passed' : 'Failed'}</dd>
          </>
        )}
      </dl>
    </div>
  )
}