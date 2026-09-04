import { AnimatePresence, motion } from 'framer-motion'
import {
  BarChart3,
  Bell,
  BookMarked,
  BookOpen,
  ChevronDown,
  ClipboardCheck,
  Compass,
  FilePlus2,
  GraduationCap,
  Home,
  LogOut,
  Menu,
  RefreshCcw,
  Search,
  Users,
  X,
} from 'lucide-react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { toast, Toaster } from 'sonner'
import type { Role } from '@/types'
import { useApp } from '@/hooks/useApp'
import { formatShortDate, fullName, initials } from '@/utils/format'
import { Logo, NotificationButton } from '@/components/UI'

const menus = {
  ALUMNO: [
    { label: 'Inicio', to: '/student', icon: Home, end: true },
    { label: 'Explorar cursos', to: '/student/explore', icon: Compass },
    { label: 'Mis cursos', to: '/student/my-courses', icon: BookOpen },
    { label: 'Mis solicitudes', to: '/student/requests', icon: ClipboardCheck },
    { label: 'Mi progreso', to: '/student/progress', icon: BarChart3 },
  ],
  PROFESOR: [
    { label: 'Inicio', to: '/teacher', icon: Home, end: true },
    { label: 'Mis cursos', to: '/teacher/courses', icon: BookMarked },
    { label: 'Crear curso', to: '/teacher/create', icon: FilePlus2 },
    { label: 'Alumnos', to: '/teacher/students', icon: Users },
  ],
  ADMINISTRADOR: [
    { label: 'Inicio', to: '/admin', icon: Home, end: true },
    { label: 'Auditorías', to: '/admin/audits', icon: ClipboardCheck },
    { label: 'Inscripciones', to: '/admin/enrollments', icon: GraduationCap },
    { label: 'Usuarios', to: '/admin/users', icon: Users },
    { label: 'Cursos', to: '/admin/courses', icon: BookOpen },
    { label: 'Reportes', to: '/admin/reports', icon: BarChart3 },
  ],
}

const labels: Record<Role, string> = {
  ALUMNO: 'Alumno',
  PROFESOR: 'Profesor',
  ADMINISTRADOR: 'Administrador',
}

const roleMarks: Record<Role, string> = {
  ALUMNO: 'AL',
  PROFESOR: 'PR',
  ADMINISTRADOR: 'AD',
}

