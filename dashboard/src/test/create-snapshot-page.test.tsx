import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { createApiClient } from '@/api'
import { ApiClientContext } from '@/app-providers'
import { CreateSnapshotPage } from '@/features/snapshots/create-snapshot-page'
import { pendingCaptureCount } from '@/features/snapshots/create-model'
import type { PreviewSnapshotResponse, SnapshotResponse } from '@/api'

const PUBLISHED: SnapshotResponse = {
  version: 104,
  hash: 'sha256:aaaaaaaaaaaaaaaaaaaa',
  content: '{}',
  status: 'Published',
  createdBy: 'alex',
  createdAt: '2026-01-02T03:04:05Z',
  publishedAt: '2026-01-02T04:00:00Z',
  archivedAt: null,
  routeCount: 126,
  serviceCount: 18,
  pluginVersions: [],
  validationResults: [],
}

const CLEAN_PREVIEW: PreviewSnapshotResponse = {
  content: '{"Routes":[]}',
  hash: 'sha256:previewhash0000000000000000000000000000000000000000000000000000000',
  routeCount: 2,
  serviceCount: 1,
  pluginVersions: [],
  validationResults: [
    { rule: 'RouteConflicts', isValid: true, message: null },
    { rule: 'References', isValid: true, message: null },
    { rule: 'OcelotCapabilities', isValid: true, message: null },
  ],
  isValid: true,
  ocelotVersion: '20.0.0',
  nextVersion: 105,
}

type Handler = (url: string, method: string, body: unknown) => Response

const ok = (payload: unknown) =>
  new Response(JSON.stringify(payload), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  })

const fail = (status: number, error: string) =>
  new Response(JSON.stringify({ error }), {
    status,
    headers: { 'content-type': 'application/json' },
  })

function renderCreate(handler: Handler) {
  const calls: { url: string; method: string; body: unknown }[] = []

  const fetchImpl = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    const body = init?.body ? JSON.parse(String(init.body)) : undefined
    calls.push({ url, method, body })
    return handler(url, method, body)
  })

  const client = createApiClient({
    baseUrl: 'http://api.test',
    fetchImpl: fetchImpl as unknown as typeof fetch,
  })

  const router = createMemoryRouter(
    [
      { path: '/snapshots/new', element: <CreateSnapshotPage /> },
      { path: '/snapshots', element: <p>Snapshot list</p> },
    ],
    { initialEntries: ['/snapshots/new'] },
  )

  render(
    <QueryClientProvider
      client={new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 } } })}
    >
      <ApiClientContext.Provider value={client}>
        <RouterProvider router={router} />
      </ApiClientContext.Provider>
    </QueryClientProvider>,
  )

  return { calls }
}

/**
 * A route as the list endpoint returns it.
 *
 * Only what the page reads is filled in: the counts and the pending-capture
 * flag. The pending flag is the field the whole question turns on, so a fixture
 * that omits it cannot say whether anything is left to capture.
 */
const routeFixture = (id: string, isDraft: boolean) => ({
  id,
  key: `GET:/${id}`,
  method: 'GET',
  upstreamPath: `/${id}`,
  host: null,
  serviceId: 'svc-1',
  isDraft,
})

const serviceFixture = (id: string, isDraft: boolean) => ({
  id,
  name: 'orders',
  description: null,
  downstreamTargets: [],
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  isDraft,
})

const baseline =
  (preview: PreviewSnapshotResponse = CLEAN_PREVIEW, created?: SnapshotResponse): Handler =>
  (url, method) => {
    if (url.endsWith('/preview')) return ok(preview)
    if (url.endsWith('/api/v1/snapshots') && method === 'POST') {
      return ok(created ?? { ...PUBLISHED, version: 105, status: 'Ready', publishedAt: null })
    }
    if (url.includes('/api/v1/routes')) {
      return ok({
        routes: [routeFixture('r-1', true), routeFixture('r-2', true)],
        totalCount: 2,
        page: 1,
        pageSize: 50,
      })
    }
    if (url.includes('/api/v1/services')) {
      return ok({
        services: [serviceFixture('s-1', true)],
        totalCount: 1,
        page: 1,
        pageSize: 50,
      })
    }
    if (url.includes('/api/v1/snapshots')) {
      return ok({ snapshots: [PUBLISHED], totalCount: 1, page: 1, pageSize: 20 })
    }
    return ok({})
  }

