import { useState } from 'react'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

import { getApiEnvironment } from '@/api/config'
import {
  ENVIRONMENTS,
  readSelectedEnvironment,
  storeSelectedEnvironment,
} from '@/lib/environment'

/**
 * Runtime environment selector.
 *
 * The control plane stores routes, services and snapshots per environment,
 * so every request must name the environment it operates on via the
 * `X-Environment` header. This selector persists the choice in localStorage
 * so it survives page reloads; the request itself sends it because the HTTP
 * client reads the same stored choice (see `lib/environment`).
 */
export function EnvironmentSelector() {
  // What the request would send right now is what the selector has to show:
  // the stored choice if there is one, otherwise the build's configured
  // environment, and a plain default only when neither is set.
  const [environment, setEnvironment] = useState(
    () => readSelectedEnvironment() || getApiEnvironment() || 'development',
  )

  const handleChange = (value: string) => {
    storeSelectedEnvironment(value)
    setEnvironment(value)
    // Reload so every open view refetches under the new environment: the query
    // caches are keyed per endpoint rather than per environment, and without a
    // reload the pages would keep showing the previous environment's data.
    window.location.reload()
  }

  const label = (value: string) => value.charAt(0).toUpperCase() + value.slice(1)

  return (
    <div className="mx-3 mt-4">
      <label htmlFor="environment-select" className="mb-1 block text-xs font-medium text-muted-foreground">
        Environment
      </label>
      <Select value={environment} onValueChange={handleChange}>
        <SelectTrigger id="environment-select" className="w-full">
          <SelectValue placeholder="Select environment" />
        </SelectTrigger>
        <SelectContent>
          {ENVIRONMENTS.map((value) => (
            <SelectItem key={value} value={value}>
              {label(value)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <p className="mt-1 text-xs text-muted-foreground">
        Select the environment to operate on. Reloads on change.
      </p>
    </div>
  )
}
