import { Monitor, Moon, Sun } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { THEMES, useTheme, type Theme } from '@/features/theme/theme-provider'

const OPTIONS: ReadonlyArray<{ value: Theme; label: string; icon: typeof Sun }> = [
  { value: 'light', label: 'Light', icon: Sun },
  { value: 'dark', label: 'Dark', icon: Moon },
  { value: 'system', label: 'System', icon: Monitor },
]

/**
 * Light / Dark / System theme toggle for navbar (background).
 */
export function NavbarThemeToggle() {
  const { theme, setTheme } = useTheme()

  return (
    <div role="radiogroup" aria-label="Colour theme" className="flex items-center gap-1">
      {OPTIONS.map(({ value, label, icon: Icon }) => {
        const selected = theme === value
        return (
          <Button
            key={value}
            type="button"
            role="radio"
            aria-checked={selected}
            aria-label={label}
            title={label}
            size="icon"
            variant={selected ? 'secondary' : 'ghost'}
            className="size-8 shrink-0"
            onClick={() => setTheme(value)}
          >
            <Icon aria-hidden="true" className="size-4" />
          </Button>
        )
      })}
    </div>
  )
}

export { THEMES }