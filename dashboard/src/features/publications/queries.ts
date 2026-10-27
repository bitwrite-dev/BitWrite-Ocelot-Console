import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

import { useApi } from '@/app-providers'
import type {
  PublicationResponse,
  CreatePublicationRequest,
  ExecutePublicationRequest,
  CancelPublicationRequest,
} from '@/api'

/**
 * Publications: records of which snapshot was sent to which gateways, when, and
 * what the outcome was.
 */

export const publicationKeys = {
  list: (page: number) =>
    ['publications', 'list', page] as const,
  current: () => ['publications', 'current'] as const,
  history: () => ['publications', 'history'] as const,
  detail: (id: string) => ['publications', 'detail', id] as const,
  create: () => ['publications', 'create'] as const,
  progress: (id: string) => ['publications', 'progress', id] as const,
  diff: (targetVersion: string, currentVersion?: string) => ['publications', 'diff', targetVersion, currentVersion] as const,
}

const PAGE_SIZE = 20

export function usePublications(page: number) {
  const api = useApi()

  return useQuery({
    queryKey: publicationKeys.list(page),
    queryFn: ({ signal }) =>
      api.resources.publications.list({ page, pageSize: PAGE_SIZE, signal }),
    placeholderData: (previous) => previous,
  })
}

export function useCurrentPublication() {
  const api = useApi()

  return useQuery({
    queryKey: publicationKeys.current(),
    queryFn: ({ signal }) => api.resources.publications.current({ signal }),
    placeholderData: (previous) => previous,
  })
}

export function usePublicationHistory() {
  const api = useApi()

  return useQuery({
    queryKey: publicationKeys.history(),
    queryFn: ({ signal }) => api.resources.publications.history({ signal }),
  })
}

export function usePublicationDetail(id: string, enabled: boolean) {
  const api = useApi()

  return useQuery({
    queryKey: publicationKeys.detail(id),
    queryFn: ({ signal }) => api.resources.publications.detail(id, { signal }),
    enabled: enabled && !!id,
  })
}

/**
 * Create a new publication record for review.
 */
export function useCreatePublication() {
  const api = useApi()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreatePublicationRequest) =>
      api.resources.publications.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['publications'] })
      queryClient.invalidateQueries({ queryKey: ['publications', 'current'] })
      queryClient.invalidateQueries({ queryKey: ['overview'] })
    },
  })
}

/**
 * Execute a created publication.
 */
export function useExecutePublication() {
  const api = useApi()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: ExecutePublicationRequest }) =>
      api.resources.publications.execute(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['publications'] })
      queryClient.invalidateQueries({ queryKey: ['publications', 'current'] })
      queryClient.invalidateQueries({ queryKey: ['overview'] })
    },
  })
}

/**
 * Get real-time progress of a publication.
 */
export function usePublicationProgress(id: string, enabled: boolean) {
  const api = useApi()

  return useQuery({
    queryKey: publicationKeys.progress(id),
    queryFn: ({ signal }) => api.resources.publications.progress(id, { signal }),
    enabled: enabled && !!id,
    refetchInterval: 2000, // Poll every 2 seconds
    placeholderData: (previous) => previous,
  })
}

/**
 * Cancel a pending publication.
 */
export function useCancelPublication() {
  const api = useApi()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: CancelPublicationRequest }) =>
      api.resources.publications.cancel(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['publications'] })
      queryClient.invalidateQueries({ queryKey: ['publications', 'current'] })
      queryClient.invalidateQueries({ queryKey: ['overview'] })
    },
  })
}
export function usePublicationDiff(targetVersion: string, currentVersion?: string, enabled: boolean = true) {
  const api = useApi()

  return useQuery({
    queryKey: publicationKeys.diff(targetVersion, currentVersion),
    queryFn: ({ signal }) =>
      api.resources.publications.diff({
        targetSnapshotVersion: targetVersion,
        currentSnapshotVersion: currentVersion,
        signal,
      }),
    enabled: enabled && !!targetVersion,
  })
}

/**
 * Whether a publication is currently the one gateways are running.
 */
export function publicationStatus(
  publication: Pick<PublicationResponse, 'status'>,
): { label: string; tone: 'ok' | 'bad' | 'warn' | 'mute' } {
  const status = publication.status.toLowerCase()

  if (status === 'published' || status.includes('complete') || status.includes('success')) {
    return { label: 'Completed', tone: 'ok' }
  }
  if (status === 'pending' || status.includes('progress') || status.includes('started')) {
    return { label: 'In Progress', tone: 'warn' }
  }
  if (status === 'failed' || status.includes('fail') || status.includes('error')) {
    return { label: 'Failed', tone: 'bad' }
  }
  if (status === 'rolledback') {
    return { label: 'Rolled Back', tone: 'mute' }
  }
  return { label: 'Unknown', tone: 'mute' }
}

/**
 * Format a timestamp for display.
 */
export function formatTimestamp(value: string | null): string {
  if (!value) return '—'
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString()
}

/**
 * Shorten a hash for display.
 */
export function shortHash(hash: string): string {
  if (!hash) return '—'
  return hash.length <= 14 ? hash : `${hash.slice(0, 12)}…`
}