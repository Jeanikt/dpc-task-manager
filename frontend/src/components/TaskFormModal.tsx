import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import type { Task, TaskInput, TaskPriority } from '../api/types'
import { PRIORITIES, PRIORITY_LABELS, todayIso } from '../lib/labels'
import { DESCRIPTION_MAX, TITLE_MAX, validateTaskInput, type FieldErrors } from '../lib/taskValidation'
import { Modal } from './Modal'

interface TaskFormModalProps {
  task?: Task
  onSubmit: (input: TaskInput) => Promise<unknown>
  onClose: () => void
}

export function TaskFormModal({ task, onSubmit, onClose }: TaskFormModalProps) {
  const isNew = !task
  const [form, setForm] = useState<TaskInput>({
    title: task?.title ?? '',
    description: task?.description ?? '',
    priority: task?.priority ?? 'Medium',
    dueDate: task?.dueDate ?? '',
  })
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const set = <K extends keyof TaskInput>(key: K, value: TaskInput[K]) => {
    setForm((f) => ({ ...f, [key]: value }))
    setErrors((e) => ({ ...e, [key]: undefined }))
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const input: TaskInput = {
      title: form.title.trim(),
      description: form.description?.trim() || null,
      priority: form.priority,
      dueDate: form.dueDate || null,
    }

    const clientErrors = validateTaskInput(input, isNew)
    setErrors(clientErrors)
    setFormError(null)
    if (Object.keys(clientErrors).length > 0) return

    setSubmitting(true)
    try {
      await onSubmit(input)
    } catch (error) {
      if (error instanceof ApiError) {
        const fieldErrors: FieldErrors = {}
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          if (field in form) fieldErrors[field as keyof TaskInput] = messages[0]
        }
        setErrors(fieldErrors)
        if (Object.keys(fieldErrors).length === 0) setFormError(error.message)
      } else {
        setFormError('Não foi possível salvar a tarefa.')
      }
      setSubmitting(false)
    }
  }

  return (
    <Modal
      title={isNew ? 'Nova tarefa' : 'Editar tarefa'}
      description={isNew ? 'A tarefa será criada com status Pendente.' : undefined}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="button button--secondary" onClick={onClose}>
            Cancelar
          </button>
          <button type="submit" form="task-form" className="button button--primary" disabled={submitting}>
            {submitting ? 'Salvando…' : isNew ? 'Criar tarefa' : 'Salvar alterações'}
          </button>
        </>
      }
    >
      <form id="task-form" className="form" onSubmit={handleSubmit} noValidate>
        {formError && (
          <div className="alert alert--error" role="alert">
            {formError}
          </div>
        )}

        <div className="field">
          <label htmlFor="task-title" className="field__label">
            Título <span className="field__required" aria-hidden>*</span>
          </label>
          <input
            id="task-title"
            className="input"
            value={form.title}
            maxLength={TITLE_MAX}
            onChange={(e) => set('title', e.target.value)}
            aria-invalid={!!errors.title}
            aria-describedby={errors.title ? 'task-title-error' : undefined}
            placeholder="Ex.: Conciliação bancária de outubro"
          />
          {errors.title && <p id="task-title-error" className="field__error">{errors.title}</p>}
        </div>

        <div className="field">
          <label htmlFor="task-description" className="field__label">Descrição</label>
          <textarea
            id="task-description"
            className="input input--textarea"
            rows={4}
            value={form.description ?? ''}
            maxLength={DESCRIPTION_MAX}
            onChange={(e) => set('description', e.target.value)}
            aria-invalid={!!errors.description}
            aria-describedby={errors.description ? 'task-description-error' : undefined}
            placeholder="Detalhes, contexto ou checklist da tarefa"
          />
          {errors.description && <p id="task-description-error" className="field__error">{errors.description}</p>}
        </div>

        <div className="form__row">
          <div className="field">
            <label htmlFor="task-priority" className="field__label">Prioridade</label>
            <select
              id="task-priority"
              className="input"
              value={form.priority}
              onChange={(e) => set('priority', e.target.value as TaskPriority)}
            >
              {PRIORITIES.map((p) => (
                <option key={p} value={p}>{PRIORITY_LABELS[p]}</option>
              ))}
            </select>
          </div>

          <div className="field">
            <label htmlFor="task-due" className="field__label">Prazo</label>
            <input
              id="task-due"
              type="date"
              className="input"
              value={form.dueDate ?? ''}
              min={isNew ? todayIso() : undefined}
              onChange={(e) => set('dueDate', e.target.value)}
              aria-invalid={!!errors.dueDate}
              aria-describedby={errors.dueDate ? 'task-due-error' : undefined}
            />
            {errors.dueDate && <p id="task-due-error" className="field__error">{errors.dueDate}</p>}
          </div>
        </div>
      </form>
    </Modal>
  )
}
