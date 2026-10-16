export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled'
export type TaskPriority = 'Low' | 'Medium' | 'High'
export type UserRole = 'User' | 'Admin'
export type TaskSortField = 'CreatedAt' | 'UpdatedAt' | 'DueDate' | 'Priority' | 'Title' | 'Status'
export type SortDirection = 'Asc' | 'Desc'

export interface Task {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  priority: TaskPriority
  dueDate: string | null
  isOverdue: boolean
  createdAt: string
  updatedAt: string
  completedAt: string | null
  ownerId: string
  ownerName: string | null
  allowedTransitions: TaskStatus[]
}

export interface TaskInput {
  title: string
  description: string | null
  priority: TaskPriority
  dueDate: string | null
}

export interface TaskQuery {
  search?: string
  status?: TaskStatus
  priority?: TaskPriority
  overdue?: boolean
  sortBy?: TaskSortField
  sortDirection?: SortDirection
  page?: number
  pageSize?: number
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface TaskSummary {
  total: number
  pending: number
  inProgress: number
  completed: number
  cancelled: number
  overdue: number
}

export interface User {
  id: string
  name: string
  email: string
  role: UserRole
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: User
}
