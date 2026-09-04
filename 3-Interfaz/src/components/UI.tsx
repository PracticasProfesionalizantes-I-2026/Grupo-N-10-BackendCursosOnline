import { AnimatePresence, motion } from 'framer-motion'
import {
  ArrowLeft,
  Bell,
  BookOpen,
  CheckCircle2,
  Clock3,
  Inbox,
  Search,
  X,
  type LucideIcon,
} from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { CourseStatus, EnrollmentStatus, ProgressStatus } from '@/types'

export function Logo({ compact = false }: { compact?: boolean }) {
  return (
    <Link
      to="/"
      className="inline-flex min-h-11 items-center rounded-lg text-foreground transition hover:text-primary"
      aria-label="Lumen, ir al inicio"
    >
      {compact ? (
        <span className="grid size-10 place-items-center border border-primary-border bg-primary-soft text-base font-black text-primary">L</span>
      ) : (
        <span>
          <span className="block text-xl font-medium leading-none tracking-[0.36em] text-foreground">LUMEN</span>
          <span className="mt-2 block text-[9px] font-semibold uppercase tracking-[0.34em] text-muted-dark">Aprender ilumina</span>
        </span>
      )}
    </Link>
  )
}

const statusStyles: Record<string, string> = {
  BORRADOR: 'border-neutral-700 bg-neutral-800/50 text-neutral-300',
  'EN REVISIÓN': 'border-amber-500/25 bg-amber-500/10 text-amber-300',
  'CAMBIOS SOLICITADOS': 'border-amber-500/25 bg-amber-500/10 text-amber-300',
  RECHAZADO: 'border-red-500/25 bg-red-500/10 text-red-300',
  PUBLICADO: 'border-emerald-500/25 bg-emerald-500/10 text-emerald-300',
  PAUSADO: 'border-primary-border bg-primary-soft text-orange-300',
  FINALIZADO: 'border-neutral-700 bg-neutral-800/50 text-neutral-300',
  PENDIENTE: 'border-amber-500/25 bg-amber-500/10 text-amber-300',
  APROBADA: 'border-emerald-500/25 bg-emerald-500/10 text-emerald-300',
  RECHAZADA: 'border-red-500/25 bg-red-500/10 text-red-300',
  CANCELADA: 'border-neutral-700 bg-neutral-800/50 text-neutral-400',
  'NO INICIADO': 'border-neutral-700 bg-neutral-800/50 text-neutral-300',
  'EN PROGRESO': 'border-primary-border bg-primary-soft text-orange-300',
  COMPLETADO: 'border-emerald-500/25 bg-emerald-500/10 text-emerald-300',
  PENDIENTE_AUDIT: 'border-amber-500/25 bg-amber-500/10 text-amber-300',
  RESUELTA: 'border-neutral-700 bg-neutral-800/50 text-neutral-300',
}

export function StatusBadge({
  status,
}: {
  status: CourseStatus | EnrollmentStatus | ProgressStatus | 'RESUELTA' | 'PENDIENTE'
}) {
  return (
    <span
      className={
        'inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-1 text-[11px] font-bold tracking-wide ' +
        (statusStyles[status] ?? statusStyles.BORRADOR)
      }
    >
      <span className="size-1.5 rounded-full bg-current" aria-hidden="true" />
      {status}
    </span>
  )
}

export const CourseStatusBadge = ({ status }: { status: CourseStatus }) => <StatusBadge status={status} />
export const EnrollmentStatusBadge = ({ status }: { status: EnrollmentStatus }) => <StatusBadge status={status} />
export const ProgressStatusBadge = ({ status }: { status: ProgressStatus }) => <StatusBadge status={status} />

export function ProgressBar({ value, className = '' }: { value: number; className?: string }) {
  return (
    <div
      className={'h-1.5 overflow-hidden rounded-full bg-surface-hover ' + className}
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={value}
      aria-label={'Progreso: ' + value + ' %'}
    >
      <motion.div
        initial={{ scaleX: 0 }}
        animate={{ scaleX: Math.max(0, Math.min(100, value)) / 100 }}
        transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
        className="h-full origin-left rounded-full bg-gradient-to-r from-primary to-orange-400"
      />
    </div>
  )
}

export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
  backTo,
}: {
  eyebrow?: string
  title: string
  description?: string
  actions?: ReactNode
  backTo?: string
}) {
  return (
    <motion.header
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.42, ease: [0.22, 1, 0.36, 1] }}
      className="mb-9 border-b border-slate-200 pb-7"
    >
      <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
        <div>
          {backTo && (
            <Link to={backTo} className="mb-4 inline-flex items-center gap-2 text-sm font-semibold text-muted hover:text-primary">
              <ArrowLeft size={16} /> Volver
            </Link>
          )}
          {eyebrow && <p className="eyebrow mb-3">{eyebrow}</p>}
          <h1 className="max-w-4xl font-display text-4xl font-semibold leading-[1.04] tracking-[-0.045em] text-foreground sm:text-5xl">{title}</h1>
          {description && <p className="mt-3 max-w-2xl text-base leading-7 text-muted">{description}</p>}
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>
    </motion.header>
  )
}

