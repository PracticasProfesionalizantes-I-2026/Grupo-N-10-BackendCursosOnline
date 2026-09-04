import { motion } from 'framer-motion'
import { ArrowLeft, ArrowRight, Eye, EyeOff, GraduationCap, ShieldCheck, UserRoundCheck } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import type { Role } from '@/types'
import { useApp } from '@/hooks/useApp'
import { Logo, LumenOrb } from '@/components/UI'

const routeByRole: Record<Role, string> = {
  ALUMNO: '/student',
  PROFESOR: '/teacher',
  ADMINISTRADOR: '/admin',
}

export function Login() {
  const { login, loginAs } = useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({})

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const nextErrors: Record<string, string> = {}
    if (!email.trim()) nextErrors.email = 'Ingresá tu email.'
    if (!password) nextErrors.password = 'Ingresá tu contraseña.'
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return
    const user = login(email, password)
    if (!user) {
      setErrors({ form: 'Las credenciales no son válidas o la cuenta está deshabilitada.' })
      return
    }
    toast.success('Bienvenido, ' + user.firstName)
    const requested = (location.state as { from?: string } | null)?.from
    const prefixByRole: Record<Role, string> = {
      ALUMNO: '/student',
      PROFESOR: '/teacher',
      ADMINISTRADOR: '/admin',
    }
    navigate(requested && requested.startsWith(prefixByRole[user.role]) ? requested : routeByRole[user.role])
  }

  const enterDemo = (role: Role) => {
    const user = loginAs(role)
    toast.success('Ingresaste como ' + user.firstName)
    navigate(routeByRole[role])
  }

  return (
    <main className="grid min-h-screen bg-background text-foreground lg:grid-cols-[1.04fr_.96fr]">
      <section className="page-grid relative hidden min-h-screen overflow-hidden border-r border-slate-200 bg-background-secondary p-12 lg:flex lg:flex-col lg:justify-between xl:p-16">
        <LumenOrb className="-right-52 top-12" />
        <div className="ambient-glow -right-16 top-20 opacity-60" aria-hidden="true" />
        <Logo />
        <motion.div
          initial={{ opacity: 0, y: 18 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.52, ease: [0.22, 1, 0.36, 1] }}
          className="relative z-10 max-w-2xl"
        >
          <p className="eyebrow">Lumen LMS</p>
          <p className="mt-5 text-balance font-display text-5xl font-semibold leading-[1.02] tracking-[-0.05em] xl:text-6xl">
            Todo lo importante,
            <span className="block text-primary">claro y en un solo lugar.</span>
          </p>
          <div className="mt-10 grid grid-cols-3 border-y border-slate-200">
            {[
              ['Cursos', 'por módulos'],
              ['Progreso', 'siempre visible'],
              ['Gestión', 'sin fricción'],
            ].map(([title, text], index) => (
              <div key={title} className={'py-5 ' + (index ? 'border-l border-slate-200 pl-5' : '')}>
                <p className="text-sm font-bold text-foreground">{title}</p>
                <p className="mt-1 text-xs text-muted">{text}</p>
              </div>
            ))}
          </div>
        </motion.div>
        <p className="relative z-10 text-xs uppercase tracking-[0.2em] text-muted-dark">Sistema académico de gestión de cursos online</p>
      </section>

      <section className="flex min-h-screen flex-col bg-background px-5 py-6 sm:px-10 lg:px-14 xl:px-20">
        <div className="flex items-center justify-between lg:justify-end">
          <span className="lg:hidden"><Logo /></span>
          <Link to="/" className="btn-ghost"><ArrowLeft size={16} /> Inicio</Link>
        </div>

        <div className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center py-12">
          <motion.div initial={{ opacity: 0, y: 14 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.42 }}>
            <p className="eyebrow">Acceso a Lumen</p>
            <h1 className="mt-3 font-display text-5xl font-semibold tracking-[-0.045em] text-foreground">Bienvenido de vuelta</h1>
            <p className="mt-3 text-base leading-7 text-muted">Ingresá a tu cuenta para continuar donde lo dejaste.</p>
          </motion.div>

          <form onSubmit={submit} className="mt-9 space-y-5" noValidate>
            {errors.form && <div role="alert" className="rounded-lg border border-red-500/25 bg-red-500/10 px-4 py-3 text-sm text-red-300">{errors.form}</div>}
            <label className="block">
              <span className="label">Email</span>
              <input
                className="input"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="tu@email.com"
                aria-invalid={Boolean(errors.email)}
              />
              {errors.email && <span className="mt-1.5 block text-xs font-medium text-red-400">{errors.email}</span>}
            </label>
            <label className="block">
              <span className="label">Contraseña</span>
              <span className="relative block">
                <input
                  className="input pr-12"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  placeholder="••••••••"
                  aria-invalid={Boolean(errors.password)}
                />
                <button type="button" className="absolute right-1 top-1/2 grid size-9 -translate-y-1/2 place-items-center rounded-lg text-muted-dark hover:bg-surface-hover hover:text-foreground" onClick={() => setShowPassword((value) => !value)} aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </span>
              {errors.password && <span className="mt-1.5 block text-xs font-medium text-red-400">{errors.password}</span>}
            </label>
            <button type="submit" className="btn-primary w-full">Iniciar sesión <ArrowRight size={17} /></button>
          </form>

          <div className="my-7 flex items-center gap-3 text-xs font-semibold uppercase tracking-[0.16em] text-muted-dark">
            <span className="h-px flex-1 bg-surface-hover" /> Accesos demo <span className="h-px flex-1 bg-surface-hover" />
          </div>

          <div className="grid gap-2 sm:grid-cols-3">
            {[
              ['ALUMNO', GraduationCap, 'Alumno'],
              ['PROFESOR', UserRoundCheck, 'Profesor'],
              ['ADMINISTRADOR', ShieldCheck, 'Admin'],
            ].map(([role, Icon, label]) => {
              const ItemIcon = Icon as typeof GraduationCap
              return (
                <button key={String(role)} onClick={() => enterDemo(role as Role)} className="btn-secondary flex-col gap-1.5 py-3">
                  <ItemIcon size={18} className="text-primary" />
                  <span>{String(label)}</span>
                </button>
              )
            })}
          </div>
          <p className="mt-5 text-center text-xs text-muted-dark">Contraseña demo: <strong className="text-foreground-secondary">demo123</strong></p>
          <p className="mt-8 text-center text-sm text-muted">¿No tenés cuenta? <Link to="/registro" className="font-bold text-primary hover:text-orange-300">Crear cuenta</Link></p>
        </div>
      </section>
    </main>
  )
}

