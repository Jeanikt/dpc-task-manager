import { describe, expect, it } from 'vitest'
import { buildQueryString } from '../api/client'
import { formatDate, transitionLabel } from './labels'
import { validateTaskInput } from './taskValidation'

describe('formatDate', () => {
  it('formata yyyy-MM-dd como dd/MM/yyyy sem deslocar o dia por fuso', () => {
    expect(formatDate('2026-10-01')).toBe('01/10/2026')
  })

  it('mostra travessão quando não há data', () => {
    expect(formatDate(null)).toBe('—')
  })
})

describe('transitionLabel', () => {
  it('usa verbos de ação conforme a origem', () => {
    expect(transitionLabel('Pending', 'InProgress')).toBe('Iniciar')
    expect(transitionLabel('Completed', 'Pending')).toBe('Reabrir')
    expect(transitionLabel('InProgress', 'Pending')).toBe('Voltar para pendente')
  })
})

describe('buildQueryString', () => {
  it('ignora valores vazios', () => {
    expect(buildQueryString({ search: '', status: 'Pending', page: 2, overdue: undefined })).toBe('?status=Pending&page=2')
  })

  it('retorna string vazia sem parâmetros', () => {
    expect(buildQueryString({})).toBe('')
  })
})

describe('validateTaskInput', () => {
  const base = { title: 'Tarefa', description: null, priority: 'Medium' as const, dueDate: null }

  it('exige título', () => {
    expect(validateTaskInput({ ...base, title: '   ' }, true).title).toBeDefined()
  })

  it('rejeita prazo no passado só na criação', () => {
    const input = { ...base, dueDate: '2000-01-01' }
    expect(validateTaskInput(input, true).dueDate).toBeDefined()
    expect(validateTaskInput(input, false).dueDate).toBeUndefined()
  })

  it('aceita dados válidos', () => {
    expect(validateTaskInput(base, true)).toEqual({})
  })
})
