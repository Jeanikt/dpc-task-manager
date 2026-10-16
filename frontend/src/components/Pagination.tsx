import { ChevronLeft, ChevronRight } from 'lucide-react'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
}

export function Pagination({ page, pageSize, totalCount, totalPages, onPageChange, onPageSizeChange }: PaginationProps) {
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <nav className="pagination" aria-label="Paginação">
      <span className="pagination__info">
        {from}–{to} de {totalCount}
      </span>
      <label className="pagination__size">
        Por página
        <select className="input input--sm" value={pageSize} onChange={(e) => onPageSizeChange(Number(e.target.value))}>
          {[10, 20, 50].map((n) => (
            <option key={n} value={n}>{n}</option>
          ))}
        </select>
      </label>
      <div className="pagination__buttons">
        <button
          type="button"
          className="icon-button"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Página anterior"
        >
          <ChevronLeft size={16} />
        </button>
        <span className="pagination__page">
          {page} / {Math.max(totalPages, 1)}
        </span>
        <button
          type="button"
          className="icon-button"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Próxima página"
        >
          <ChevronRight size={16} />
        </button>
      </div>
    </nav>
  )
}
