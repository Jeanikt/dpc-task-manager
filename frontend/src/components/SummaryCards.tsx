import type { TaskStatus, TaskSummary } from '../api/types'

export type SummaryFilter = { status?: TaskStatus; overdue?: boolean }

interface SummaryCardsProps {
  summary: TaskSummary | undefined
  active: SummaryFilter
  onSelect: (filter: SummaryFilter) => void
}

const CARDS: { key: string; label: string; value: (s: TaskSummary) => number; filter: SummaryFilter; tone: string }[] = [
  { key: 'total', label: 'Total', value: (s) => s.total, filter: {}, tone: 'neutral' },
  { key: 'pending', label: 'Pendentes', value: (s) => s.pending, filter: { status: 'Pending' }, tone: 'Pending' },
  { key: 'progress', label: 'Em andamento', value: (s) => s.inProgress, filter: { status: 'InProgress' }, tone: 'InProgress' },
  { key: 'completed', label: 'Concluídas', value: (s) => s.completed, filter: { status: 'Completed' }, tone: 'Completed' },
  { key: 'overdue', label: 'Atrasadas', value: (s) => s.overdue, filter: { overdue: true }, tone: 'overdue' },
]

function isActive(card: SummaryFilter, active: SummaryFilter) {
  return card.status === active.status && !!card.overdue === !!active.overdue
}

export function SummaryCards({ summary, active, onSelect }: SummaryCardsProps) {
  return (
    <section className="summary" aria-label="Resumo das tarefas">
      {CARDS.map((card) => {
        const selected = isActive(card.filter, active)
        return (
          <button
            key={card.key}
            type="button"
            className={`summary__card summary__card--${card.tone}`}
            aria-pressed={selected}
            onClick={() => onSelect(selected ? {} : card.filter)}
          >
            <span className="summary__label">{card.label}</span>
            <span className="summary__value">{summary ? card.value(summary) : '–'}</span>
          </button>
        )
      })}
    </section>
  )
}
