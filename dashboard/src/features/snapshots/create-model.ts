import { useQuery } from '@tanstack/react-query'

import { useApi } from '@/app-providers'
import type { PreviewSnapshotResponse, SnapshotListResponse } from '@/api'

import { useSnapshotMutations } from './queries'

/**
 * The data behind the create page's three steps.
 *
 * The counts come from the totals the list endpoints already return, so the page
 * can show what is about to be sealed without adding an endpoint that only
 * supports one screen.
 */

/** What the current management state holds, by count and draft status. */
export function useCreateSourceCounts() {
  const api = useApi()

  // One page wide enough for a realistic installation. The page has to read the
  // routes themselves, not only their total, because the pending question is
  // about which of them are draft — and a page of one route would answer that
  // about one route.
  const SOURCE_PAGE_SIZE = 50

  const routes = useQuery({
    queryKey: ['routes', 'list', 1, SOURCE_PAGE_SIZE],
    queryFn: ({ signal }) =>
      api.resources.routes.list({ page: 1, pageSize: SOURCE_PAGE_SIZE, signal }),
    staleTime: 30_000,
  })

  const services = useQuery({
    queryKey: ['services', 'list', 1, SOURCE_PAGE_SIZE],
    queryFn: ({ signal }) =>
      api.resources.services.list({ page: 1, pageSize: SOURCE_PAGE_SIZE, signal }),
    staleTime: 30_000,
  })

  const globalConfig = useQuery({
    queryKey: ['global-configuration', 'current'],
    queryFn: ({ signal }) => api.resources.globalConfiguration.get({ signal }),
    staleTime: 30_000,
  })

  // Whether the whole of both lists was read. A shorter page than the total says
  // the rest was not, and a pending count computed from a partial read would
  // understate — the failure mode that blocks a snapshot that should be created.
  const allRead =
    routes.data !== undefined &&
    services.data !== undefined &&
    routes.data.routes.length >= routes.data.totalCount &&
    services.data.services.length >= services.data.totalCount

  return {
    isPending: routes.isPending || services.isPending || globalConfig.isPending,
    allRead,
    counts: {
      routes: routes.data ? routes.data.totalCount : null,
      services: services.data ? services.data.totalCount : null,
    },
    draftStatus: {
      routes: routes.data ? routes.data.routes.filter((r) => r.isDraft).length : null,
      services: services.data ? services.data.services.filter((s) => s.isDraft).length : null,
      globalConfig: globalConfig.data && globalConfig.data.isDraft ? 1 : 0,
    },
  }
}

/**
 * How much of the management state is still pending capture, or null when that
 * cannot be said.
 *
 * Null rather than zero when the lists were not read in full: "nothing to
 * capture" is a claim about the whole state, and a claim made from a page of it
 * would block exactly the snapshot that should have been created.
 */
export function pendingCaptureCount(
  draftStatus: {
    routes: number | null
    services: number | null
    globalConfig: number | null
  },
  allRead: boolean,
): number | null {
  if (!allRead) return null
  return (draftStatus.routes ?? 0) + (draftStatus.services ?? 0) + (draftStatus.globalConfig ?? 0)
}

/**
 * The most recently published snapshot, if any.
 *
 * The list is newest-first, so the first row that was published is the one
 * gateways are meant to be running. A snapshot that was published and later
 * archived keeps its `publishedAt`, which is exactly what makes it a candidate.
 */
export function useLatestPublishedSnapshot() {
  const api = useApi()

  const query = useQuery({
    queryKey: ['snapshots', 'published-latest'],
    queryFn: ({ signal }) =>
      api.resources.snapshots.list({ page: 1, pageSize: 20, signal }) as Promise<SnapshotListResponse>,
    staleTime: 30_000,
  })

  return {
    isPending: query.isPending,
    snapshot: query.data?.snapshots.find((snapshot) => snapshot.publishedAt !== null) ?? null,
  }
}

/**
 * Resolves the artifact a snapshot would contain.
 *
 * Loaded with the page rather than on demand: step 1 shows the configuration
 * the snapshot would capture, and the counts beside the source options say how
 * much there is while this shows what it is. Step 2 re-runs it, so the
 * validation verdict is always for the state as it is then rather than for
 * whatever was on screen when the page loaded.
 */
export function usePreviewSnapshot() {
  const api = useApi()

  const query = useQuery({
    queryKey: ['snapshots', 'preview'],
    queryFn: ({ signal }) => api.resources.snapshots.preview({ signal }),
    // The management state can move under the operator, so a preview is never
    // reused across a revisit without an explicit re-run.
    staleTime: 0,
    gcTime: 0,
  })

  return {
    data: query.data as PreviewSnapshotResponse | null,
    isFetching: query.isFetching,
    isError: query.isError,
    error: query.error,
    run: () => query.refetch(),
  }
}

/** Creating, plus what came of it. */
export function useSnapshotCreate() {
  const { create } = useSnapshotMutations()

  return {
    create,
    isPending: create.isPending,
    error: create.error,
    data: create.data,
  }
}
