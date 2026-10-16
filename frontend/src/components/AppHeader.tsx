import { LogOut, ShieldCheck } from 'lucide-react'
import { useAuth } from '../auth/useAuth'

export function AppHeader() {
  const { user, isAdmin, logout } = useAuth()

  return (
    <header className="topbar">
      <div className="topbar__inner">
        <div className="brand">
          <img src="/favicon.svg" alt="" width={28} height={28} />
          <span className="brand__name">Gestor de Tarefas</span>
        </div>
        {user && (
          <div className="topbar__user">
            <div className="topbar__identity">
              <span className="topbar__name">{user.name}</span>
              <span className="topbar__email">{user.email}</span>
            </div>
            {isAdmin && (
              <span className="role-tag" title="Administradores veem as tarefas de todos os usuários">
                <ShieldCheck size={14} aria-hidden /> Admin
              </span>
            )}
            <button type="button" className="button button--ghost button--sm" onClick={logout}>
              <LogOut size={16} aria-hidden />
              Sair
            </button>
          </div>
        )}
      </div>
    </header>
  )
}
