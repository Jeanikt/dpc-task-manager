import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { setAuthToken, setUnauthorizedHandler } from '../api/client'
import { authApi } from '../api/endpoints'
import type { AuthResponse, User } from '../api/types'

const STORAGE_KEY = 'task-manager.session'

interface Session {
  token: string
  expiresAt: string
  user: User
}

export interface AuthContextValue {
  user: User | null
  isAdmin: boolean
  login: (email: string, password: string) => Promise<void>
  register: (name: string, email: string, password: string) => Promise<void>
  logout: () => void
}

// eslint-disable-next-line react-refresh/only-export-components
export const AuthContext = createContext<AuthContextValue | null>(null)

function loadSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const session = JSON.parse(raw) as Session
    if (new Date(session.expiresAt) <= new Date()) return null
    return session
  } catch {
    return null
  }
}

function saveSession(session: Session | null) {
  try {
    if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    return
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<Session | null>(() => {
    const initial = loadSession()
    setAuthToken(initial?.token ?? null)
    return initial
  })

  const applySession = useCallback(
    (next: Session | null) => {
      setAuthToken(next?.token ?? null)
      saveSession(next)
      setSession(next)
      if (!next) queryClient.clear()
    },
    [queryClient],
  )

  const fromResponse = useCallback(
    (response: AuthResponse) =>
      applySession({ token: response.accessToken, expiresAt: response.expiresAt, user: response.user }),
    [applySession],
  )

  const logout = useCallback(() => applySession(null), [applySession])

  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  useEffect(() => {
    if (!session) return
    const ms = new Date(session.expiresAt).getTime() - Date.now()
    const timer = window.setTimeout(logout, Math.max(ms, 0))
    return () => window.clearTimeout(timer)
  }, [session, logout])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      isAdmin: session?.user.role === 'Admin',
      login: async (email, password) => fromResponse(await authApi.login(email, password)),
      register: async (name, email, password) => fromResponse(await authApi.register(name, email, password)),
      logout,
    }),
    [session, fromResponse, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
