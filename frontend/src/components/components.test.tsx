import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ApiError } from '../api/client'
import type { Task } from '../api/types'
import { StatusMenu } from './StatusMenu'
import { TaskFormModal } from './TaskFormModal'

const task: Task = {
  id: '1',
  title: 'Conciliação bancária',
  description: null,
  status: 'Pending',
  priority: 'High',
  dueDate: null,
  isOverdue: false,
  createdAt: '2026-10-16T12:00:00Z',
  updatedAt: '2026-10-16T12:00:00Z',
  completedAt: null,
  ownerId: 'u1',
  ownerName: 'Usuário',
  allowedTransitions: ['InProgress', 'Completed', 'Cancelled'],
}

describe('StatusMenu', () => {
  it('lista só as transições permitidas e devolve o status escolhido', async () => {
    const onChange = vi.fn()
    render(<StatusMenu task={task} onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: /status/i }))
    const items = screen.getAllByRole('menuitem').map((el) => el.textContent)
    expect(items).toEqual(['Iniciar', 'Concluir', 'Cancelar'])

    await userEvent.click(screen.getByRole('menuitem', { name: 'Concluir' }))
    expect(onChange).toHaveBeenCalledWith('Completed')
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })
})

describe('TaskFormModal', () => {
  it('não envia o formulário sem título', async () => {
    const onSubmit = vi.fn()
    render(<TaskFormModal onSubmit={onSubmit} onClose={() => {}} />)

    await userEvent.click(screen.getByRole('button', { name: 'Criar tarefa' }))

    expect(screen.getByText('O título é obrigatório.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('envia os dados normalizados', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<TaskFormModal onSubmit={onSubmit} onClose={() => {}} />)

    await userEvent.type(screen.getByLabelText(/título/i), '  Enviar DCTFWeb  ')
    await userEvent.selectOptions(screen.getByLabelText('Prioridade'), 'High')
    await userEvent.click(screen.getByRole('button', { name: 'Criar tarefa' }))

    expect(onSubmit).toHaveBeenCalledWith({ title: 'Enviar DCTFWeb', description: null, priority: 'High', dueDate: null })
  })

  it('mostra no campo o erro de validação devolvido pela API', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new ApiError(400, 'Dados inválidos', { title: ['Título já usado.'] }))
    render(<TaskFormModal onSubmit={onSubmit} onClose={() => {}} />)

    await userEvent.type(screen.getByLabelText(/título/i), 'Tarefa')
    await userEvent.click(screen.getByRole('button', { name: 'Criar tarefa' }))

    expect(await screen.findByText('Título já usado.')).toBeInTheDocument()
    expect(screen.getByLabelText(/título/i)).toHaveAttribute('aria-invalid', 'true')
  })

  it('preenche os campos ao editar', () => {
    render(<TaskFormModal task={{ ...task, description: 'Banco do Brasil' }} onSubmit={vi.fn()} onClose={() => {}} />)

    expect(screen.getByRole('heading', { name: 'Editar tarefa' })).toBeInTheDocument()
    expect(screen.getByLabelText(/título/i)).toHaveValue('Conciliação bancária')
    expect(screen.getByLabelText('Descrição')).toHaveValue('Banco do Brasil')
  })
})
