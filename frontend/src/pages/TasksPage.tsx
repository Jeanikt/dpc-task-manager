import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ClipboardList, Plus, RefreshCw } from 'lucide-react'
import { ApiError } from '../api/client'
import { tasksApi } from '../api/endpoints'
import type { SortDirection, Task, TaskInput, TaskPriority, TaskQuery, TaskSortField, TaskStatus } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Pagination } from '../components/Pagination'
import { SummaryCards } from '../components/SummaryCards'
import { TaskFilters, type FilterValues } from '../components/TaskFilters'
import { TaskFormModal } from '../components/TaskFormModal'
import { TaskTable } from '../components/TaskTable'
import { useToast } from '../components/Toast'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { STATUS_LABELS } from '../lib/labels'

const DEFAULTS = { sortBy: 'CreatedAt' as TaskSortField, sortDirection: 'Desc' as SortDirection, pageSize: 10 }

function useTaskSearchParams() {
  const [params, setParams] = useSearchParams()

  const filters: FilterValues = {
    search: params.get('q') ?? '',
    status: (params.get('status') as TaskStatus | null) ?? undefined,
    priority: (params.get('priority') as TaskPriority | null) ?? undefined,
    overdue: params.get('overdue') === 'true',
    sortBy: (params.get('sort') as TaskSortField | null) ?? DEFAULTS.sortBy,
    sortDirection: (params.get('dir') as SortDirection | null) ?? DEFAULTS.sortDirection,
  }
  const page = Math.max(Number(params.get('page')) || 1, 1)
  const pageSize = Number(params.get('size')) || DEFAULTS.pageSize

  const update = useCallback(
    (patch: Partial<FilterValues> & { page?: number; pageSize?: number }) => {
      setParams(
        (current) => {
          const next = new URLSearchParams(current)
          const set = (key: string, value: string | undefined, fallback?: string) => {
            if (!value || value === fallback) next.delete(key)
            else next.set(key, value)
          }
          if ('search' in patch) set('q', patch.search)
          if ('status' in patch) set('status', patch.status)
          if ('priority' in patch) set('priority', patch.priority)
          if ('overdue' in patch) set('overdue', patch.overdue ? 'true' : undefined)
          if ('sortBy' in patch) set('sort', patch.sortBy, DEFAULTS.sortBy)
          if ('sortDirection' in patch) set('dir', patch.sortDirection, DEFAULTS.sortDirection)
          if ('pageSize' in patch) set('size', String(patch.pageSize), String(DEFAULTS.pageSize))
          set('page', patch.page && patch.page > 1 ? String(patch.page) : undefined)
          return next
        },
        { replace: true },
      )
    },
    [setParams],
  )

  return { filters, page, pageSize, update }
}

type Dialog = { kind: 'create' } | { kind: 'edit'; task: Task } | { kind: 'delete'; task: Task } | null

