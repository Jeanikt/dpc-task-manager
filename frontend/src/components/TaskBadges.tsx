import { ArrowDown, ArrowRight, ArrowUp } from 'lucide-react'
import type { TaskPriority, TaskStatus } from '../api/types'
import { PRIORITY_LABELS, STATUS_LABELS } from '../lib/labels'

export function StatusBadge({ status }: { status: TaskStatus }) {
  return (
    <span className={`badge badge--${status}`}>
      <span className="badge__dot" aria-hidden />
      {STATUS_LABELS[status]}
    </span>
  )
}

const PRIORITY_ICONS = { Low: ArrowDown, Medium: ArrowRight, High: ArrowUp } as const

export function PriorityTag({ priority }: { priority: TaskPriority }) {
  const Icon = PRIORITY_ICONS[priority]
  return (
    <span className={`priority priority--${priority}`}>
      <Icon size={14} aria-hidden />
      {PRIORITY_LABELS[priority]}
    </span>
  )
}