/**
 * The same installation with nothing pending capture.
 *
 * Every route and the service have been sealed by a snapshot, so the page is
 * looking at a state a snapshot already carries.
 */
const settledBaseline: Handler = (url) => {
  if (url.endsWith('/preview')) return ok(CLEAN_PREVIEW)
  if (url.includes('/api/v1/routes')) {
    return ok({
      routes: [routeFixture('r-1', false), routeFixture('r-2', false)],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    })
  }
  if (url.includes('/api/v1/services')) {
    return ok({
      services: [serviceFixture('s-1', false)],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    })
  }
  if (url.includes('/api/v1/snapshots')) {
    return ok({ snapshots: [PUBLISHED], totalCount: 1, page: 1, pageSize: 20 })
  }
  return ok({})
}

/** Walks from step 1 to step 3, which is what a passing validation unlocks. */
async function advanceToSeal() {
  await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))
  await screen.findByText('Resolved configuration')
  await userEvent.click(await screen.findByRole('button', { name: /continue to review/i }))
}

describe('the three steps', () => {
  it('starts on step one, not on the seal', () => {
    renderCreate(baseline())

    expect(screen.getByText('Step 1 of 3')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /continue to validation/i })).toBeInTheDocument()
  })

  it('resolves the configuration with the page, because step 1 shows it', async () => {
    // The source step displays what would be captured, so the artifact is
    // resolved rather than deferred: an operator cannot read a document nobody
    // fetched.
    const { calls } = renderCreate(baseline())

    await waitFor(() => {
      expect(calls.some((call) => call.url.endsWith('/preview'))).toBe(true)
    })
    expect(await screen.findByText('Configuration to capture')).toBeInTheDocument()
  })

  it('re-runs the preview when the operator moves on, so the verdict is for the current state', async () => {
    // The state can move while the operator reads, so reaching step 2 resolves
    // again rather than trusting what was on screen when the page loaded.
    const { calls } = renderCreate(baseline())

    await waitFor(() => {
      expect(calls.some((call) => call.url.endsWith('/preview'))).toBe(true)
    })
    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))

    await waitFor(() => {
      const previews = calls.filter((call) => call.url.endsWith('/preview'))
      expect(previews.length).toBeGreaterThan(1)
    })
    expect(await screen.findByText('Step 2 of 3')).toBeInTheDocument()
  })

  it('really runs the validation when the step is reached', async () => {
    const { calls } = renderCreate(baseline())

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))

    await waitFor(() => {
      expect(calls.some((call) => call.url.endsWith('/preview') && call.method === 'POST')).toBe(true)
    })
    expect(await screen.findByText('Step 2 of 3')).toBeInTheDocument()
  })

  it('shows the hash the artifact would carry, not one it might get', async () => {
    renderCreate(baseline())

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))

    // The server computed this over the exact string it would store, so the
    // operator sees the hash rather than trusting that one will appear later.
    expect(await screen.findByText('sha256:previ…')).toBeInTheDocument()
  })

  it('lists every rule it ran, not just the verdict', async () => {
    renderCreate(baseline())

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))

    expect(await screen.findByText('RouteConflicts')).toBeInTheDocument()
    expect(screen.getByText('References')).toBeInTheDocument()
    expect(screen.getByText('OcelotCapabilities')).toBeInTheDocument()
  })

  it('offers the seal only after validation passed', async () => {
    renderCreate(baseline())

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))
    await screen.findByText('Step 2 of 3')

    // Sealing an artifact that failed would record a broken configuration as
    // the desired state.
    expect(screen.getByRole('button', { name: /continue to review/i })).toBeEnabled()
  })

  it('refuses to move past a failed validation', async () => {
    renderCreate(
      baseline({
        ...CLEAN_PREVIEW,
        isValid: false,
        validationResults: [
          {
            rule: 'RouteConflicts',
            isValid: false,
            message: 'Overlapping routes: GET:/api/orders localhost',
          },
        ],
      }),
    )

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))

    expect(await screen.findByText('Validation failed')).toBeInTheDocument()
    expect(screen.getByText(/overlapping routes/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /continue to review/i })).toBeDisabled()
  })

  it('reports that nothing could be built, rather than offering an empty artifact', async () => {
    renderCreate(
      baseline({
        ...CLEAN_PREVIEW,
        content: null,
        hash: null,
        isValid: false,
        validationResults: [
          {
            rule: 'ConfigurationBuild',
            isValid: false,
            message: 'The management state could not be turned into a configuration',
          },
        ],
      }),
    )

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))

    expect(
      await screen.findByText(/nothing could be built from the current state/i),
    ).toBeInTheDocument()
    expect(screen.getByText(/no hash — nothing was built/i)).toBeInTheDocument()
  })

  it('shows a warning without blocking, because clearing a gateway is deliberate', async () => {
    renderCreate(
      baseline({
        ...CLEAN_PREVIEW,
        routeCount: 0,
        validationResults: [
          { rule: 'HasContent', isValid: false, message: 'Warning: no routes are configured.' },
        ],
      }),
    )

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))

    expect(await screen.findByText(/warning: no routes are configured/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /continue to review/i })).toBeEnabled()
  })

  it('names the version it will take, so the seal is not a surprise', async () => {
    renderCreate(baseline())

    await advanceToSeal()

    expect(await screen.findByText(/becomes snapshot #105/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /create snapshot #105/i })).toBeInTheDocument()
  })

  it('creates the snapshot and says it is not live yet', async () => {
    const { calls } = renderCreate(baseline())

    await advanceToSeal()
    await userEvent.click(screen.getByRole('button', { name: /create snapshot #105/i }))

    await waitFor(() => {
      expect(
        calls.some((call) => call.url.endsWith('/api/v1/snapshots') && call.method === 'POST'),
      ).toBe(true)
    })

    expect(await screen.findByText('Snapshot #105 created')).toBeInTheDocument()
    expect(screen.getByText(/not running anywhere until it is published/i)).toBeInTheDocument()
  })

  it('lets the operator re-run validation after editing the state', async () => {
    const { calls } = renderCreate(baseline())

    await userEvent.click(await screen.findByRole('button', { name: /continue to validation/i }))
    await screen.findByText('Step 2 of 3')
    await userEvent.click(screen.getByRole('button', { name: /re-run validation/i }))

    await waitFor(() => {
      const previews = calls.filter((call) => call.url.endsWith('/preview'))
      expect(previews.length).toBeGreaterThan(1)
    })
  })
})

describe('what the source step shows', () => {
  it('counts what is about to be sealed, and how much of it is still pending', async () => {
    renderCreate(baseline())

    // The counts beside the source say what exists; the draft counts say what a
    // snapshot has not taken yet. The two were the same number before, because
    // the flag they read did not exist.
    expect(await screen.findByText('2 routes (2 draft)')).toBeInTheDocument()
    expect(screen.getByText('1 services (1 draft)')).toBeInTheDocument()
  })

  it('says so and pauses when nothing has changed since the last snapshot', async () => {
    renderCreate(settledBaseline)

    // A re-seal of an unchanged configuration is exactly what the operator asked
    // not to be offered, so the step says why rather than sending them on.
    expect(
      await screen.findByText('Nothing has changed since the last snapshot'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /continue to validation/i })).toBeDisabled()
  })

  it('still lets a deliberate re-seal through', async () => {
    renderCreate(settledBaseline)

    // The pause is not a refusal: a fresh baseline or a cleanup snapshot is
    // occasionally deliberate, and hiding the way through would break it.
    await userEvent.click(await screen.findByRole('button', { name: /continue anyway/i }))

    expect(await screen.findByText('Step 2 of 3')).toBeInTheDocument()
  })

  it('cannot claim nothing to capture from a partial read', () => {
    // The counts answer from one page, and a page that holds no drafts says
    // nothing about the rows behind it. "Nothing pending" is a claim about the
    // whole state, so a partial read answers null rather than zero — and the
    // page reads null as "keep going".
    expect(pendingCaptureCount({ routes: 0, services: 0, globalConfig: 0 }, false)).toBeNull()

    // Read in full, the same answer is a claim the gate can act on.
    expect(pendingCaptureCount({ routes: 0, services: 0, globalConfig: 0 }, true)).toBe(0)
    expect(pendingCaptureCount({ routes: 1, services: 2, globalConfig: 1 }, true)).toBe(4)
  })

  it('shows the configuration the snapshot would capture, as JSON', async () => {
    renderCreate(
      baseline({
        ...CLEAN_PREVIEW,
        content: '{"Routes":[{"UpstreamPathTemplate":"/orders"}]}',
      }),
    )

    // The document the operator is agreeing to seal, not a description of it:
    // "2 routes and 1 service" says how much, and this says what.
    expect(await screen.findByText('Configuration to capture')).toBeInTheDocument()
    await waitFor(() => {
      expect(document.body.textContent).toContain('"UpstreamPathTemplate"')
      expect(document.body.textContent).toContain('"/orders"')
    })
    expect(screen.getByText(/would seal as #105/i)).toBeInTheDocument()
  })

  it('says plainly when the state did not resolve into a configuration', async () => {
    renderCreate(baseline({ ...CLEAN_PREVIEW, content: null, hash: null }))

    expect(await screen.findByText('Nothing to capture yet')).toBeInTheDocument()
    expect(screen.getByText(/a snapshot of it would seal nothing/i)).toBeInTheDocument()
  })

  it('explains a failed resolution without failing the step', async () => {
    renderCreate((url, method) =>
      url.endsWith('/preview')
        ? fail(500, 'resolver unavailable')
        : baseline()(url, method, undefined),
    )

    expect(await screen.findByText('The configuration could not be resolved')).toBeInTheDocument()
    expect(screen.getByText(/resolver unavailable/i)).toBeInTheDocument()
    // The step still works: continuing resolves it again, where a failure is
    // reported against the rules that were checked.
    expect(screen.getByRole('button', { name: /continue to validation/i })).toBeEnabled()
  })

  it('does not claim zero when the counts could not be read', async () => {
    renderCreate(() => fail(500, 'down'))

    // "0 routes" would read as "about to seal an empty artifact", which is the
    // most damaging thing this page could say while the API is broken.
    expect(await screen.findByText('— routes')).toBeInTheDocument()
  })

  it('says plainly that creating does not publish', () => {
    renderCreate(baseline())

    expect(
      screen.getByText(/creating a snapshot does not publish it to gateways/i),
    ).toBeInTheDocument()
  })

  it('offers no baseline when nothing has been published', async () => {
    renderCreate((url) => {
      if (url.includes('/api/v1/routes')) return ok({ routes: [], totalCount: 0, page: 1, pageSize: 1 })
      if (url.includes('/api/v1/services')) {
        return ok({ services: [], totalCount: 0, page: 1, pageSize: 1 })
      }
      if (url.includes('/api/v1/snapshots')) {
        return ok({ snapshots: [], totalCount: 0, page: 1, pageSize: 20 })
      }
      return ok({})
    })

    expect(
      await screen.findByText(/no snapshot has been published yet/i),
    ).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: /use a published snapshot/i })).toBeDisabled()
  })

  it('admits the API has one source, so a baseline is a record and not a different artifact', () => {
    renderCreate(baseline())

    // Silently pretending the baseline changes what gets captured would make the
    // recorded hash describe a file the operator never saw.
    expect(
      screen.getByText(/a baseline only records what this snapshot will be judged against/i),
    ).toBeInTheDocument()
  })

  it('tells the operator their label and summary are not stored yet', async () => {
    renderCreate(baseline())

    await advanceToSeal()
    await userEvent.type(screen.getByLabelText(/snapshot label/i), 'payments policy')

    // Silently dropping what someone typed is worse than saying it will not be
    // kept, because they would assume the audit trail had it.
    expect(
      screen.getByText(/does not yet store the label or change summary/i),
    ).toBeInTheDocument()
  })
})
