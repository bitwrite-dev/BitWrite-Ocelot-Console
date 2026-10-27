import { cn } from 'cn'

interface PublicationProgressBarProps {
  progress: number
  status: string
  className?: string
}

/**
 * Progress bar for publication with status-aware styling.
 */
export function PublicationProgressBar({ progress, status, className }: PublicationProgressBarProps) {
  const statusLower = status.toLowerCase()
  const isComplete = statusLower === 'published' || statusLower.includes('complete') || statusLower.includes('success')
  const isFailed = statusLower === 'failed' || statusLower.includes('fail') || statusLower.includes('error')

  return (
    <div className={cn('w-full h-2 bg-muted rounded-full overflow-hidden', className)}>
      <div
        className={cn(
          'h-full transition-all duration-500 ease-out',
          isFailed ? 'bg-destructive' : 'bg-primary'
        )}
        style={{ width: `${Math.min(100, Math.max(0, progress))}%` }}
        role="progressbar"
        aria-valuenow={Math.round(progress)}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`Publication progress: ${Math.round(progress)}%`}
      />
    </div>
  )
}