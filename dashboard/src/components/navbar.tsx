import { Menu } from 'lucide-react'
import { NavLink } from 'react-router-dom'

import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { ThemeToggle } from '@/components/theme-toggle'
import { EnvironmentSelector } from '@/components/environment-selector'
import { MobileSidebar } from '@/components/sidebar'
import type { Role } from '@/navigation'

type DraftCounts = {
  routes: number
  services: number
  globalConfig: number
} | null

interface NavbarProps {
  roles?: Role[]
  draftCounts: DraftCounts
  totalDrafts: number | null
}

export function Navbar({
  roles,
  draftCounts,
  totalDrafts,
}: NavbarProps) {
  return (
    <header className="sticky top-0 z-40 flex h-16 items-center gap-4 border-b border-sidebar-border bg-sidebar/95 backdrop-blur supports-[backdrop-filter]:bg-sidebar/60 px-4 md:px-6">
      <MobileSidebar roles={roles} draftCounts={draftCounts} />

      <div className="flex items-center gap-4">
        <span className="font-heading text-lg font-semibold hidden sm:block text-sidebar-foreground">
          Ocelot Console
        </span>
      </div>

      <div className="flex-1" />

      <div className="flex items-center gap-3">
        <EnvironmentSelector />

        <Separator className="h-6" />

        <ThemeToggle />

        {totalDrafts !== null && totalDrafts > 0 && (
          <Badge variant="destructive" className="ml-2">
            {totalDrafts} pending
          </Badge>
        )}
      </div>
    </header>
  )
}