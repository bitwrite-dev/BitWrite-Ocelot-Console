import { useState } from 'react'
import { Check, ClipboardCopy, Copy, GitCompare, Plus, Send } from 'lucide-react'

import { JsonHighlight, copyToClipboard } from '@/components/json-highlight'
import { StatusBadge } from '@/components/status-badge'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { isPermissionError } from '@/lib/api-error'
import { getEffectiveEnvironment } from '@/api/config'
import { useNavigate } from 'react-router-dom'
import type { SnapshotResponse } from '@/api'

import {
  describeComposition,
  describeDeployment,
  describeDifference,
  publicationState,
  shortHash,
  snapshotSummary,
  validationResultsOf,
  usePublishTargetGateways,
  useSnapshotComparison,
  useSnapshotDeployment,
  useSnapshotMutations,
  useSnapshots,
  validationVerdict,
  type SnapshotStatusFilter,
  SNAPSHOT_STATUSES,
} from './queries'

const PAGE_SIZE = 20
const ALL_STATUSES = 'all'

/**
 * Snapshots: the immutable, hashed records that make up a published state.
 *
 * A snapshot cannot be edited, so every action here either reads one or makes
 * something else current. That is why the table has no inline editing while the
 * global configuration page does.
 */
export function SnapshotsPage() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<SnapshotStatusFilter>(null)
  const [viewing, setViewing] = useState<SnapshotResponse | null>(null)
  const [comparing, setComparing] = useState<SnapshotResponse | null>(null)

  const { data, isPending, isError, error, refetch } = useSnapshots(page, PAGE_SIZE, status)

  if (isPending) return <SnapshotsSkeleton />
  if (isError || !data) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Could not load snapshots</AlertTitle>
        <AlertDescription>
          {error instanceof Error ? error.message : String(error ?? 'Nothing was returned')}
          <Button
            variant="outline"
            size="sm"
            className="ml-3"
            onClick={() => {
              refetch()
            }}
          >
            Try again
          </Button>
        </AlertDescription>
      </Alert>
    )
  }

  // A diff is against the snapshot immediately before this one, which is the
  // next row in a newest-first list.
  const previousFor = (index: number): number | null => {
    const older = data.snapshots[index + 1]
    return older ? older.version : null
  }

  const summary = snapshotSummary(data.snapshots, data.totalCount, data.pageSize)
  const hasPrevious = (snapshot: SnapshotResponse) =>
    data.snapshots.some((other) => other.version !== snapshot.version)

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold">Configuration snapshots</h1>
          <p className="text-sm text-muted-foreground">
            Immutable, validated configuration artifacts for controlled gateway
            publication.
          </p>
        </div>

        <Button onClick={() => navigate('/snapshots/new')}>
          <Plus aria-hidden="true" className="size-4" />
          Create snapshot
        </Button>
      </header>

      <SummaryCards summary={summary} />

      <div className="flex flex-wrap items-center gap-2">
        <Label htmlFor="snapshot-status" className="text-sm text-muted-foreground">
          Status
        </Label>
        <Select
          value={status ?? ALL_STATUSES}
          onValueChange={(value) => {
            setStatus(value === ALL_STATUSES ? null : (value as SnapshotStatusFilter))
            // A filter that matches three rows should not leave the operator on
            // page 7 of a list they can no longer scroll through.
            setPage(1)
          }}
        >
          <SelectTrigger id="snapshot-status" className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_STATUSES}>All</SelectItem>
            {SNAPSHOT_STATUSES.map((value) => (
              <SelectItem key={value} value={value}>
                {value}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {data.snapshots.length === 0 ? (
        <Card>
          <CardContent className="pt-6 text-sm text-muted-foreground">
            {status
              ? `No snapshots with the status ${status}.`
              : 'No snapshots yet. Create one from the current management state.'}
          </CardContent>
        </Card>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Version</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Created by</TableHead>
                <TableHead>Created at</TableHead>
                <TableHead>Integrity hash</TableHead>
                <TableHead>Validation</TableHead>
                <TableHead>Publication</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.snapshots.map((snapshot, index) => (
                <SnapshotRow
                  key={snapshot.version}
                  snapshot={snapshot}
                  previous={previousFor(index)}
                  onView={() => setViewing(snapshot)}
                  onCompare={() => setComparing(snapshot)}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Pagination
        page={data.page}
        pageSize={data.pageSize}
        totalCount={data.totalCount}
        onPage={setPage}
      />

      {viewing ? (
        <SnapshotSheet
          snapshot={viewing}
          onClose={() => setViewing(null)}
          onCompare={() => {
            setComparing(viewing)
            setViewing(null)
          }}
          canCompare={hasPrevious(viewing)}
        />
      ) : null}

      {comparing ? (
        <CompareSheet
          snapshot={comparing}
          previous={previousFor(data.snapshots.findIndex((s) => s.version === comparing.version))}
          onClose={() => setComparing(null)}
        />
      ) : null}
    </div>
  )
}

/**
 * Who the page attributes a new snapshot to.
 *
 * Fixed until the dashboard resolves a real session (#433). It is sent rather
 * than left blank because the API requires it, and an honest placeholder beats
 * an empty audit field.
 */
const CURRENT_OPERATOR = 'dashboard'

function SnapshotRow({
  snapshot,
  previous,
  onView,
  onCompare,
}: {
  snapshot: SnapshotResponse
  previous: number | null
  onView: () => void
  onCompare: () => void
}) {
  const validation = validationVerdict(snapshot)
  const publication = publicationState(snapshot)

  return (
    <TableRow>
      <TableCell>
        <div className="font-medium">#{snapshot.version}</div>
        <div className="text-xs text-muted-foreground">
          {describeComposition(snapshot)}
        </div>
      </TableCell>
      <TableCell>
        <StatusBadge status={snapshot.status} />
      </TableCell>
      <TableCell className="text-muted-foreground">{snapshot.createdBy}</TableCell>
      <TableCell className="text-muted-foreground">
        {formatTimestamp(snapshot.createdAt)}
      </TableCell>
      <TableCell>
        <HashCell hash={snapshot.hash} />
      </TableCell>
      <TableCell>
        <StatusBadge status={validation.label} />
      </TableCell>
      <TableCell>
        <div className="flex flex-wrap items-center gap-2">
          <StatusBadge status={publication.label} />
        </div>
        <div className="mt-1 text-xs text-muted-foreground">
          {publicationNote(snapshot)}
        </div>
      </TableCell>
      <TableCell>
        <div className="flex items-center justify-end gap-1">
          <Button variant="link" size="sm" className="h-auto p-0" onClick={onView}>
            View
          </Button>
          <Button
            variant="link"
            size="sm"
            className="h-auto p-0"
            // Nothing to diff the oldest loaded snapshot against.
            disabled={previous === null}
            onClick={onCompare}
          >
            Compare
          </Button>
        </div>
      </TableCell>
    </TableRow>
  )
}

/**
 * The publication column's second line.
 *
 * A snapshot that has been published keeps that timestamp even after another
 * snapshot replaced it, so the date is the fact worth carrying here. Nothing
 * claims the snapshot is what gateways run right now — that is what the badge
 * next to it is for.
 */
function publicationNote(snapshot: SnapshotResponse): string {
  if (snapshot.publishedAt) return `Published on ${formatTimestamp(snapshot.publishedAt)}`
  return 'Never published'
}

/** The hash, truncated for reading and copyable in full. */
function HashCell({ hash }: { hash: string }) {
  const [copied, setCopied] = useState(false)

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(hash)
      setCopied(true)
      // Without the reset the button keeps claiming success indefinitely.
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard access is refused in some contexts; the hash is still on
      // screen, so a failure here is not worth interrupting the operator for.
    }
  }

  return (
    <span className="inline-flex items-center gap-1">
      <code className="font-mono text-xs" title={hash || 'No hash'}>
        {shortHash(hash)}
      </code>
      {hash ? (
        <Button
          variant="ghost"
          size="icon"
          className="size-6"
          aria-label={copied ? 'Hash copied' : 'Copy the full hash'}
          onClick={copy}
        >
          <Copy aria-hidden="true" className="size-3" />
        </Button>
      ) : null}
    </span>
  )
}

function SummaryCards({
  summary,
}: {
  summary: ReturnType<typeof snapshotSummary>
}) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <Card>
        <CardHeader className="pb-2">
          <CardDescription>Published</CardDescription>
          <CardTitle className="text-2xl">
            {summary.publishedVersion === null ? '—' : `#${summary.publishedVersion}`}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-xs text-muted-foreground">
            {summary.publishedVersion === null
              ? `No published snapshot ${summary.scope}`
              : `Desired state ${summary.scope}`}
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-2">
          <CardDescription>Ready to publish</CardDescription>
          <CardTitle className="text-2xl">{summary.readyCount}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-xs text-muted-foreground">
            Validated immutable artifacts {summary.scope}
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-2">
          <CardDescription>Validation failures</CardDescription>
          <CardTitle className="text-2xl">{summary.failureCount}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-xs text-muted-foreground">
            Needs conflict resolution {summary.scope}
          </p>
        </CardContent>
      </Card>
    </div>
  )
}

/**
 * Publishing, behind a confirmation that names what it will change.
 *
 * A publish rewrites what every gateway in one environment serves, so the
 * confirmation names the environment and the gateways the publication reaches
 * rather than asking a bare "are you sure" whose subject is ambiguous while the
 * sheet is still on screen. Both facts are also what the API requires: a
 * publication has to name its environment, it has to match the one the request
 * was sent for, and it has to name its target gateways — so the dialog reads
 * them from the same places the request does rather than discovering the
 * refusal after the operator has already agreed.
 */
function PublishDialog({
  snapshot,
  onDone,
}: {
  snapshot: SnapshotResponse
  onDone: () => void
}) {
  const { publish } = useSnapshotMutations()
  const [outcome, setOutcome] = useState<string | null>(null)
  const environment = getEffectiveEnvironment()
  const { data: gateways, isPending, isError } = usePublishTargetGateways()
  const targetGatewayIds = (gateways ?? []).map((gateway) => gateway.id)

  // Nothing is sent while either half is missing: the API would refuse it, and a
  // refusal after the operator has agreed to the change reads as a surprise
  // rather than as a precondition.
  const blocked =
    environment === '' || isPending || targetGatewayIds.length === 0 || publish.isPending

  return (
    <AlertDialog open onOpenChange={(open) => !open && onDone()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Publish snapshot #{snapshot.version}?</AlertDialogTitle>
          <AlertDialogDescription>
            Every gateway in {environment || 'the selected environment'} will serve
            the {describeComposition(snapshot)} this snapshot carries. The snapshot
            is kept — nothing is deleted — and it can be published again later to
            roll back.
          </AlertDialogDescription>
        </AlertDialogHeader>

        {environment === '' ? (
          <Alert variant="destructive">
            <AlertTitle>No environment is selected</AlertTitle>
            <AlertDescription>
              A publication has to name the environment it is for. Pick one in the
              sidebar, then open this again.
            </AlertDescription>
          </Alert>
        ) : null}

        {isPending ? (
          <p className="text-xs text-muted-foreground">
            Looking up the gateways this publication will reach…
          </p>
        ) : null}

        {!isPending && targetGatewayIds.length === 0 ? (
          <Alert variant="destructive">
            <AlertTitle>No gateways to publish to</AlertTitle>
            <AlertDescription>
              {isError
                ? `The gateway list for ${environment} could not be read, and a publication has to name the gateways it reaches. Close this and try again.`
                : `There are no gateways registered in ${environment}, and a publication has to name at least one. Register a gateway, then try again.`}
            </AlertDescription>
          </Alert>
        ) : null}

        {outcome ? (
          <Alert>
            <AlertTitle>Outcome</AlertTitle>
            <AlertDescription>{outcome}</AlertDescription>
          </Alert>
        ) : null}

        {publish.error ? (
          <Alert variant="destructive">
            <AlertTitle>
              {isPermissionError(publish.error) ? 'Not permitted' : 'The publication was refused'}
            </AlertTitle>
            <AlertDescription>
              {isPermissionError(publish.error)
                ? 'Publishing needs the SnapshotManager or Admin role.'
                : publish.error instanceof Error
                  ? publish.error.message
                  : String(publish.error)}
            </AlertDescription>
          </Alert>
        ) : null}

        <AlertDialogFooter>
          <AlertDialogCancel type="button">Cancel</AlertDialogCancel>
          <Button
            type="button"
            disabled={blocked}
            onClick={() =>
              publish.mutate(
                {
                  version: snapshot.version,
                  body: {
                    initiatedBy: CURRENT_OPERATOR,
                    environment,
                    targetGatewayIds,
                  },
                },
                {
                  onSuccess: (deployment) => setOutcome(describeDeployment(deployment)),
                },
              )
            }
          >
            {publish.isPending ? 'Publishing…' : `Publish #${snapshot.version}`}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

/**
 * The detail view: the configuration this snapshot will publish, and what
 * gateways are running.
 */
function SnapshotSheet({
  snapshot,
  onClose,
  onCompare,
  canCompare,
}: {
  snapshot: SnapshotResponse
  onClose: () => void
  onCompare: () => void
  canCompare: boolean
}) {
  const { data: deployment } = useSnapshotDeployment(snapshot.version, true)
  const results = validationResultsOf(snapshot)
  const publish = useSnapshotMutations().publish
  const [confirming, setConfirming] = useState(false)
  const [copied, setCopied] = useState(false)

  const copy = async () => {
    const ok = await copyToClipboard(snapshot.content)
    if (ok) {
      setCopied(true)
      // Without the reset the button keeps claiming success indefinitely.
      setTimeout(() => setCopied(false), 2000)
    }
  }

  return (
    <>
      <Sheet open onOpenChange={(open) => !open && onClose()}>
        <SheetContent className="w-full overflow-y-auto sm:max-w-2xl">
          <SheetHeader>
            <SheetTitle>Snapshot #{snapshot.version}</SheetTitle>
            <SheetDescription>
              Created by {snapshot.createdBy} on {formatTimestamp(snapshot.createdAt)}
            </SheetDescription>
          </SheetHeader>

          <div className="space-y-4 px-4 pb-6">
            <div className="flex flex-wrap items-center gap-2">
              <StatusBadge status={snapshot.status} />
              <StatusBadge status={validationVerdict(snapshot).label} />
              <StatusBadge status={publicationState(snapshot).label} />
            </div>

            <Card>
              <CardHeader className="flex flex-row items-start justify-between gap-4">
                <div className="space-y-1.5">
                  <CardTitle>Configuration</CardTitle>
                  <CardDescription>
                    The Ocelot configuration this snapshot publishes to every gateway.
                  </CardDescription>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={copy}
                    disabled={!snapshot.content}
                  >
                    {copied ? (
                      <Check className="size-4" aria-hidden="true" />
                    ) : (
                      <ClipboardCopy className="size-4" aria-hidden="true" />
                    )}
                    {copied ? 'Copied' : 'Copy'}
                  </Button>
                  <Button
                    size="sm"
                    disabled={snapshot.status === 'Archived' || publish.isPending}
                    onClick={() => setConfirming(true)}
                  >
                    <Send aria-hidden="true" className="mr-1 size-4" />
                    Publish
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                {snapshot.content ? (
                  <JsonHighlight payload={snapshot.content} />
                ) : (
                  // The list answered without the document, which is a partial
                  // response rather than an empty configuration.
                  <p className="text-xs text-muted-foreground">
                    This response did not include the configuration document, so there
                    is nothing to preview.
                  </p>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Validation</CardTitle>
                <CardDescription>
                  {results === null
                    ? 'Not reported by the API'
                    : `${results.length} rules checked`}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-1.5">
                {results === null ? (
                  // The column was missing, which points at the server rather than
                  // at this snapshot. Saying "no results" would blame the snapshot
                  // for something it never reported.
                  <p className="text-xs text-muted-foreground">
                    This response did not include validation results, so nothing can be
                    said about whether this snapshot passed.
                  </p>
                ) : results.length === 0 ? (
                  <p className="text-xs text-muted-foreground">
                    No results were recorded. That is not the same as passing.
                  </p>
                ) : (
                  results.map((result) => (
                    <div key={result.rule} className="flex items-start gap-2 text-xs">
                      <StatusBadge
                        status={result.isValid ? 'Passed' : 'Failed'}
                        className="shrink-0"
                      />
                      <div>
                        <div className="font-medium">{result.rule}</div>
                        {result.message ? (
                          <div className="text-muted-foreground">{result.message}</div>
                        ) : null}
                      </div>
                    </div>
                  ))
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Gateway deployment</CardTitle>
                <CardDescription>What gateways are running, per publication</CardDescription>
              </CardHeader>
              <CardContent>
                {deployment ? (
                  <div className="space-y-2 text-xs">
                    <div className="flex items-center gap-2">
                      <StatusBadge status={deployment.status} />
                      <span className="text-muted-foreground">
                        {formatTimestamp(deployment.startedAt)}
                      </span>
                    </div>
                    <dl className="grid gap-1 sm:grid-cols-[110px_1fr] text-muted-foreground">
                      <dt>Publication</dt>
                      <dd className="truncate font-mono">{deployment.publicationId}</dd>
                      <dt>Finished</dt>
                      <dd>{formatTimestamp(deployment.completedAt)}</dd>
                    </dl>
                    {deployment.failureReason ? (
                      <p className="text-destructive">{deployment.failureReason}</p>
                    ) : null}
                  </div>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    This snapshot has never been published, so no gateway is running it.
                  </p>
                )}
              </CardContent>
            </Card>

            {canCompare ? (
              <Button variant="outline" onClick={onCompare}>
                <GitCompare aria-hidden="true" className="size-4" />
                Compare with the previous snapshot
              </Button>
            ) : null}
          </div>
        </SheetContent>
      </Sheet>

      {confirming ? (
        <PublishDialog snapshot={snapshot} onDone={() => setConfirming(false)} />
      ) : null}
    </>
  )
}

/** The diff, against the snapshot immediately before this one. */
function CompareSheet({
  snapshot,
  previous,
  onClose,
}: {
  snapshot: SnapshotResponse
  previous: number | null
  onClose: () => void
}) {
  const { data: comparison, isPending } = useSnapshotComparison(
    snapshot.version,
    previous ?? 0,
    previous !== null,
  )

  return (
    <Sheet open onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="w-full overflow-y-auto sm:max-w-2xl">
        <SheetHeader>
          <SheetTitle>
            #{snapshot.version} compared with #{previous ?? '—'}
          </SheetTitle>
          <SheetDescription>What changed between these two snapshots</SheetDescription>
        </SheetHeader>

        <div className="px-4 pb-6">
          {previous === null ? (
            <p className="text-sm text-muted-foreground">
              The earliest loaded snapshot has nothing to compare against.
            </p>
          ) : isPending ? (
            <Skeleton className="h-32 w-full" />
          ) : comparison && comparison.differences.length > 0 ? (
            <ul className="space-y-1 text-sm">
              {comparison.differences.map((line, index) => {
                const { kind, text } = describeDifference(line)
                return (
                  <li
                    key={index}
                    className={
                      kind === 'removed'
                        ? 'text-destructive'
                        : kind === 'added'
                          ? 'text-muted-foreground'
                          : ''
                    }
                  >
                    <code className="font-mono text-xs">{text}</code>
                  </li>
                )
              })}
            </ul>
          ) : (
            <p className="text-sm text-muted-foreground">
              No differences from #{previous}.
            </p>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}

function Pagination({
  page,
  pageSize,
  totalCount,
  onPage,
}: {
  page: number
  pageSize: number
  totalCount: number
  onPage: (page: number) => void
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (totalPages <= 1) return null

  return (
    <div className="flex items-center justify-between text-sm">
      <p className="text-muted-foreground">
        Showing {pageSize * (page - 1) + 1}–
        {Math.min(pageSize * page, totalCount)} of {totalCount} snapshots
      </p>
      <div className="flex gap-2">
        <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => onPage(page - 1)}>
          Previous
        </Button>
        <Button
          variant="outline"
          size="sm"
          disabled={page >= totalPages}
          onClick={() => onPage(page + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  )
}

function SnapshotsSkeleton() {
  return (
    <div className="space-y-6">
      <Skeleton className="h-8 w-64" />
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Skeleton className="h-28" />
        <Skeleton className="h-28" />
        <Skeleton className="h-28" />
      </div>
      <Skeleton className="h-64 w-full" />
    </div>
  )
}

function formatTimestamp(value: string | null): string {
  if (!value) return '—'
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString()
}
