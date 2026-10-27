import { cn } from 'cn'
import { useReducedMotion } from '@/hooks/use-reduced-motion'

interface MotionGraphProps {
  stages: Array<{
    id: string
    label: string
    status: 'completed' | 'active' | 'pending' | 'failed' | 'unknown'
    progress?: number
  }>
  className?: string
}

const stageOrder = [
  { id: 'changes', label: 'Configuration Changes' },
  { id: 'draft', label: 'Draft' },
  { id: 'snapshot', label: 'Snapshot' },
  { id: 'publication', label: 'Publication' },
  { id: 'gateways', label: 'Gateway Nodes' },
] as const

const statusColors = {
  completed: 'bg-primary text-primary-foreground border-primary',
  active: 'bg-primary/20 text-primary border-primary animate-pulse',
  pending: 'bg-muted text-muted-foreground border-muted',
  failed: 'bg-destructive/20 text-destructive border-destructive',
  unknown: 'bg-muted/50 text-muted/50 border-muted/50',
}

const statusIcons = {
  completed: 'check',
  active: 'loader',
  pending: 'circle',
  failed: 'x-circle',
  unknown: 'help-circle',
}

function StatusIcon({ status, className }: { status: string; className?: string }) {
  const icons = {
    check: (
      <svg className={cn('w-4 h-4', className)} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M20 6L9 17l-5-5" />
      </svg>
    ),
    loader: (
      <svg className={cn('w-4 h-4 animate-spin', className)} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M21 12a9 9 0 1 1-6.219-8.56" />
      </svg>
    ),
    circle: (
      <svg className={cn('w-4 h-4', className)} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="12" cy="12" r="10" />
      </svg>
    ),
    'x-circle': (
      <svg className={cn('w-4 h-4', className)} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="12" cy="12" r="10" />
        <path d="M15 9l-6 6M9 9l6 6" />
      </svg>
    ),
    'help-circle': (
      <svg className={cn('w-4 h-4', className)} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="12" cy="12" r="10" />
        <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" />
        <path d="M12 17h.01" />
      </svg>
    ),
  }

  const iconName = statusIcons[status as keyof typeof statusIcons] || 'help-circle'
  return icons[iconName as keyof typeof icons]
}

const MotionGraphStageNode = ({
  stage,
  status,
  progress,
  compact,
}: {
  stage: { id: string; label: string }
  status: string
  progress?: number
  compact?: boolean
}) => {
  const stageLabel = status === 'active' && progress !== undefined
    ? `${Math.round(progress)}%`
    : status.charAt(0).toUpperCase() + status.slice(1)

  const statusColors = {
    completed: 'bg-primary text-primary-foreground border-primary',
    active: 'bg-primary/20 text-primary border-primary animate-pulse',
    pending: 'bg-muted text-muted-foreground border-muted',
    failed: 'bg-destructive/20 text-destructive border-destructive',
    unknown: 'bg-muted/50 text-muted/50 border-muted/50',
  }

  const statusColorsBadge = {
    completed: 'bg-primary/10 text-primary',
    active: 'bg-primary/10 text-primary animate-pulse',
    pending: 'bg-muted text-muted-foreground',
    failed: 'bg-destructive/10 text-destructive',
    unknown: 'bg-muted/50 text-muted/50',
  }

  const stageLabelText = status === 'active' && progress !== undefined
    ? `${Math.round(progress)}%`
    : status.charAt(0).toUpperCase() + status.slice(1)

  return (
    <div className="flex items-center gap-4">
      {/* Stage node */}
      <div className="relative flex-shrink-0">
        <div
          className={cn(
            'flex items-center justify-center w-10 h-10 rounded-full border-2 transition-all duration-300',
            {
              'bg-primary text-primary-foreground border-primary': status === 'completed',
              'bg-primary/20 text-primary border-primary animate-pulse': status === 'active',
              'bg-muted text-muted-foreground border-muted': status === 'pending',
              'bg-destructive/20 text-destructive border-destructive': status === 'failed',
              'bg-muted/50 text-muted/50 border-muted/50': status === 'unknown',
            }
          )}
          aria-label={`Stage ${stage.label}: ${status}`}
        >
          <StatusIcon status={status} />
        </div>
        {/* Progress ring for active stage */}
        {status === 'active' && progress !== undefined && progress > 0 && (
          <div className="absolute inset-0" aria-hidden="true">
            <svg className="w-10 h-10 -rotate-90" viewBox="0 0 36 36">
              <circle
                cx="18"
                cy="18"
                r="15.915"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeDasharray={`${progress * 100} 100`}
                strokeLinecap="round"
                className="text-primary/50"
                transform="rotate(-90 18 18)"
              />
            </svg>
          </div>
        )}
      </div>

      {/* Stage label and progress */}
      <div className="flex-1 min-w-0 space-y-1">
        <div className="flex items-center gap-2">
          <span className="font-medium text-sm truncate">{stage.label}</span>
          <span
            className={cn(
              'px-2 py-0.5 text-xs font-medium rounded-full',
              status === 'completed' && 'bg-primary/10 text-primary',
              status === 'active' && 'bg-primary/10 text-primary animate-pulse',
              status === 'pending' && 'bg-muted text-muted-foreground',
              status === 'failed' && 'bg-destructive/10 text-destructive',
              status === 'unknown' && 'bg-muted/50 text-muted/50'
            )}
          >
            {status === 'active' && progress !== undefined
              ? `${Math.round(progress)}%`
              : status.charAt(0).toUpperCase() + status.slice(1)}
          </span>
        </div>
        {progress !== undefined && status === 'active' && (
          <div className="h-1 bg-muted rounded-full overflow-hidden">
            <div
              className="h-full bg-primary transition-all duration-300 ease-out"
              style={{ width: `${progress}%` }}
              aria-hidden="true"
            />
          </div>
        )}
      </div>
    </div>
  )
}

