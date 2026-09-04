import { AnimatePresence, motion } from 'framer-motion'
import { ArrowLeft, ArrowRight, BookOpen, Check, CheckCircle2, Eye, EyeOff, GraduationCap, UserRoundCheck } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import type { Role } from '@/types'
import { Logo, LumenOrb } from '@/components/UI'
import { useApp } from '@/hooks/useApp'

type RegisterRole = Extract<Role, 'ALUMNO' | 'PROFESOR'>
type FormData = {
  role: RegisterRole
  email: string
  password: string
  firstName: string
  lastName: string
  dni: string
  phone: string
  address: string
  postalCode: string
}

const initialForm: FormData = {
  role: 'ALUMNO',
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  dni: '',
  phone: '',
  address: '',
  postalCode: '',
}

const stepLabels = ['Cuenta', 'Acceso', 'Datos', 'Revisión', 'Listo']

export function Register() {
  const { state, register } = useApp()
  const navigate = useNavigate()
  const [step, setStep] = useState(1)
  const [form, setForm] = useState<FormData>(initialForm)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [showPassword, setShowPassword] = useState(false)

  const setField = (field: keyof FormData, value: string) => {
    setForm((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: '' }))
  }

  const validateStep = () => {
    const next: Record<string, string> = {}
    if (step === 2) {
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) next.email = 'Ingresá un email válido.'
      if (state.users.some((user) => user.email.toLowerCase() === form.email.trim().toLowerCase())) next.email = 'Ese email ya está registrado.'
      if (form.password.length < 6) next.password = 'Usá al menos 6 caracteres.'
    }
    if (step === 3) {
      ;(['firstName', 'lastName', 'dni', 'phone', 'address', 'postalCode'] as const).forEach((field) => {
        if (!form[field].trim()) next[field] = 'Este campo es obligatorio.'
      })
      if (form.dni && !/^\d{7,9}$/.test(form.dni.replace(/\D/g, ''))) next.dni = 'Ingresá un DNI válido.'
    }
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const next = () => {
    if (!validateStep()) return
    setStep((value) => Math.min(4, value + 1))
  }

  const confirm = () => {
    register({ ...form, email: form.email.trim().toLowerCase() })
    setStep(5)
    toast.success('Tu cuenta fue creada')
  }

  return (
    <main className="page-grid relative min-h-screen overflow-hidden bg-canvas px-4 py-6 sm:px-8">
      <LumenOrb className="-right-72 -top-72 opacity-70" />
      <div className="relative z-10 mx-auto max-w-5xl">
        <div className="flex items-center justify-between">
          <Logo />
          <Link to="/login" className="btn-ghost">¿Ya tenés cuenta? <span className="font-bold text-primary">Iniciar sesión</span></Link>
        </div>

        <ol className="mx-auto mt-10 flex max-w-3xl items-start justify-between" aria-label="Progreso del registro">
          {stepLabels.map((label, index) => {
            const number = index + 1
            const complete = step > number
            const active = step === number
            return (
              <li key={label} className="relative flex flex-1 flex-col items-center text-center last:flex-none">
                <span className={'grid size-9 place-items-center rounded-full border text-xs font-bold transition ' + (complete ? 'border-primary bg-primary text-white' : active ? 'border-primary bg-surface text-primary shadow-md shadow-glow' : 'border-slate-200 bg-surface text-slate-400')}>
                  {complete ? <Check size={15} /> : number}
                </span>
                <span className={'mt-2 hidden text-[11px] font-bold sm:block ' + (active || complete ? 'text-primary' : 'text-slate-400')}>{label}</span>
                {number < 5 && <span className={'absolute left-[calc(50%+1.15rem)] right-[calc(-50%+1.15rem)] top-4 h-px ' + (complete ? 'bg-primary' : 'bg-surface-hover')} />}
              </li>
            )
          })}
        </ol>

        <motion.section layout className="card mx-auto mt-8 max-w-3xl overflow-hidden">
          <div className="border-b border-slate-100 bg-surface px-6 py-6 sm:px-8">
            <p className="eyebrow">Paso {step} de 5</p>
            <h1 className="mt-2 font-display text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
              {step === 1 && '¿Cómo querés usar Lumen?'}
              {step === 2 && 'Creá tus datos de acceso'}
              {step === 3 && 'Contanos sobre vos'}
              {step === 4 && 'Revisá tu información'}
              {step === 5 && 'Tu cuenta está lista'}
            </h1>
            {step < 5 && <p className="mt-2 text-sm leading-6 text-slate-500">Completá la información para continuar. Todos los campos indicados son obligatorios.</p>}
          </div>

          <div className="min-h-[340px] p-6 sm:p-8">
            <AnimatePresence mode="wait">
              <motion.div key={step} initial={{ opacity: 0, x: 12 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -12 }} transition={{ duration: 0.18 }}>
                {step === 1 && (
                  <div className="grid gap-4 sm:grid-cols-2">
                    {[
                      ['ALUMNO', GraduationCap, 'Alumno', 'Inscribite en cursos y seguí tu progreso.'],
                      ['PROFESOR', UserRoundCheck, 'Profesor', 'Creá cursos y acompañá a tus alumnos.'],
                    ].map(([role, Icon, title, text]) => {
                      const ItemIcon = Icon as typeof GraduationCap
                      const selected = form.role === role
                      return (
                        <button
                          key={String(role)}
                          onClick={() => setForm((current) => ({ ...current, role: role as RegisterRole }))}
                          className={'relative min-h-44 rounded-xl border p-5 text-left transition hover:-translate-y-0.5 ' + (selected ? 'border-primary-border bg-primary-soft shadow-glow' : 'border-slate-200 bg-surface hover:border-primary-border')}
                        >
                          <span className={'grid size-12 place-items-center rounded-xl ' + (selected ? 'bg-primary text-white' : 'bg-surface-raised text-slate-600')}><ItemIcon size={23} /></span>
                          <span className="mt-5 block text-lg font-bold text-foreground">{String(title)}</span>
                          <span className="mt-1 block text-sm leading-6 text-slate-500">{String(text)}</span>
                          <span className={'absolute right-4 top-4 grid size-6 place-items-center rounded-full border ' + (selected ? 'border-primary bg-primary text-white' : 'border-slate-200')} aria-hidden="true">{selected && <Check size={14} />}</span>
                        </button>
                      )
                    })}
                  </div>
                )}
                {step === 2 && (
                  <div className="mx-auto max-w-lg space-y-5">
                    <label className="block">
                      <span className="label">Email *</span>
                      <input className="input" type="email" value={form.email} onChange={(event) => setField('email', event.target.value)} placeholder="tu@email.com" autoComplete="email" />
                      {errors.email && <span className="mt-1.5 block text-xs font-medium text-red-400">{errors.email}</span>}
                    </label>
                    <label className="block">
                      <span className="label">Contraseña *</span>
                      <span className="relative block">
                        <input className="input pr-12" type={showPassword ? 'text' : 'password'} value={form.password} onChange={(event) => setField('password', event.target.value)} placeholder="Mínimo 6 caracteres" autoComplete="new-password" />
                        <button type="button" className="absolute right-1 top-1/2 grid size-9 -translate-y-1/2 place-items-center rounded-lg text-slate-400 hover:bg-surface-raised" onClick={() => setShowPassword((value) => !value)} aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
                          {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                        </button>
                      </span>
                      {errors.password && <span className="mt-1.5 block text-xs font-medium text-red-400">{errors.password}</span>}
                    </label>
                  </div>
                )}
                {step === 3 && (
                  <div className="grid gap-5 sm:grid-cols-2">
                    {[
                      ['firstName', 'Nombre *', 'Sofía'],
                      ['lastName', 'Apellido *', 'Martínez'],
                      ['dni', 'DNI *', '30123456'],
                      ['phone', 'Teléfono *', '+54 9 11 5555-1234'],
                      ['address', 'Dirección *', 'Av. Siempre Viva 123'],
                      ['postalCode', 'Código Postal *', 'C1000'],
                    ].map(([field, label, placeholder]) => (
                      <label key={field} className="block">
                        <span className="label">{label}</span>
                        <input className="input" value={String(form[field as keyof FormData])} onChange={(event) => setField(field as keyof FormData, event.target.value)} placeholder={placeholder} />
                        {errors[field] && <span className="mt-1.5 block text-xs font-medium text-red-400">{errors[field]}</span>}
                      </label>
                    ))}
                  </div>
                )}
                {step === 4 && (
                  <div className="grid gap-4 sm:grid-cols-2">
                    {[
                      ['Tipo de cuenta', form.role === 'ALUMNO' ? 'Alumno' : 'Profesor'],
                      ['Email', form.email],
                      ['Nombre completo', form.firstName + ' ' + form.lastName],
                      ['DNI', form.dni],
                      ['Teléfono', form.phone],
                      ['Dirección', form.address + ', ' + form.postalCode],
                    ].map(([label, value]) => (
                      <div key={label} className="rounded-xl border border-slate-200 bg-background-secondary p-4">
                        <p className="text-xs font-bold uppercase tracking-wider text-slate-400">{label}</p>
                        <p className="mt-1.5 font-semibold text-slate-800">{value || '—'}</p>
                      </div>
                    ))}
                    <div className="sm:col-span-2 rounded-xl border border-primary-border bg-primary-soft p-4 text-sm leading-6 text-orange-200">
                      Al confirmar, tu cuenta quedará habilitada con el rol seleccionado.
                    </div>
                  </div>
                )}
                {step === 5 && (
                  <div className="flex min-h-[300px] flex-col items-center justify-center text-center">
                    <span className="grid size-20 place-items-center rounded-xl bg-emerald-500/10 text-emerald-300"><CheckCircle2 size={38} /></span>
                    <h2 className="mt-6 text-2xl font-bold text-foreground">¡Bienvenido a Lumen, {form.firstName}!</h2>
                    <p className="mt-2 max-w-md text-sm leading-6 text-slate-500">Tu cuenta como {form.role === 'ALUMNO' ? 'Alumno' : 'Profesor'} fue creada correctamente.</p>
                    <button onClick={() => navigate(form.role === 'ALUMNO' ? '/student' : '/teacher')} className="btn-primary mt-7">Ir a mi panel <ArrowRight size={17} /></button>
                  </div>
                )}
              </motion.div>
            </AnimatePresence>
          </div>

          {step < 5 && (
            <footer className="flex items-center justify-between border-t border-slate-100 bg-background-secondary/70 px-6 py-4 sm:px-8">
              <button className="btn-ghost" onClick={() => (step === 1 ? navigate('/') : setStep((value) => value - 1))}><ArrowLeft size={16} /> Atrás</button>
              {step < 4 ? (
                <button className="btn-primary" onClick={next}>Continuar <ArrowRight size={16} /></button>
              ) : (
                <button className="btn-primary" onClick={confirm}>Confirmar registro <Check size={16} /></button>
              )}
            </footer>
          )}
        </motion.section>
        <p className="mt-6 flex items-center justify-center gap-2 text-center text-xs text-slate-400"><BookOpen size={14} /> El registro público está disponible para Alumnos y Profesores.</p>
      </div>
    </main>
  )
}