export function TasksPage() {
  const { isAdmin } = useAuth()
  const toast = useToast()
  const queryClient = useQueryClient()
  const { filters, page, pageSize, update } = useTaskSearchParams()
  const [dialog, setDialog] = useState<Dialog>(null)
  const [searchInput, setSearchInput] = useState(filters.search)
  const debouncedSearch = useDebouncedValue(searchInput)

  useEffect(() => {
    if (debouncedSearch !== filters.search) update({ search: debouncedSearch })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch])

  const query: TaskQuery = useMemo(
    () => ({
      search: filters.search || undefined,
      status: filters.status,
      priority: filters.priority,
      overdue: filters.overdue || undefined,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      page,
      pageSize,
    }),
    [filters.search, filters.status, filters.priority, filters.overdue, filters.sortBy, filters.sortDirection, page, pageSize],
  )

  const tasksQuery = useQuery({
    queryKey: ['tasks', query],
    queryFn: () => tasksApi.list(query),
    placeholderData: keepPreviousData,
  })
  const summaryQuery = useQuery({ queryKey: ['tasks-summary'], queryFn: tasksApi.summary })

  const data = tasksQuery.data
  useEffect(() => {
    if (data && data.items.length === 0 && data.totalPages > 0 && page > data.totalPages) update({ page: data.totalPages })
  }, [data, page, update])

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['tasks'] })
    void queryClient.invalidateQueries({ queryKey: ['tasks-summary'] })
  }

  const onError = (error: unknown) => toast.error(error instanceof ApiError ? error.message : 'Não foi possível concluir a operação.')

  const createMutation = useMutation({
    mutationFn: (input: TaskInput) => tasksApi.create(input),
    onSuccess: () => {
      toast.success('Tarefa criada.')
      setDialog(null)
      refresh()
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, input }: { id: string; input: TaskInput }) => tasksApi.update(id, input),
    onSuccess: () => {
      toast.success('Tarefa atualizada.')
      setDialog(null)
      refresh()
    },
  })

  const statusMutation = useMutation({
    mutationFn: ({ task, status }: { task: Task; status: TaskStatus }) => tasksApi.changeStatus(task.id, status),
    onSuccess: (task) => {
      toast.success(`Status alterado para "${STATUS_LABELS[task.status]}".`)
      refresh()
    },
    onError,
  })

  const deleteMutation = useMutation({
    mutationFn: (task: Task) => tasksApi.remove(task.id),
    onSuccess: () => {
      toast.success('Tarefa excluída.')
      setDialog(null)
      refresh()
    },
    onError: (error) => {
      setDialog(null)
      onError(error)
    },
  })

  const busyTaskId = statusMutation.isPending ? (statusMutation.variables?.task.id ?? null) : null
  const tasks = data?.items ?? []
  const hasFilters = !!(filters.search || filters.status || filters.priority || filters.overdue)

  return (
    <main className="page">
      <div className="page__header">
        <div>
          <h1 className="page__title">Tarefas</h1>
          <p className="page__subtitle">
            {isAdmin ? 'Visão de administrador: tarefas de todos os usuários.' : 'Acompanhe e atualize as suas tarefas.'}
          </p>
        </div>
        <div className="page__actions">
          <button type="button" className="icon-button" onClick={refresh} aria-label="Atualizar lista" title="Atualizar">
            <RefreshCw size={16} className={tasksQuery.isFetching ? 'spin' : undefined} />
          </button>
          <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'create' })}>
            <Plus size={16} aria-hidden /> Nova tarefa
          </button>
        </div>
      </div>

      <SummaryCards
        summary={summaryQuery.data}
        active={{ status: filters.status, overdue: filters.overdue }}
        onSelect={(f) => update({ status: f.status, overdue: !!f.overdue })}
      />

      <section className="panel" aria-label="Lista de tarefas">
        <TaskFilters
          values={{ ...filters, search: searchInput }}
          onChange={(patch) => {
            if ('search' in patch) setSearchInput(patch.search ?? '')
            else update(patch)
          }}
          onClear={() => {
            setSearchInput('')
            update({ search: '', status: undefined, priority: undefined, overdue: false })
          }}
        />

        {tasksQuery.isError ? (
          <div className="empty">
            <p className="empty__title">Não foi possível carregar as tarefas.</p>
            <p className="empty__text">
              {tasksQuery.error instanceof ApiError ? tasksQuery.error.message : 'Tente novamente em instantes.'}
            </p>
            <button type="button" className="button button--secondary" onClick={() => void tasksQuery.refetch()}>
              Tentar novamente
            </button>
          </div>
        ) : tasksQuery.isPending ? (
          <div className="skeleton-list" aria-label="Carregando tarefas">
            {Array.from({ length: 5 }, (_, i) => (
              <div key={i} className="skeleton-row" />
            ))}
          </div>
        ) : tasks.length === 0 ? (
          <div className="empty">
            <ClipboardList size={36} className="empty__icon" aria-hidden />
            <p className="empty__title">{hasFilters ? 'Nenhuma tarefa encontrada' : 'Nenhuma tarefa cadastrada'}</p>
            <p className="empty__text">
              {hasFilters ? 'Ajuste a pesquisa ou limpe os filtros.' : 'Crie a primeira tarefa para começar a organizar o trabalho.'}
            </p>
            {!hasFilters && (
              <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'create' })}>
                <Plus size={16} aria-hidden /> Nova tarefa
              </button>
            )}
          </div>
        ) : (
          <>
            <TaskTable
              tasks={tasks}
              showOwner={isAdmin}
              busyTaskId={busyTaskId}
              onEdit={(task) => setDialog({ kind: 'edit', task })}
              onDelete={(task) => setDialog({ kind: 'delete', task })}
              onChangeStatus={(task, status) => statusMutation.mutate({ task, status })}
            />
            <Pagination
              page={data!.page}
              pageSize={data!.pageSize}
              totalCount={data!.totalCount}
              totalPages={data!.totalPages}
              onPageChange={(p) => update({ page: p })}
              onPageSizeChange={(size) => update({ pageSize: size })}
            />
          </>
        )}
      </section>

      {dialog?.kind === 'create' && (
        <TaskFormModal onClose={() => setDialog(null)} onSubmit={(input) => createMutation.mutateAsync(input)} />
      )}
      {dialog?.kind === 'edit' && (
        <TaskFormModal
          task={dialog.task}
          onClose={() => setDialog(null)}
          onSubmit={(input) => updateMutation.mutateAsync({ id: dialog.task.id, input })}
        />
      )}
      {dialog?.kind === 'delete' && (
        <ConfirmDialog
          title="Excluir tarefa"
          message={`A tarefa "${dialog.task.title}" será excluída permanentemente. Deseja continuar?`}
          confirmLabel="Excluir"
          busy={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(dialog.task)}
          onCancel={() => setDialog(null)}
        />
      )}
    </main>
  )
}
