import { request } from './client'
import type {
  AuthResponse,
  PagedResult,
  Task,
  TaskInput,
  TaskQuery,
  TaskStatus,
  TaskSummary,
  User,
} from './types'

export const authApi = {
  login: (email: string, password: string) =>
    request<AuthResponse>('/api/auth/login', { method: 'POST', body: { email, password } }),
  register: (name: string, email: string, password: string) =>
    request<AuthResponse>('/api/auth/register', { method: 'POST', body: { name, email, password } }),
  me: () => request<User>('/api/auth/me'),
}

export const tasksApi = {
  list: (query: TaskQuery) => request<PagedResult<Task>>('/api/tasks', { query: { ...query } }),
  summary: () => request<TaskSummary>('/api/tasks/summary'),
  get: (id: string) => request<Task>(`/api/tasks/${id}`),
  create: (input: TaskInput) => request<Task>('/api/tasks', { method: 'POST', body: input }),
  update: (id: string, input: TaskInput) => request<Task>(`/api/tasks/${id}`, { method: 'PUT', body: input }),
  changeStatus: (id: string, status: TaskStatus) =>
    request<Task>(`/api/tasks/${id}/status`, { method: 'PATCH', body: { status } }),
  remove: (id: string) => request<void>(`/api/tasks/${id}`, { method: 'DELETE' }),
}