/**
 * Motion Graph visualizing the publication lifecycle stages.
 * 
 * Stages: Configuration Changes → Draft → Snapshot → Publication → Gateway Nodes
 * 
 * Visual style: Technical, minimal, with animated flow between stages.
 */
export function MotionGraph({ stages, className, compact = false }: MotionGraphProps) {
  const prefersReducedMotion = useReducedMotion()

  // Build stage data with status from props
  const stageData = stageOrder.map((stage) => {
    const stageInfo = stages.find(s => s.id === stage.id)
    return {
      ...stage,
      status: stageInfo?.status || 'pending',
      progress: stageInfo?.progress,
    }
  })

  // When reduced motion is preferred, disable animations
  const animationClass = prefersReducedMotion ? '' : 'transition-all duration-300'

  return (
    <div className={cn('space-y-4', className)}>
      {/* Stage indicators with connecting lines */}
      <div className="relative">
        {/* Stage nodes */}
        <div className="flex flex-col space-y-4">
          {stageOrder.map((stage, index) => {
            const statusInfo = stages.find(s => s.id === stage.id)
            const status = statusInfo?.status || 'pending'
            const progress = statusInfo?.progress

            return (
              <MotionGraphStageNode
                key={stage.id}
                stage={stage}
                status={status}
                progress={progress}
                compact={compact}
              />
            )
          })}
        </div>
      </div>

      {/* Legend */}
      {!compact && (
        <div className="flex flex-wrap items-center gap-4 text-xs text-muted-foreground pt-2 border-t border-border">
          <span className="font-medium">Legend:</span>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full border-2 border-primary bg-primary" />
            <span>Completed</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full border-2 border-primary bg-primary/20 animate-pulse" />
            <span>In Progress</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full border-2 border-muted bg-muted" />
            <span>Pending</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full border-2 border-destructive bg-destructive/20" />
            <span>Failed</span>
          </div>
        </div>
      )}
    </div>
  )
}

function calculateOverallProgress(stages: Array<{ id: string; status: string; progress?: number }>): number {
  const totalStages = stageOrder.length
  let completedStages = 0
  let activeProgress = 0

  for (const stage of stageOrder) {
    const stageInfo = stages.find(s => s.id === stage.id)
    if (!stageInfo) break

    if (stageInfo.status === 'completed') {
      completedStages++
    } else if (stageInfo.status === 'active' && stageInfo.progress) {
      activeProgress = stageInfo.progress / 100
      break
    } else {
      break
    }
  }

  return Math.min(100, ((completedStages + activeProgress) / totalStages) * 100)
}

