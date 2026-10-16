import type { TaskPriority, TaskSortField, TaskStatus } from '../api/types'

export const STATUS_LABELS: Record<TaskStatus, string> = {
  Pending: 'Pendente',
  InProgress: 'Em andamento',
  Completed: 'Concluída',
  Cancelled: 'Cancelada',
}

export const PRIORITY_LABELS: Record<TaskPriority, string> = {
  Low: 'Baixa',
  Medium: 'Média',
  High: 'Alta',
}

export const SORT_LABELS: Record<TaskSortField, string> = {
  CreatedAt: 'Data de criação',
  UpdatedAt: 'Última atualização',
  DueDate: 'Prazo',
  Priority: 'Prioridade',
  Title: 'Título',
  Status: 'Status',
}

export const STATUSES = Object.keys(STATUS_LABELS) as TaskStatus[]
export const PRIORITIES = Object.keys(PRIORITY_LABELS) as TaskPriority[]

export function transitionLabel(from: TaskStatus, to: TaskStatus): string {
  switch (to) {
    case 'InProgress':
      return from === 'Completed' ? 'Reabrir em andamento' : 'Iniciar'
    case 'Completed':
      return 'Concluir'
    case 'Cancelled':
      return 'Cancelar'
    case 'Pending':
      return from === 'InProgress' ? 'Voltar para pendente' : 'Reabrir'
  }
}

export function formatDate(isoDate: string | null): string {
  if (!isoDate) return '—'
  const [year, month, day] = isoDate.slice(0, 10).split('-')
  return `${day}/${month}/${year}`
}

export function formatDateTime(isoDateTime: string): string {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(isoDateTime))
}

export function todayIso(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset() * 60_000
  return new Date(now.getTime() - offset).toISOString().slice(0, 10)
}
