import { BookOpen, CalendarRange, GraduationCap, Users } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Area, AreaChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { toast } from 'sonner'
import { EmptyState, MetricCard, PageHeader } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { dateInputValue } from '@/utils/format'

type Range = { from: string; to: string }

export function AdminReports() {
  const { state } = useApp()
  const today = new Date()
  const initialFrom = new Date(today)
  initialFrom.setMonth(initialFrom.getMonth() - 8)
  const [draft, setDraft] = useState<Range>({ from: dateInputValue(initialFrom), to: dateInputValue(today) })
  const [range, setRange] = useState<Range>(draft)
  const [error, setError] = useState('')

  const report = useMemo(() => {
    const from = new Date(range.from + 'T00:00:00')
    const to = new Date(range.to + 'T23:59:59')
    const inRange = (date: string) => {
      const value = new Date(date)
      return value >= from && value <= to
    }
    const users = state.users.filter((item) => inRange(item.createdAt))
    const courses = state.courses.filter((item) => inRange(item.createdAt))
    const enrollments = state.enrollments.filter((item) => inRange(item.requestedAt))
    const buckets = new Map<string, { name: string; usuarios: number; cursos: number; inscripciones: number }>()
    const bucketFor = (date: string) => {
      const value = new Date(date)
      const key = value.getFullYear() + '-' + String(value.getMonth() + 1).padStart(2, '0')
      if (!buckets.has(key)) {
        buckets.set(key, {
          name: new Intl.DateTimeFormat('es-AR', { month: 'short', year: '2-digit' }).format(value),
          usuarios: 0,
          cursos: 0,
          inscripciones: 0,
        })
      }
      return buckets.get(key)!
    }
    users.forEach((item) => bucketFor(item.createdAt).usuarios++)
    courses.forEach((item) => bucketFor(item.createdAt).cursos++)
    enrollments.forEach((item) => bucketFor(item.requestedAt).inscripciones++)
    return {
      users: users.length,
      courses: courses.length,
      enrollments: enrollments.length,
      chart: Array.from(buckets.entries()).sort(([a], [b]) => a.localeCompare(b)).map(([, value]) => value),
    }
  }, [range, state.courses, state.enrollments, state.users])

  const apply = () => {
    if (!draft.from || !draft.to) {
      setError('Completá las fechas Desde y Hasta.')
      return
    }
    if (new Date(draft.from) > new Date(draft.to)) {
      setError('La fecha Desde no puede ser posterior a Hasta.')
      return
    }
    setError('')
    setRange(draft)
    toast.success('Reporte actualizado.')
  }

  const hasResults = report.users + report.courses + report.enrollments > 0

  return (
    <>
      <PageHeader eyebrow="Información básica" title="Reportes" description="Consultá usuarios registrados, cursos creados e inscripciones realizadas dentro de un rango de fechas." />
      <section className="mb-7 border-y border-slate-200 bg-background-secondary/40 py-5">
        <div className="grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
          <label className="block">
            <span className="label">Desde *</span>
            <input className="input" type="date" value={draft.from} onChange={(event) => setDraft((current) => ({ ...current, from: event.target.value }))} />
          </label>
          <label className="block">
            <span className="label">Hasta *</span>
            <input className="input" type="date" value={draft.to} onChange={(event) => setDraft((current) => ({ ...current, to: event.target.value }))} />
          </label>
          <button className="btn-primary" onClick={apply}><CalendarRange size={17} /> Aplicar rango</button>
        </div>
        {error && <p role="alert" className="mt-3 text-sm font-semibold text-red-400">{error}</p>}
      </section>

      <section className="grid gap-4 sm:grid-cols-3">
        <MetricCard label="Usuarios registrados" value={report.users} icon={Users} tone="primary" />
        <MetricCard label="Cursos creados" value={report.courses} icon={BookOpen} tone="neutral" />
        <MetricCard label="Inscripciones realizadas" value={report.enrollments} icon={GraduationCap} tone="emerald" />
      </section>

      <section className="panel mt-7">
        <div className="mb-6">
          <p className="eyebrow">Evolución en el período</p>
          <h2 className="mt-2 text-2xl font-bold text-foreground">Actividad registrada</h2>
          <p className="mt-1 text-sm text-slate-500">Vista agrupada por mes, sin métricas financieras ni analítica avanzada.</p>
        </div>
        {hasResults ? (
          <div className="h-[360px] w-full" aria-label="Gráfico de actividad mensual">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={report.chart} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="usersFill" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="#F97316" stopOpacity={0.28} /><stop offset="95%" stopColor="#F97316" stopOpacity={0} /></linearGradient>
                  <linearGradient id="coursesFill" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="#D4D4D4" stopOpacity={0.14} /><stop offset="95%" stopColor="#D4D4D4" stopOpacity={0} /></linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="4 4" stroke="#262626" />
                <XAxis dataKey="name" tick={{ fill: '#737373', fontSize: 12 }} axisLine={false} tickLine={false} />
                <YAxis allowDecimals={false} tick={{ fill: '#737373', fontSize: 12 }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={{ borderRadius: 10, borderColor: '#262626', backgroundColor: '#161616', color: '#F5F5F5', boxShadow: '0 18px 50px rgba(0,0,0,.45)' }} />
                <Legend iconType="circle" wrapperStyle={{ color: '#A3A3A3', fontSize: 12, paddingTop: 14 }} />
                <Area type="monotone" dataKey="usuarios" name="Usuarios" stroke="#F97316" strokeWidth={3} fill="url(#usersFill)" />
                <Area type="monotone" dataKey="cursos" name="Cursos" stroke="#D4D4D4" strokeWidth={2} fill="url(#coursesFill)" />
                <Area type="monotone" dataKey="inscripciones" name="Inscripciones" stroke="#16A34A" strokeWidth={3} fillOpacity={0} />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <EmptyState title="No hay datos para mostrar" description="No existen registros dentro del rango seleccionado." />
        )}
      </section>
    </>
  )
}
