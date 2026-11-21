import { UserCircle } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { NavbarThemeToggle } from '@/components/navbar-theme-toggle'
import { EnvironmentSelector } from '@/components/environment-selector'
import { MobileSidebar } from '@/components/sidebar'
import { useAuth } from '@/app-providers'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
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
  const { logout, isAuthenticated, getUsername } = useAuth()
  const username = getUsername()

  return (
    <header className="sticky top-0 z-40 flex h-16 items-center gap-4 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 px-4 md:px-6 flex-wrap">
      <MobileSidebar roles={roles} draftCounts={draftCounts} />

      <div className="flex items-center gap-4 shrink-0 min-w-0">
        <EnvironmentSelector />
      </div>

      <div className="flex-1 min-w-0" />

      <div className="flex items-center gap-2 shrink-0 flex-wrap">
        <Separator className="h-6 hidden md:block border-border" />

        <NavbarThemeToggle />

        {totalDrafts !== null && totalDrafts > 0 && (
          <Badge variant="secondary" className="ml-2 whitespace-nowrap">
            {totalDrafts} pending
          </Badge>
        )}

        {isAuthenticated() && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="hidden sm:flex items-center gap-2 px-3">
                <UserCircle className="size-4" />
                <span className="font-medium">{username}</span>
                <span className="ml-1 size-4 opacity-50">
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="m6 9 6 6 6-6" />
                  </svg>
                </span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onSelect={() => logout()}>
                Logout
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </div>
    </header>
  )
}