export function AppShell() {
  const { state, currentUser, logout, loginAs, resetDemoData, markNotificationsRead } = useApp()
  const [mobileOpen, setMobileOpen] = useState(false)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const [query, setQuery] = useState('')
  const location = useLocation()
  const navigate = useNavigate()
  const profileRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    setMobileOpen(false)
    setProfileOpen(false)
    setNotificationsOpen(false)
  }, [location.pathname])

  if (!currentUser) return null

  const notifications = state.notifications.filter((item) => item.userId === currentUser.id)
  const unread = notifications.filter((item) => !item.read).length

  const switchRole = (role: Role) => {
    loginAs(role)
    navigate(role === 'ALUMNO' ? '/student' : role === 'PROFESOR' ? '/teacher' : '/admin')
    toast.success('Vista cambiada a ' + labels[role])
  }

  const submitSearch = (event: FormEvent) => {
    event.preventDefault()
    const base =
      currentUser.role === 'ALUMNO'
        ? '/student/explore'
        : currentUser.role === 'PROFESOR'
          ? '/teacher/courses'
          : '/admin/courses'
    navigate(base + (query.trim() ? '?q=' + encodeURIComponent(query.trim()) : ''))
  }

  const sidebar = (
    <div className="flex h-full flex-col bg-background-secondary">
      <div className="flex h-24 items-center border-b border-slate-200 px-6">
        <Logo />
        <button className="btn-ghost ml-auto min-h-10 px-3 lg:hidden" onClick={() => setMobileOpen(false)} aria-label="Cerrar navegación">
          <X size={20} />
        </button>
      </div>

      <nav className="thin-scrollbar flex-1 overflow-y-auto px-3 py-7" aria-label="Navegación principal">
        <p className="mb-4 px-4 text-[10px] font-bold uppercase tracking-[0.22em] text-muted-dark">Navegación</p>
        <div className="space-y-1">
          {menus[currentUser.role].map((item) => {
            const Icon = item.icon
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  'relative flex min-h-12 items-center gap-3 overflow-hidden rounded-lg px-4 text-sm font-medium transition-colors ' +
                  (isActive
                    ? 'bg-primary-soft text-foreground'
                    : 'text-muted hover:bg-surface-hover hover:text-foreground')
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && (
                      <motion.span
                        layoutId="active-navigation"
                        className="absolute inset-y-2 left-0 w-0.5 rounded-full bg-primary"
                        transition={{ type: 'spring', stiffness: 420, damping: 34 }}
                      />
                    )}
                    <Icon size={18} strokeWidth={1.8} className={isActive ? 'text-primary' : 'text-muted-dark'} />
                    <span>{item.label}</span>
                  </>
                )}
              </NavLink>
            )
          })}
        </div>
      </nav>

      <div className="border-t border-slate-200 p-4">
        <div className="border border-slate-200 bg-surface/60 p-3">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-muted-dark">Vista demo</p>
              <p className="mt-1 text-sm font-semibold text-foreground">{labels[currentUser.role]}</p>
            </div>
            <span className="size-2 rounded-full bg-primary shadow-glow" aria-hidden="true" />
          </div>
          <div className="mt-3 grid grid-cols-3 gap-1" aria-label="Cambiar rol demo">
            {(['ALUMNO', 'PROFESOR', 'ADMINISTRADOR'] as Role[]).map((role) => (
              <button
                key={role}
                onClick={() => switchRole(role)}
                className={
                  'min-h-8 border px-2 text-[10px] font-extrabold tracking-wider transition ' +
                  (currentUser.role === role
                    ? 'border-primary-border bg-primary-soft text-primary'
                    : 'border-slate-200 bg-background-secondary text-muted-dark hover:border-slate-300 hover:text-foreground')
                }
                title={'Entrar como ' + labels[role]}
                aria-label={'Entrar como ' + labels[role]}
              >
                {roleMarks[role]}
              </button>
            ))}
          </div>
        </div>
        <button
          className="mt-2 flex min-h-10 w-full items-center gap-2 rounded-lg px-3 text-left text-xs font-medium text-muted-dark transition hover:bg-surface-hover hover:text-primary"
          onClick={() => {
            resetDemoData()
            navigate('/login')
            toast.success('La demostración fue restablecida')
          }}
        >
          <RefreshCcw size={14} /> Restablecer demostración
        </button>
      </div>
    </div>
  )

  return (
    <div className="min-h-screen bg-background text-foreground">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-64 border-r border-slate-200 lg:block">{sidebar}</aside>

      <AnimatePresence>
        {mobileOpen && (
          <>
            <motion.button
              aria-label="Cerrar navegación"
              className="fixed inset-0 z-40 bg-black/80 backdrop-blur-sm lg:hidden"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setMobileOpen(false)}
            />
            <motion.aside
              initial={{ x: -280 }}
              animate={{ x: 0 }}
              exit={{ x: -280 }}
              transition={{ duration: 0.25, ease: [0.22, 1, 0.36, 1] }}
              className="fixed inset-y-0 left-0 z-50 w-72 border-r border-slate-300 lg:hidden"
            >
              {sidebar}
            </motion.aside>
          </>
        )}
      </AnimatePresence>

      <div className="lg:pl-64">
        <header className="sticky top-0 z-30 flex h-20 items-center gap-3 border-b border-slate-200 bg-background/85 px-4 backdrop-blur-xl sm:px-6 lg:px-8">
          <button className="btn-secondary min-h-11 px-3 lg:hidden" onClick={() => setMobileOpen(true)} aria-label="Abrir navegación">
            <Menu size={20} />
          </button>

          <form onSubmit={submitSearch} className="relative hidden max-w-xl flex-1 sm:block">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-dark" size={18} />
            <input
              className="input bg-background-secondary pl-10"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Buscar cursos, personas o estados…"
              aria-label="Búsqueda global"
            />
            <span className="pointer-events-none absolute right-3 top-1/2 hidden -translate-y-1/2 border border-slate-300 px-2 py-1 text-[10px] text-muted-dark md:block">⌘ K</span>
          </form>

          <div className="ml-auto flex items-center gap-2">
            <div className="relative">
              <NotificationButton
                unread={unread}
                onClick={() => {
                  setNotificationsOpen((value) => !value)
                  setProfileOpen(false)
                  if (unread) markNotificationsRead()
                }}
              />
              <AnimatePresence>
                {notificationsOpen && (
                  <motion.div
                    initial={{ opacity: 0, y: 8, scale: 0.98 }}
                    animate={{ opacity: 1, y: 0, scale: 1 }}
                    exit={{ opacity: 0, y: 6 }}
                    transition={{ duration: 0.18 }}
                    className="absolute right-0 top-14 w-[min(23rem,calc(100vw-2rem))] rounded-xl border border-slate-300 bg-surface-raised p-2 shadow-soft"
                  >
                    <div className="flex items-center justify-between border-b border-slate-200 px-3 py-3">
                      <p className="font-bold text-foreground">Notificaciones</p>
                      <Bell size={17} className="text-primary" />
                    </div>
                    {notifications.length === 0 ? (
                      <p className="px-3 py-6 text-center text-sm text-muted">No tenés notificaciones.</p>
                    ) : (
                      notifications.slice(0, 5).map((item) => (
                        <div key={item.id} className="border-b border-slate-200 px-3 py-3 last:border-0 hover:bg-surface-hover">
                          <p className="text-sm font-semibold text-foreground">{item.title}</p>
                          <p className="mt-0.5 text-xs leading-5 text-muted">{item.body}</p>
                          <p className="mt-1 text-[11px] text-muted-dark">{formatShortDate(item.createdAt)}</p>
                        </div>
                      ))
                    )}
                  </motion.div>
                )}
              </AnimatePresence>
            </div>

            <div className="relative" ref={profileRef}>
              <button
                onClick={() => {
                  setProfileOpen((value) => !value)
                  setNotificationsOpen(false)
                }}
                className="flex min-h-11 items-center gap-2 rounded-lg border border-slate-300 bg-background-secondary px-2.5 transition hover:border-primary-border hover:bg-surface"
                aria-label="Abrir menú de usuario"
              >
                <span className="grid size-8 place-items-center rounded-full border border-primary-border bg-primary-soft text-xs font-bold text-primary">{initials(currentUser)}</span>
                <span className="hidden text-left md:block">
                  <span className="block max-w-28 truncate text-sm font-bold leading-4 text-foreground">{currentUser.firstName}</span>
                  <span className="block text-[10px] font-semibold text-muted-dark">{labels[currentUser.role]}</span>
                </span>
                <ChevronDown size={15} className="text-muted-dark" />
              </button>

              <AnimatePresence>
                {profileOpen && (
                  <motion.div
                    initial={{ opacity: 0, y: 8, scale: 0.98 }}
                    animate={{ opacity: 1, y: 0, scale: 1 }}
                    exit={{ opacity: 0, y: 6 }}
                    transition={{ duration: 0.18 }}
                    className="absolute right-0 top-14 w-64 rounded-xl border border-slate-300 bg-surface-raised p-2 shadow-soft"
                  >
                    <div className="border-b border-slate-300 px-3 py-3">
                      <p className="text-sm font-bold text-foreground">{fullName(currentUser)}</p>
                      <p className="mt-0.5 truncate text-xs text-muted">{currentUser.email}</p>
                    </div>
                    <button
                      className="mt-1 flex min-h-10 w-full items-center gap-2 rounded-lg px-3 text-left text-sm font-semibold text-red-400 hover:bg-red-500/10"
                      onClick={() => {
                        logout()
                        navigate('/login')
                      }}
                    >
                      <LogOut size={16} /> Cerrar sesión
                    </button>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </div>
        </header>

        <main className="relative mx-auto min-h-[calc(100vh-5rem)] max-w-[1600px] overflow-hidden px-4 py-8 sm:px-6 lg:px-10 lg:py-10">
          <div className="ambient-glow -right-80 top-12 opacity-40" aria-hidden="true" />
          <div className="relative z-[1]">
            <Outlet />
          </div>
        </main>
      </div>
      <Toaster richColors theme="dark" position="top-right" />
    </div>
  )
}

