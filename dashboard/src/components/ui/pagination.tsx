import { cn } from 'cn'
import { ChevronLeft, ChevronRight } from 'lucide-react'

import { Button } from '@/components/ui/button'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  onPage: (page: number) => void
  className?: string
}

function calculateTotalPages(totalCount: number, pageSize: number): number {
  return Math.max(1, Math.ceil(totalCount / pageSize))
}

export function Pagination({ page, pageSize, totalCount, onPage, className }: PaginationProps) {
  const totalPages = calculateTotalPages(totalCount, pageSize)

  if (totalPages <= 1) return null

  const startPage = Math.max(1, page - 2)
  const endPage = Math.min(totalPages, startPage + 4)

  return (
    <nav
      className={cn('flex items-center justify-center gap-2', className)}
      aria-label="Pagination"
    >
      <Button
        variant="ghost"
        size="sm"
        onClick={() => onPage(1)}
        disabled={page === 1}
        aria-label="First page"
        title="First page"
      >
        <ChevronLeft className="size-4" />
      </Button>

      <Button
        variant="ghost"
        size="sm"
        onClick={() => onPage(page - 1)}
        disabled={page === 1}
        aria-label="Previous page"
        title="Previous page"
      >
        <ChevronLeft className="size-4" />
      </Button>

      <div className="flex items-center gap-1">
        {Array.from({ length: endPage - startPage + 1 }, (_, i) => startPage + i).map((p) => (
          <Button
            key={p}
            variant={p === page ? 'default' : 'ghost'}
            size="sm"
            onClick={() => onPage(p)}
            aria-label={`Page ${p}`}
            aria-current={p === page ? 'page' : undefined}
            className="w-8 h-8"
          >
            {p}
          </Button>
        ))}
      </div>

      <Button
        variant="ghost"
        size="sm"
        onClick={() => onPage(page + 1)}
        disabled={page === totalPages}
        aria-label="Next page"
        title="Next page"
      >
        <ChevronRight className="size-4" />
      </Button>

      <Button
        variant="ghost"
        size="sm"
        onClick={() => onPage(totalPages)}
        disabled={page === totalPages}
        aria-label="Last page"
        title="Last page"
      >
        <ChevronRight className="size-4" />
      </Button>
    </nav>
  )
}