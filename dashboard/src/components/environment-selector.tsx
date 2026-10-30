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
    <div className="flex items-center gap-2">
      <label htmlFor="environment-select" className="text-xs font-medium text-primary-foreground hidden sm:block">
        Environment
      </label>
      <Select value={environment} onValueChange={handleChange}>
        <SelectTrigger id="environment-select" className="w-[160px] bg-primary-foreground/10 border-primary/30 text-primary-foreground">
          <SelectValue placeholder="Select environment" />
        </SelectTrigger>
        <SelectContent className="bg-popover border-border">
          {ENVIRONMENTS.map((value) => (
            <SelectItem key={value} value={value}>
              {label(value)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}
