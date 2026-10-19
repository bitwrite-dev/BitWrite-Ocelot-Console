/**
 * Status → tone mapping.
 *
 * Kept separate from the component so `status-badge.tsx` only exports
 * components (React Fast Refresh) and the mapping can be tested or reused on its
 * own.
 *
 * The status strings are not invented here — they are the exact values the
 * domain produces (`SnapshotStatus`, `PublicationStatus`, `RuntimeStatus`,
 * `LicenseStatus` in Domain/ValueObjects/Status/StatusValueObjects.cs). An
 * unrecognised value maps to `neutral` rather than disappearing, so a new domain
 * status shows up as visible-but-unstyled instead of rendering nothing.
 */

export type StatusTone = 'success' | 'info' | 'warning' | 'destructive' | 'neutral'

export const TONE_CLASSES: Record<StatusTone, string> = {
  success: 'bg-[#43BF8E] text-white',
  info: 'bg-[#5AA9E6] text-white',
  warning: 'bg-[#E8A91C] text-white',
  destructive: 'bg-[#EA6048] text-white',
  neutral: 'bg-[#8BA79F] text-white',
}

const STATUS_TONES: Record<string, StatusTone> = {
  // SnapshotStatus
  Ready: 'info',
  Published: 'success',
  Archived: 'neutral',
  // PublicationStatus
  Pending: 'warning',
  Failed: 'destructive',
  // RuntimeStatus
  Active: 'success',
  Connecting: 'warning',
  Degraded: 'warning',
  Disconnected: 'destructive',
  // LicenseStatus
  Expired: 'destructive',
  Revoked: 'destructive',
}

export function toneForStatus(status: string): StatusTone {
  return STATUS_TONES[status] ?? 'neutral'
}
