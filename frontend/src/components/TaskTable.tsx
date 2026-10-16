import { CalendarClock, Pencil, Trash2 } from 'lucide-react'
import type { Task, TaskStatus } from '../api/types'
import { formatDate, formatDateTime } from '../lib/labels'
import { StatusMenu } from './StatusMenu'
import { PriorityTag, StatusBadge } from './TaskBadges'

interface TaskTableProps {
  tasks: Task[]
  showOwner: boolean
  busyTaskId: string | null
  onEdit: (task: Task) => void
  onDelete: (task: Task) => void
  onChangeStatus: (task: Task, status: TaskStatus) => void
}

export function TaskTable({ tasks, showOwner, busyTaskId, onEdit, onDelete, onChangeStatus }: TaskTableProps) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th scope="col">Tarefa</th>
            <th scope="col">Status</th>
            <th scope="col">Prioridade</th>
            <th scope="col">Prazo</th>
            {showOwner && <th scope="col">Responsável</th>}
            <th scope="col">Atualizada</th>
            <th scope="col" className="table__actions-head">
              <span className="sr-only">Ações</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {tasks.map((task) => {
            const closed = task.status === 'Completed' || task.status === 'Cancelled'
            const busy = busyTaskId === task.id
            return (
              <tr key={task.id} className={closed ? 'table__row--closed' : undefined} data-status={task.status} aria-busy={busy}>
                <td data-label="Tarefa" className="table__title-cell">
                  <span className="table__title">{task.title}</span>
                  {task.description && <span className="table__description">{task.description}</span>}
                </td>
                <td data-label="Status">
                  <StatusBadge status={task.status} />
                </td>
                <td data-label="Prioridade">
                  <PriorityTag priority={task.priority} />
                </td>
                <td data-label="Prazo">
                  <span className={task.isOverdue ? 'due due--overdue' : 'due'}>
                    {task.isOverdue && <CalendarClock size={14} aria-hidden />}
                    {formatDate(task.dueDate)}
                    {task.isOverdue && <span className="sr-only"> (atrasada)</span>}
                  </span>
                </td>
                {showOwner && <td data-label="Responsável">{task.ownerName ?? '—'}</td>}
                <td data-label="Atualizada" className="table__muted">
                  {formatDateTime(task.updatedAt)}
                </td>
                <td className="table__actions">
                  <StatusMenu task={task} disabled={busy} onChange={(status) => onChangeStatus(task, status)} />
                  <button
                    type="button"
                    className="icon-button"
                    onClick={() => onEdit(task)}
                    disabled={closed || busy}
                    aria-label={`Editar ${task.title}`}
                    title={closed ? 'Reabra a tarefa para editar' : 'Editar'}
                  >
                    <Pencil size={16} />
                  </button>
                  <button
                    type="button"
                    className="icon-button icon-button--danger"
                    onClick={() => onDelete(task)}
                    disabled={busy}
                    aria-label={`Excluir ${task.title}`}
                    title="Excluir"
                  >
                    <Trash2 size={16} />
                  </button>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
