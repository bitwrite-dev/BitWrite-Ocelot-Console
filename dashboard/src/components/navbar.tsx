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
import { NavbarThemeToggle } from '@/components/navbar-theme-toggle'
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
    <header className="sticky top-0 z-40 flex h-16 items-center gap-4 border-b border-primary bg-primary/95 backdrop-blur supports-[backdrop-filter]:bg-primary/60 px-4 md:px-6 flex-wrap">
      <MobileSidebar roles={roles} draftCounts={draftCounts} />

      <div className="flex items-center gap-4 shrink-0 min-w-0">
        <EnvironmentSelector />
      </div>

      <div className="flex-1 min-w-0" />

      <div className="flex items-center gap-2 shrink-0 flex-wrap">
        <Separator className="h-6 hidden md:block border-primary/30" />

        <NavbarThemeToggle />

        {totalDrafts !== null && totalDrafts > 0 && (
          <Badge variant="secondary" className="ml-2 whitespace-nowrap">
            {totalDrafts} pending
          </Badge>
        )}
      </div>
    </header>
  )
}