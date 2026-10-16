import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'

type Mode = 'login' | 'register'

export function LoginPage() {
  const { user, login, register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [mode, setMode] = useState<Mode>('login')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  const redirectTo = (location.state as { from?: string } | null)?.from ?? '/'
  if (user) return <Navigate to={redirectTo} replace />

  const isRegister = mode === 'register'

  const switchMode = (next: Mode) => {
    setMode(next)
    setError(null)
    setFieldErrors({})
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    setFieldErrors({})
    setSubmitting(true)
    try {
      if (isRegister) await register(name, email, password)
      else await login(email, password)
      navigate(redirectTo, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setFieldErrors(err.fieldErrors)
        if (Object.keys(err.fieldErrors).length === 0) setError(err.message)
      } else {
        setError('Não foi possível entrar. Tente novamente.')
      }
      setSubmitting(false)
    }
  }

  const fieldError = (field: string) =>
    fieldErrors[field]?.[0] && (
      <p id={`${field}-error`} className="field__error">{fieldErrors[field][0]}</p>
    )

  return (
    <div className="auth">
      <aside className="auth__aside" aria-hidden>
        <div className="auth__aside-inner">
          <img src="/favicon.svg" alt="" width={40} height={40} />
          <p className="auth__tagline">Organize demandas, acompanhe prazos e saiba o que está atrasado antes que vire problema.</p>
          <ul className="auth__points">
            <li>Status com regras de transição</li>
            <li>Pesquisa, filtros e indicadores</li>
            <li>Acesso por usuário com JWT</li>
          </ul>
        </div>
      </aside>

      <main className="auth__main">
        <div className="auth__card">
          <h1 className="auth__title">{isRegister ? 'Criar conta' : 'Entrar'}</h1>
          <p className="auth__subtitle">
            {isRegister ? 'Cadastre-se para começar a gerenciar suas tarefas.' : 'Acesse o Gestor de Tarefas.'}
          </p>

          <form className="form" onSubmit={handleSubmit} noValidate>
            {error && (
              <div className="alert alert--error" role="alert">
                {error}
              </div>
            )}

            {isRegister && (
              <div className="field">
                <label htmlFor="name" className="field__label">Nome</label>
                <input
                  id="name"
                  className="input"
                  autoComplete="name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  aria-invalid={!!fieldErrors.name}
                  aria-describedby={fieldErrors.name ? 'name-error' : undefined}
                />
                {fieldError('name')}
              </div>
            )}

            <div className="field">
              <label htmlFor="email" className="field__label">E-mail</label>
              <input
                id="email"
                type="email"
                className="input"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                aria-invalid={!!fieldErrors.email}
                aria-describedby={fieldErrors.email ? 'email-error' : undefined}
              />
              {fieldError('email')}
            </div>

            <div className="field">
              <label htmlFor="password" className="field__label">Senha</label>
              <input
                id="password"
                type="password"
                className="input"
                autoComplete={isRegister ? 'new-password' : 'current-password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                aria-invalid={!!fieldErrors.password}
                aria-describedby={fieldErrors.password ? 'password-error' : isRegister ? 'password-hint' : undefined}
              />
              {fieldError('password') ||
                (isRegister && (
                  <p id="password-hint" className="field__hint">Mínimo de 8 caracteres, com letras e números.</p>
                ))}
            </div>

            <button type="submit" className="button button--primary button--block" disabled={submitting}>
              {submitting ? 'Aguarde…' : isRegister ? 'Criar conta' : 'Entrar'}
            </button>
          </form>

          <p className="auth__switch">
            {isRegister ? 'Já tem conta?' : 'Ainda não tem conta?'}{' '}
            <button type="button" className="link-button" onClick={() => switchMode(isRegister ? 'login' : 'register')}>
              {isRegister ? 'Entrar' : 'Criar conta'}
            </button>
          </p>

          {import.meta.env.DEV && !isRegister && (
            <p className="auth__demo">
              Ambiente de desenvolvimento: as contas de demonstração estão no README.
            </p>
          )}
        </div>
      </main>
    </div>
  )
}
