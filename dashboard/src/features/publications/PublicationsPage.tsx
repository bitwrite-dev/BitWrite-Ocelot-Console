import { useState } from 'react'
import { Loader2, Send, AlertCircle } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ErrorState } from '@/components/page-state'
import { Pagination } from '@/components/ui/pagination'
import { PageHeader } from '@/components/app-layout'
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from '@/components/ui/sheet'
import { AlertDialog, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

import { usePublications, usePublicationDetail, useCreatePublication, usePublicationProgress, usePublicationDiff, useCurrentPublication, useCancelPublication } from './queries'
import { PublicationStatusBadge, PublicationProgressBar, GatewayProgressCard, MotionGraph } from './components'
import { formatTimestamp, shortHash } from './queries'
import { readSelectedEnvironment } from '@/lib/environment'
import { useSnapshots, describeComposition, shortHash as snapShortHash } from '@/features/snapshots/queries'


/** Publication management page with full workflow. */
export function PublicationsPage() {
  const [page, setPage] = useState(1)
  const [viewingId, setViewingId] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [selectedSnapshot, setSelectedSnapshot] = useState<number | null>(null)
  const [reviewing, setReviewing] = useState(false)
  const [executing, setExecuting] = useState(false)
  const [executingId, setExecutingId] = useState<string | null>(null)
  const [isPublishing, setIsPublishing] = useState(false)
  const [publishError, setPublishError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string | null>(null)

  const { data, isPending, isError, error, refetch } = usePublications(page)
  const { data: currentPublication } = useCurrentPublication()

  const createMutation = useCreatePublication()
  const cancelMutation = useCancelPublication()

  async function handleCancel(publicationId: string) {
    try {
      await cancelMutation.mutateAsync({ id: publicationId, request: { initiatedBy: 'dashboard' } })
      refetch()
    } catch (error) {
      console.error('Cancel failed:', error)
    }
  }

  if (isPending && !executing && !isPublishing) return <PublicationsSkeleton />
  if (isError || !data) {
    return (
      <div className="space-y-4">
        <PageHeader
          title="Publications"
          description="Manage configuration publications to gateway nodes."
        />
        <ErrorState
          title="Could not load publications"
          message={error instanceof Error ? error.message : String(error ?? 'Nothing was returned')}
          action={<Button variant="outline" size="sm" onClick={() => refetch()}>Try again</Button>}
        />
      </div>
    )
  }

  const filteredPublications = data.publications.filter(p => 
    !statusFilter || p.status.toLowerCase().includes(statusFilter.toLowerCase())
  )

  return (
    <div className="space-y-6">
      <PageHeader
        title="Publications"
        description="Manage configuration publications to gateway nodes. Create, review, and track deployments."
        actions={
          !creating && !reviewing && !executing && (
            <Button onClick={() => setCreating(true)}>
              <Send className="mr-2 size-4" aria-hidden="true" />
              Create Publication
            </Button>
          )
        }
      />

      <CurrentPublicationCard current={currentPublication ?? null} />

      {creating && (
        <CreatePublicationFlow
          onCancel={() => { setCreating(false); setSelectedSnapshot(null); }}
          onCreate={(snapshotVersion) => {
            setSelectedSnapshot(snapshotVersion)
            setCreating(false)
            setReviewing(true)
          }}
        />
      )}

      {reviewing && selectedSnapshot && (
        <ReviewPublicationPage
          snapshotVersion={selectedSnapshot}
          onCancel={() => { setReviewing(false); setSelectedSnapshot(null); }}
          onConfirm={async () => {
            try {
              await executePublication(selectedSnapshot)
            } catch (error) {
              console.error('Publication execution failed:', error)
            }
            setReviewing(false)
            setSelectedSnapshot(null)
          }}
        />
      )}

{publishError && (
        <Alert variant="destructive">
          <AlertTitle>Publication failed</AlertTitle>
          <AlertDescription>{publishError}</AlertDescription>
        </Alert>
      )}

      {isPublishing && !executing && (
        <Card>
          <CardContent className="py-8 text-center">
            <Loader2 className="mx-auto h-8 w-8 animate-spin text-primary" />
            <p className="mt-4 text-sm text-muted-foreground">
              Creating and executing publication...
            </p>
          </CardContent>
        </Card>
      )}

      {executing && executingId && (
        <PublicationProgressPage
          publicationId={executingId}
          onClose={() => { setExecuting(false); setExecutingId(null); }}
        />
      )}

      {!creating && !reviewing && !executing && !isPublishing && (
        <>
          <PublicationsFilters
            statusFilter={statusFilter}
            onStatusChange={setStatusFilter}
          />
          <PublicationsTable
            publications={filteredPublications}
            onView={setViewingId}
            viewingId={viewingId}
            onCancel={handleCancel}
          />

          <Pagination
            page={data.page}
            pageSize={data.pageSize}
            totalCount={data.totalCount}
            onPage={setPage}
          />
        </>
      )}

      {viewingId && (
        <PublicationDetailSheet
          publicationId={viewingId}
          onClose={() => setViewingId(null)}
        />
      )}

      {creating && selectedSnapshot && (
        <CreatePublicationConfirmSheet
          snapshotVersion={selectedSnapshot}
          onCancel={() => { setCreating(false); setSelectedSnapshot(null); }}
          onConfirm={() => { setCreating(false); setReviewing(true); }}
        />
      )}
    </div>
  )

  async function executePublication(snapshotVersion: number) {
    setIsPublishing(true)
    setPublishError(null)
    try {
      // First create the publication
      const environment = readSelectedEnvironment() || 'development'
      const result = await createMutation.mutateAsync({
        snapshotVersion: snapshotVersion.toString(),
        initiatedBy: 'dashboard',
        environment,
        targetGatewayIds: undefined
      })

      // Then execute it
      const executingId = result.publicationId
      try {
        await executeMutation.mutateAsync({ 
          id: executingId, 
          request: { 
            initiatedBy: 'dashboard',
            environment: readSelectedEnvironment() || 'development'
          } 
        })
        // Only set executing state if execution succeeded
        setExecuting(true)
        setExecutingId(executingId)
      } catch (error) {
        console.error('Execute failed:', error)
        const executeError = error instanceof Error ? error.message : String(error)
        setPublishError(executeError)
      }
    } catch (error) {
      console.error('Publication failed:', error)
      setPublishError(error instanceof Error ? error.message : String(error))
    } finally {
      setIsPublishing(false)
    }
  }
}

function CurrentPublicationCard({ current }: { current: { current: { snapshotVersion: number } | null } | null }) {
  if (!current?.current) {
    return (
      <Card className="border-dashed">
        <CardContent className="py-6 text-center text-sm text-muted-foreground">
          No snapshot is currently published. Gateways are running their default
          configuration.
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="border-primary">
      <CardContent className="py-4">
        <div className="flex flex-wrap items-center gap-3">
          <span className="text-sm font-medium text-primary">Currently published:</span>
          <span className="px-2 py-1 text-xs font-medium bg-primary/10 text-primary rounded">
            Snapshot #{current.current.snapshotVersion}
          </span>
          <p className="text-sm text-muted-foreground ml-auto">
            Gateways are running this snapshot.
          </p>
        </div>
      </CardContent>
    </Card>
  )
}

function PublicationsFilters({ statusFilter, onStatusChange }: { statusFilter: string | null; onStatusChange: (v: string | null) => void }) {
  return (
    <Card>
      <CardContent className="py-4">
        <div className="flex flex-wrap items-center gap-4">
          <Label htmlFor="publication-status" className="text-sm text-muted-foreground">
            Filter by status
          </Label>
          <Select
            value={statusFilter ?? 'all'}
            onValueChange={(value) => {
              onStatusChange(value === 'all' ? null : value)
            }}
          >
            <SelectTrigger id="publication-status" className="w-48">
              <SelectValue placeholder="All statuses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="completed">Completed</SelectItem>
              <SelectItem value="in progress">In Progress</SelectItem>
              <SelectItem value="pending">Pending</SelectItem>
              <SelectItem value="failed">Failed</SelectItem>
              <SelectItem value="rolled back">Rolled Back</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </CardContent>
    </Card>
  )
}

function PublicationsTable({ publications, onView, viewingId, onCancel }: {
  publications: Array<{
    id: string
    snapshotVersion: number
    status: string
    initiatedBy: string
    startedAt: string
    completedAt: string | null
    failureReason: string | null
    gatewayStates: { gatewayId: string; status: string }[]
  }>
  onView: (id: string) => void
  viewingId: string | null
  onCancel?: (id: string) => void
}) {
  if (publications.length === 0) {
    return (
      <Card>
        <CardContent className="pt-6 text-sm text-muted-foreground text-center">
          No publications yet. Click <span className="font-medium">Create Publication</span> to start a new deployment.
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>ID</TableHead>
            <TableHead>Snapshot</TableHead>
            <TableHead>Initiated by</TableHead>
            <TableHead>Started</TableHead>
            <TableHead>Completed</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">Gateways</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {publications.map((publication) => (
            <PublicationRow
              key={publication.id}
              publication={publication}
              isViewing={viewingId === publication.id}
              onClick={() => onView(publication.id)}
              onCancel={onCancel}
            />
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

function PublicationRow({ publication, isViewing, onClick, onCancel }: {
  publication: {
    id: string
    snapshotVersion: number
    status: string
    initiatedBy: string
    startedAt: string
    completedAt: string | null
    failureReason: string | null
    gatewayStates: { gatewayId: string; status: string }[]
  }
  isViewing: boolean
  onClick: () => void
  onCancel?: (id: string) => void
}) {

  const isPending = publication.status.toLowerCase() === 'pending'

  return (
    <TableRow
      className={`cursor-pointer ${isViewing ? 'bg-primary/5' : 'hover:bg-accent'}`}
      onClick={onClick}
    >
      <TableCell className="font-mono text-xs">{shortHash(publication.id)}</TableCell>
      <TableCell>
        <span className="px-2 py-0.5 text-xs font-medium bg-muted text-muted-foreground rounded">
          #{publication.snapshotVersion}
        </span>
      </TableCell>
      <TableCell className="text-sm">{publication.initiatedBy}</TableCell>
      <TableCell className="text-sm text-muted-foreground">
        {formatTimestamp(publication.startedAt)}
      </TableCell>
      <TableCell className="text-sm text-muted-foreground">
        {formatTimestamp(publication.completedAt)}
      </TableCell>
      <TableCell>
        <PublicationStatusBadge status={publication.status} />
      </TableCell>
      <TableCell className="text-right text-sm text-muted-foreground">
        {publication.gatewayStates.length} gateway{publication.gatewayStates.length !== 1 ? 's' : ''}
      </TableCell>
      <TableCell className="text-right">
        {isPending && onCancel && (
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 text-destructive hover:text-destructive"
            onClick={(e) => {
              e.stopPropagation()
              onCancel(publication.id)
            }}
            title="Cancel publication"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M18 6L6 18M6 6l12 12" />
            </svg>
          </Button>
        )}
      </TableCell>
    </TableRow>
  )
}

function PublicationDetailSheet({ publicationId, onClose }: { publicationId: string; onClose: () => void }) {
  const { data: publication, isPending } = usePublicationDetail(publicationId, !!publicationId)

  if (isPending || !publication) return <PublicationDetailSkeleton />

  return (
    <Sheet open onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="w-full overflow-y-auto sm:max-w-3xl">
        <SheetHeader>
          <SheetTitle>Publication #{publication.id.slice(0, 8)}</SheetTitle>
          <SheetDescription>
            Snapshot #{publication.snapshotVersion} · {formatTimestamp(publication.startedAt)}
          </SheetDescription>
        </SheetHeader>

        <div className="space-y-4 px-4 pb-6">
          <div className="flex flex-wrap items-center gap-2">
            <PublicationStatusBadge status={publication.status} />
            <span className="px-2 py-0.5 text-xs font-medium bg-muted text-muted-foreground rounded">
              Snapshot #{publication.snapshotVersion}
            </span>
          </div>

          <Separator />

          <Card>
            <CardHeader>
              <CardTitle>Publication Details</CardTitle>
              <CardDescription>Core publication metadata</CardDescription>
            </CardHeader>
            <CardContent>
              <dl className="grid gap-1 sm:grid-cols-[110px_1fr] text-sm">
                <dt>Publication ID</dt>
                <dd className="truncate font-mono text-xs">{publication.id}</dd>
                <dt>Snapshot</dt>
                <dd>#{publication.snapshotVersion}</dd>
                <dt>Status</dt>
                <dd>
                  <PublicationStatusBadge status={publication.status} />
                </dd>
                <dt>Initiated by</dt>
                <dd>{publication.initiatedBy}</dd>
                <dt>Started</dt>
                <dd>{formatTimestamp(publication.startedAt)}</dd>
                <dt>Completed</dt>
                <dd>{formatTimestamp(publication.completedAt)}</dd>
                {publication.failureReason && (
                  <>
                    <dt>Failure reason</dt>
                    <dd className="text-destructive">{publication.failureReason}</dd>
                  </>
                )}
              </dl>
            </CardContent>
          </Card>

          <Separator />

          <Card>
            <CardHeader>
              <CardTitle>Gateway deployment</CardTitle>
              <CardDescription>Individual gateway deployment results</CardDescription>
            </CardHeader>
            <CardContent>
              {publication.gatewayStates.length === 0 ? (
                <p className="text-xs text-muted-foreground">No gateway states recorded.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Gateway</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Received</TableHead>
                      <TableHead>Validated</TableHead>
                      <TableHead>Applied</TableHead>
                      <TableHead>Healthy</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {publication.gatewayStates.map((gw: { gatewayId: string; status: string; receivedAt: string | null; validatedAt: string | null; appliedAt: string | null; healthyAt: string | null }) => (
                      <TableRow key={gw.gatewayId}>
                        <TableCell className="font-mono text-xs">{shortHash(gw.gatewayId)}</TableCell>
                        <TableCell>
                          <Badge variant={gw.status.toLowerCase() === 'healthy' || gw.status.toLowerCase() === 'completed' ? 'default' : gw.status.toLowerCase() === 'failed' ? 'destructive' : 'secondary'}>
                            {gw.status}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-muted-foreground text-xs">
                          {gw.receivedAt ? new Date(gw.receivedAt).toLocaleTimeString() : '—'}
                        </TableCell>
                        <TableCell className="text-muted-foreground text-xs">
                          {gw.validatedAt ? new Date(gw.validatedAt).toLocaleTimeString() : '—'}
                        </TableCell>
                        <TableCell className="text-muted-foreground text-xs">
                          {gw.appliedAt ? new Date(gw.appliedAt).toLocaleTimeString() : '—'}
                        </TableCell>
                        <TableCell className="text-muted-foreground text-xs">
                          {gw.healthyAt ? new Date(gw.healthyAt).toLocaleTimeString() : '—'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>

          {publication.failureReason && (
            <Alert variant="destructive">
              <AlertTitle>Publication failed</AlertTitle>
              <AlertDescription>{publication.failureReason}</AlertDescription>
            </Alert>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}

function PublicationDetailSkeleton() {
  return (
    <Sheet open>
      <SheetContent className="w-full overflow-y-auto sm:max-w-3xl">
        <SheetHeader>
          <SheetTitle><Skeleton className="h-6 w-48" /></SheetTitle>
        </SheetHeader>
        <div className="space-y-4 px-4 pb-6">
          <Skeleton className="h-8 w-48" />
          <Separator />
          <Skeleton className="h-32 w-full" />
        </div>
      </SheetContent>
    </Sheet>
  )
}

function CreatePublicationFlow({ onCancel, onCreate }: { onCancel: () => void; onCreate: (version: number) => void }) {
  const [selectedVersion, setSelectedVersion] = useState<number | null>(null)
  const { data: snapshotsData, isPending: snapshotsPending } = useSnapshots(1, 50, 'Ready')

  // Filter only Ready snapshots (eligible for publication)
  const availableSnapshots = snapshotsData?.snapshots.filter(s => s.status === 'Ready') ?? []

  const handleCreate = async () => {
    if (selectedVersion) {
      await onCreate(selectedVersion)
    }
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold">Create Publication</h1>
          <p className="text-sm text-muted-foreground">
            Select a snapshot to publish to gateway nodes.
          </p>
        </div>
        <Button variant="outline" onClick={onCancel}>Cancel</Button>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Select Snapshot</CardTitle>
          <CardDescription>Choose which snapshot version to publish. Only snapshots with "Ready" status are eligible.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {snapshotsPending ? (
            <div className="space-y-2">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : availableSnapshots.length === 0 ? (
            <div className="text-center py-8">
              <AlertCircle className="mx-auto h-12 w-12 text-muted-foreground" />
              <h3 className="mt-4 text-lg font-medium">No eligible snapshots</h3>
              <p className="mt-2 text-sm text-muted-foreground">
                No snapshots with "Ready" status are available for publication.
                Create a snapshot from the <a href="/snapshots" className="underline hover:text-primary">Snapshots page</a> first.
              </p>
            </div>
          ) : (
            <div className="space-y-2">
              <Label htmlFor="snapshot-select" className="text-sm font-medium">
                Select Snapshot
              </Label>
              <Select
                value={selectedVersion?.toString() ?? ''}
                onValueChange={(value) => setSelectedVersion(value ? parseInt(value, 10) : null)}
              >
                <SelectTrigger id="snapshot-select" className="w-full">
                  <SelectValue placeholder="Select a snapshot" />
                </SelectTrigger>
                <SelectContent>
                  {availableSnapshots.map((snapshot) => (
                    <SelectItem key={snapshot.version} value={snapshot.version.toString()}>
                      <div className="flex flex-col gap-1">
                        <span className="font-medium">Snapshot #{snapshot.version}</span>
                        <span className="text-xs text-muted-foreground">
                          {describeComposition(snapshot)} · {formatTimestamp(snapshot.createdAt)} · {snapshot.status}
                        </span>
                        <span className="text-xs font-mono text-muted-foreground">
                          Hash: {snapShortHash(snapshot.configurationHash)}
                        </span>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}
          <p className="text-sm text-muted-foreground">
            Select a snapshot version to publish to gateway nodes.
          </p>
        </CardContent>
      </Card>

      <div className="flex gap-2">
        <Button variant="outline" onClick={onCancel}>Cancel</Button>
        <Button onClick={handleCreate} disabled={!selectedVersion}>
          Create Publication
        </Button>
      </div>
    </div>
  )
}

function CreatePublicationConfirmSheet({ snapshotVersion, onCancel, onConfirm }: {
  snapshotVersion: number
  onCancel: () => void
  onConfirm: () => void
}) {
  return (
    <AlertDialog open onOpenChange={(open) => !open && onCancel()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Publish snapshot #{snapshotVersion}?</AlertDialogTitle>
          <AlertDialogDescription>
            This will create a publication record for review before execution.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={onCancel}>Cancel</AlertDialogCancel>
          <Button onClick={onConfirm}>Create Publication</Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function ReviewPublicationPage({ snapshotVersion, onCancel, onConfirm }: {
  snapshotVersion: number
  onCancel: () => void
  onConfirm: () => void
}) {
  const { data: diff, isPending: diffPending } = usePublicationDiff(snapshotVersion.toString())

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold">Review Publication</h1>
          <p className="text-sm text-muted-foreground">
            Review the details before executing the publication.
          </p>
        </div>
        <Button variant="outline" onClick={onCancel}>Cancel</Button>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Snapshot #{snapshotVersion}</CardTitle>
          <CardDescription>Review the snapshot that will be published.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-center gap-2">
            <span className="px-2 py-0.5 text-xs font-medium bg-muted text-muted-foreground rounded">
              Snapshot #{snapshotVersion}
            </span>
          </div>

          {diffPending ? (
            <Skeleton className="h-16 w-full" />
          ) : diff ? (
            <div className="space-y-2">
              <h4 className="text-sm font-medium">Changes from current published version:</h4>
              {diff.hasDifferences ? (
                <ul className="list-disc list-inside space-y-1 text-sm text-muted-foreground">
                  {diff.differences.map((diffLine, i) => (
                    <li key={i}>{diffLine}</li>
                  ))}
                </ul>
              ) : (
                <p className="text-sm text-muted-foreground">
                  No changes detected from the current published version.
                </p>
              )}
              {diff.currentSnapshotVersion && (
                <p className="text-xs text-muted-foreground">
                  Comparing against snapshot #{diff.currentSnapshotVersion}
                </p>
              )}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              Unable to load diff information.
            </p>
          )}
        </CardContent>
      </Card>

      <div className="flex gap-2">
        <Button variant="outline" onClick={onCancel}>Cancel</Button>
        <Button onClick={onConfirm}>Publish Snapshot</Button>
      </div>
    </div>
  )
}

function PublicationProgressPage({ publicationId, onClose }: { publicationId: string; onClose: () => void }) {
  const { data: progress, isPending, isError, error } = usePublicationProgress(publicationId, true)

  if (isPending) return <ProgressSkeleton />

  const isFailed = progress?.status?.toLowerCase().includes('fail') || progress?.status?.toLowerCase().includes('error')
  const isCompleted = progress?.status?.toLowerCase().includes('complete') || progress?.status?.toLowerCase().includes('success') || progress?.status?.toLowerCase() === 'published'

  // Default stages when progress data is not available
  const stages = progress ? [
    { id: 'changes', label: 'Configuration Changes', status: 'completed' as const },
    { id: 'draft', label: 'Draft', status: 'completed' as const },
    { id: 'snapshot', label: 'Snapshot', status: 'completed' as const },
    { id: 'publication', label: 'Publication', status: isFailed ? 'failed' as const : isCompleted ? 'completed' as const : (progress.status.toLowerCase().includes('progress') ? 'active' as const : 'pending' as const), progress: isCompleted ? 100 : progress.progressPercentage },
    { id: 'gateways', label: 'Gateway Nodes', status: isFailed ? 'failed' as const : isCompleted ? 'completed' as const : 'pending' as const },
  ] : [
    { id: 'changes', label: 'Configuration Changes', status: 'completed' as const },
    { id: 'draft', label: 'Draft', status: 'completed' as const },
    { id: 'snapshot', label: 'Snapshot', status: 'completed' as const },
    { id: 'publication', label: 'Publication', status: 'active' as const, progress: 0 },
    { id: 'gateways', label: 'Gateway Nodes', status: 'pending' as const },
  ]

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold">Publication Progress</h1>
          <p className="text-sm text-muted-foreground">
            Publication #{publicationId.slice(0, 8)} in progress.
          </p>
        </div>
        <Button variant="outline" onClick={onClose}>Close</Button>
      </header>

      {isError && (
        <Alert variant="destructive">
          <AlertTitle>Could not load progress</AlertTitle>
          <AlertDescription>
            {error instanceof Error ? error.message : String(error ?? 'Unknown error')}
          </AlertDescription>
        </Alert>
      )}

      <MotionGraph stages={stages} />

      <Separator />

      {progress && (
        <PublicationProgressBar progress={progress.progressPercentage} status={progress.status} />
      )}

      <Separator />

      <div className="space-y-4">
        <h3 className="text-lg font-semibold">Gateway Deployment</h3>
        {progress && progress.gatewayProgress.length > 0 ? (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {progress.gatewayProgress.map((gw) => (
              <GatewayProgressCard
                key={gw.gatewayId}
                gatewayId={gw.gatewayId}
                gatewayName={gw.gatewayName}
                status={gw.status}
                receivedAt={gw.receivedAt}
                validatedAt={gw.validatedAt}
                appliedAt={gw.appliedAt}
                healthyAt={gw.healthyAt}
                failedAt={gw.failedAt}
                isValid={gw.isValid}
                failureReason={gw.failureReason}
              />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">
            Waiting for gateway status updates...
          </p>
        )}
      </div>
    </div>
  )
}

function ProgressSkeleton() {
  return (
    <div className="space-y-6">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-64 w-full" />
      <Separator />
      <Skeleton className="h-8 w-48" />
      <Skeleton className="h-4 w-full" />
      <Separator />
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Skeleton className="h-48" />
        <Skeleton className="h-48" />
        <Skeleton className="h-48" />
      </div>
    </div>
  )
}

function PublicationsSkeleton() {
  return (
    <div className="space-y-6">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-16 w-full" />
      <Skeleton className="h-64 w-full" />
    </div>
  )
}