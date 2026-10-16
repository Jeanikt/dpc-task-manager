import type { TaskInput } from '../api/types'
import { todayIso } from './labels'

export type FieldErrors = Partial<Record<keyof TaskInput, string>>

export const TITLE_MAX = 150
export const DESCRIPTION_MAX = 2000

export function validateTaskInput(input: TaskInput, isNew: boolean): FieldErrors {
  const errors: FieldErrors = {}
  if (!input.title.trim()) errors.title = 'O título é obrigatório.'
  else if (input.title.trim().length > TITLE_MAX) errors.title = `O título deve ter no máximo ${TITLE_MAX} caracteres.`
  if ((input.description?.length ?? 0) > DESCRIPTION_MAX)
    errors.description = `A descrição deve ter no máximo ${DESCRIPTION_MAX} caracteres.`
  if (isNew && input.dueDate && input.dueDate < todayIso()) errors.dueDate = 'O prazo não pode estar no passado.'
  return errors
}
