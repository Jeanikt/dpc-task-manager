import { ArrowDownWideNarrow, ArrowUpNarrowWide, Search, X } from 'lucide-react'
import type { SortDirection, TaskPriority, TaskSortField, TaskStatus } from '../api/types'
import { PRIORITIES, PRIORITY_LABELS, SORT_LABELS, STATUSES, STATUS_LABELS } from '../lib/labels'

export interface FilterValues {
  search: string
  status?: TaskStatus
  priority?: TaskPriority
  overdue: boolean
  sortBy: TaskSortField
  sortDirection: SortDirection
}

interface TaskFiltersProps {
  values: FilterValues
  onChange: (patch: Partial<FilterValues>) => void
  onClear: () => void
}

export function TaskFilters({ values, onChange, onClear }: TaskFiltersProps) {
  const hasFilters = !!(values.search || values.status || values.priority || values.overdue)

  return (
    <div className="filters" role="search">
      <div className="filters__search">
        <Search size={16} className="filters__search-icon" aria-hidden />
        <input
          type="search"
          className="input"
          placeholder="Pesquisar por título ou descrição"
          aria-label="Pesquisar tarefas"
          value={values.search}
          onChange={(e) => onChange({ search: e.target.value })}
        />
      </div>

      <select
        className="input"
        aria-label="Filtrar por status"
        value={values.status ?? ''}
        onChange={(e) => onChange({ status: (e.target.value || undefined) as TaskStatus | undefined })}
      >
        <option value="">Todos os status</option>
        {STATUSES.map((s) => (
          <option key={s} value={s}>{STATUS_LABELS[s]}</option>
        ))}
      </select>

      <select
        className="input"
        aria-label="Filtrar por prioridade"
        value={values.priority ?? ''}
        onChange={(e) => onChange({ priority: (e.target.value || undefined) as TaskPriority | undefined })}
      >
        <option value="">Todas as prioridades</option>
        {PRIORITIES.map((p) => (
          <option key={p} value={p}>{PRIORITY_LABELS[p]}</option>
        ))}
      </select>

      <label className="checkbox">
        <input type="checkbox" checked={values.overdue} onChange={(e) => onChange({ overdue: e.target.checked })} />
        Somente atrasadas
      </label>

      <div className="filters__sort">
        <select
          className="input"
          aria-label="Ordenar por"
          value={values.sortBy}
          onChange={(e) => onChange({ sortBy: e.target.value as TaskSortField })}
        >
          {(Object.keys(SORT_LABELS) as TaskSortField[]).map((field) => (
            <option key={field} value={field}>{SORT_LABELS[field]}</option>
          ))}
        </select>
        <button
          type="button"
          className="icon-button"
          onClick={() => onChange({ sortDirection: values.sortDirection === 'Asc' ? 'Desc' : 'Asc' })}
          aria-label={values.sortDirection === 'Asc' ? 'Ordem crescente. Alternar para decrescente' : 'Ordem decrescente. Alternar para crescente'}
          title={values.sortDirection === 'Asc' ? 'Crescente' : 'Decrescente'}
        >
          {values.sortDirection === 'Asc' ? <ArrowUpNarrowWide size={16} /> : <ArrowDownWideNarrow size={16} />}
        </button>
      </div>

      {hasFilters && (
        <button type="button" className="button button--ghost button--sm" onClick={onClear}>
          <X size={14} aria-hidden /> Limpar filtros
        </button>
      )}
    </div>
  )
}