export function MetricCard({
  label,
  value,
  hint,
  icon: Icon = BookOpen,
  tone = 'primary',
}: {
  label: string
  value: string | number
  hint?: string
  icon?: LucideIcon
  tone?: 'primary' | 'neutral' | 'emerald' | 'amber'
}) {
  const tones = {
    primary: 'text-primary',
    neutral: 'text-foreground-secondary',
    emerald: 'text-emerald-400',
    amber: 'text-amber-400',
  }
  return (
    <motion.article
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      whileHover={{ y: -2 }}
      transition={{ duration: 0.36, ease: [0.22, 1, 0.36, 1] }}
      className="relative flex min-h-28 items-center gap-4 border-l border-slate-300 pl-4 sm:pl-5"
    >
      <span className={'grid size-10 shrink-0 place-items-center bg-surface-raised ' + tones[tone]}>
        <Icon size={19} aria-hidden="true" />
      </span>
      <div>
        <p className="font-display text-3xl font-semibold tracking-[-0.04em] text-foreground">{value}</p>
        <p className="mt-1 text-sm text-muted">{label}</p>
        {hint && <p className="mt-1 text-xs leading-5 text-muted-dark">{hint}</p>}
      </div>
    </motion.article>
  )
}

export function SearchInput({
  value,
  onChange,
  placeholder = 'Buscar',
  className = '',
}: {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
}) {
  return (
    <label className={'relative block ' + className}>
      <span className="sr-only">{placeholder}</span>
      <Search className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-dark" size={18} />
      <input
        className="input pl-10"
        type="search"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
      />
    </label>
  )
}

export function EmptyState({
  title = 'No hay resultados',
  description = 'Probá cambiar la búsqueda o los filtros.',
  action,
}: {
  title?: string
  description?: string
  action?: ReactNode
}) {
  return (
    <div className="flex min-h-60 flex-col items-center justify-center border-y border-dashed border-slate-300 px-6 py-12 text-center">
      <span className="grid size-12 place-items-center border border-slate-300 bg-surface text-muted-dark">
        <Inbox size={22} />
      </span>
      <h3 className="mt-5 text-lg font-bold text-foreground">{title}</h3>
      <p className="mt-1 max-w-sm text-sm leading-6 text-muted">{description}</p>
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}

export function Modal({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  size = 'md',
}: {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  children: ReactNode
  footer?: ReactNode
  size?: 'sm' | 'md' | 'lg'
}) {
  const widths = { sm: 'max-w-md', md: 'max-w-xl', lg: 'max-w-3xl' }
  return (
    <AnimatePresence>
      {open && (
        <motion.div
          className="fixed inset-0 z-[80] grid place-items-center bg-black/75 p-4 backdrop-blur-sm"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onMouseDown={(event) => event.target === event.currentTarget && onClose()}
        >
          <motion.div
            role="dialog"
            aria-modal="true"
            aria-labelledby="modal-title"
            initial={{ opacity: 0, scale: 0.97, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.98, y: 6 }}
            transition={{ duration: 0.22 }}
            className={'max-h-[90vh] w-full overflow-hidden rounded-xl border border-slate-300 bg-surface-raised shadow-soft ' + widths[size]}
          >
            <header className="flex items-start justify-between border-b border-slate-300 px-6 py-5">
              <div>
                <h2 id="modal-title" className="text-xl font-bold text-foreground">{title}</h2>
                {description && <p className="mt-1 text-sm text-muted">{description}</p>}
              </div>
              <button className="btn-ghost -mr-2 min-h-10 px-3" onClick={onClose} aria-label="Cerrar">
                <X size={19} />
              </button>
            </header>
            <div className="thin-scrollbar max-h-[62vh] overflow-y-auto px-6 py-5">{children}</div>
            {footer && <footer className="flex flex-wrap justify-end gap-2 border-t border-slate-300 px-6 py-4">{footer}</footer>}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}

export function NotificationButton({ unread, onClick }: { unread: number; onClick: () => void }) {
  return (
    <button
      className="relative grid size-11 place-items-center rounded-lg border border-slate-300 bg-background-secondary text-muted transition hover:border-primary-border hover:bg-primary-soft hover:text-primary"
      onClick={onClick}
      aria-label="Abrir notificaciones"
    >
      <Bell size={19} />
      {unread > 0 && (
        <span className="absolute -right-1 -top-1 grid min-w-5 place-items-center rounded-full bg-primary px-1 text-[10px] font-bold leading-5 text-background">
          {unread}
        </span>
      )}
    </button>
  )
}

export function InfoPill({ children, icon: Icon = Clock3 }: { children: ReactNode; icon?: LucideIcon }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-slate-300 bg-surface-raised px-2.5 py-1 text-xs font-medium text-muted">
      <Icon size={13} aria-hidden="true" />
      {children}
    </span>
  )
}

export function SuccessLine({ children }: { children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-2 text-sm font-semibold text-emerald-400">
      <CheckCircle2 size={17} />
      {children}
    </span>
  )
}

export function LumenOrb({ className = '' }: { className?: string }) {
  return <div className={'lumen-orbit ' + className} aria-hidden="true" />
}

export function CourseArtwork({
  courseId,
  category,
  className = '',
}: {
  courseId: string
  category: string
  className?: string
}) {
  const variant = Array.from(courseId).reduce((sum, character) => sum + character.charCodeAt(0), 0) % 3
  return (
    <div className={'course-art ' + className} data-variant={variant} aria-hidden="true">
      <span className="absolute bottom-4 left-4 z-10 text-[10px] font-bold uppercase tracking-[0.22em] text-orange-300/80">{category}</span>
      <span className="absolute left-4 top-4 z-10 font-display text-3xl font-semibold tracking-[-0.04em] text-white/10">
        {String(variant + 1).padStart(2, '0')}
      </span>
    </div>
  )
}
