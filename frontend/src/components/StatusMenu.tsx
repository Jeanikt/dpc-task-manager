import { useEffect, useRef, useState } from 'react'
import { ChevronDown } from 'lucide-react'
import type { Task, TaskStatus } from '../api/types'
import { transitionLabel } from '../lib/labels'

interface StatusMenuProps {
  task: Task
  disabled?: boolean
  onChange: (status: TaskStatus) => void
}

export function StatusMenu({ task, disabled, onChange }: StatusMenuProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: PointerEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && setOpen(false)
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  return (
    <div className="menu" ref={rootRef}>
      <button
        type="button"
        className="button button--secondary button--sm"
        aria-haspopup="menu"
        aria-expanded={open}
        disabled={disabled || task.allowedTransitions.length === 0}
        onClick={() => setOpen((v) => !v)}
      >
        Status
        <ChevronDown size={14} aria-hidden />
      </button>
      {open && (
        <ul className="menu__list" role="menu" aria-label={`Alterar status de ${task.title}`}>
          {task.allowedTransitions.map((status) => (
            <li key={status} role="none">
              <button
                type="button"
                role="menuitem"
                className={`menu__item menu__item--${status}`}
                onClick={() => {
                  setOpen(false)
                  onChange(status)
                }}
              >
                {transitionLabel(task.status, status)}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
