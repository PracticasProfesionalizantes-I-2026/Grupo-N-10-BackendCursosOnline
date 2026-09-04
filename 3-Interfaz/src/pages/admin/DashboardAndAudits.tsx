import { AlertTriangle, ArrowRight, BookOpen, CheckCircle2, ClipboardCheck, Eye, GraduationCap, Users, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import type { Audit, AuditDecision } from '@/types'
import { CourseStatusBadge, EmptyState, MetricCard, Modal, PageHeader, SearchInput, StatusBadge } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { formatDate, formatDuration, fullName } from '@/utils/format'

export function AdminDashboard() {
  const { state } = useApp()
  const published = state.courses.filter((item) => item.status === 'PUBLICADO').length
  const pendingAudits = state.audits.filter((item) => item.status === 'PENDIENTE')
  const pendingEnrollments = state.enrollments.filter((item) => item.status === 'PENDIENTE')
  const activity = state.recentActivity.filter((item) => item.audience === 'ADMINISTRADOR' || item.audience === 'TODOS').slice(0, 5)

  return (
    <>
      <PageHeader eyebrow="Panel de administración" title="Todo bajo control" description="Revisá los procesos que requieren atención y mantené coherente el estado de la plataforma." />
      <section className="grid border-y border-slate-200 py-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Usuarios registrados" value={state.users.length} icon={Users} tone="primary" />
        <MetricCard label="Cursos publicados" value={published} icon={BookOpen} tone="emerald" />
        <MetricCard label="Auditorías pendientes" value={pendingAudits.length} icon={ClipboardCheck} tone="amber" />
        <MetricCard label="Inscripciones pendientes" value={pendingEnrollments.length} icon={GraduationCap} tone="neutral" />
      </section>

      <section className="mt-10 grid gap-7 xl:grid-cols-[1.3fr_.7fr]">
        <article className="border-y border-slate-200 py-6">
          <div className="flex items-end justify-between gap-4">
            <div>
              <p className="eyebrow">Requieren atención</p>
              <h2 className="mt-2 font-display text-3xl font-semibold text-foreground">Pendientes de decisión</h2>
            </div>
            <span className="grid size-11 place-items-center rounded-xl bg-amber-500/10 text-amber-300"><AlertTriangle size={21} /></span>
          </div>
          <div className="mt-6 grid gap-4 md:grid-cols-2">
            <div className="rounded-xl border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <p className="font-bold text-foreground">Cursos en revisión</p>
                <span className="grid size-8 place-items-center rounded-lg bg-primary-soft text-sm font-bold text-primary">{pendingAudits.length}</span>
              </div>
              <div className="mt-4 space-y-3">
                {pendingAudits.slice(0, 3).map((audit) => {
                  const course = state.courses.find((item) => item.id === audit.courseId)
                  const teacher = state.users.find((item) => item.id === audit.teacherId)
                  return (
                    <div key={audit.id} className="rounded-xl bg-background-secondary p-3">
                      <p className="truncate text-sm font-bold text-slate-800">{course?.title}</p>
                      <p className="mt-1 text-xs text-slate-400">{fullName(teacher)} · {audit.type}</p>
                    </div>
                  )
                })}
              </div>
              <Link to="/admin/audits" className="btn-ghost mt-3 w-full">Ir a Auditorías <ArrowRight size={16} /></Link>
            </div>
            <div className="rounded-xl border border-slate-200 p-4">
              <div className="flex items-center justify-between">
                <p className="font-bold text-foreground">Solicitudes de inscripción</p>
                <span className="grid size-8 place-items-center rounded-lg bg-primary-soft text-sm font-bold text-primary">{pendingEnrollments.length}</span>
              </div>
              <div className="mt-4 space-y-3">
                {pendingEnrollments.slice(0, 3).map((enrollment) => {
                  const course = state.courses.find((item) => item.id === enrollment.courseId)
                  const student = state.users.find((item) => item.id === enrollment.studentId)
                  return (
                    <div key={enrollment.id} className="rounded-xl bg-background-secondary p-3">
                      <p className="truncate text-sm font-bold text-slate-800">{fullName(student)}</p>
                      <p className="mt-1 truncate text-xs text-slate-400">{course?.title}</p>
                    </div>
                  )
                })}
              </div>
              <Link to="/admin/enrollments" className="btn-ghost mt-3 w-full">Ir a Inscripciones <ArrowRight size={16} /></Link>
            </div>
          </div>
        </article>

        <article className="border-t border-slate-200 pt-6 xl:border-l xl:border-t-0 xl:pl-7 xl:pt-0">
          <p className="eyebrow">Registro</p>
          <h2 className="mt-2 text-xl font-bold text-foreground">Actividad reciente</h2>
          <div className="mt-5 space-y-5">
            {activity.map((item) => (
              <div key={item.id} className="relative border-l-2 border-primary-border pl-4">
                <span className="absolute -left-[5px] top-1 size-2 rounded-full bg-primary" />
                <p className="text-sm font-medium leading-6 text-slate-700">{item.message}</p>
                <p className="mt-1 text-xs text-slate-400">{formatDate(item.createdAt)}</p>
              </div>
            ))}
          </div>
        </article>
      </section>
    </>
  )
}

export function AdminAudits() {
  const { state, resolveAudit } = useApp()
  const [tab, setTab] = useState<'PENDIENTE' | 'RESUELTA'>('PENDIENTE')
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [changeOpen, setChangeOpen] = useState(false)
  const [observation, setObservation] = useState('')
  const selected = state.audits.find((item) => item.id === selectedId)
  const filtered = useMemo(
    () =>
      state.audits.filter((audit) => {
        const course = state.courses.find((item) => item.id === audit.courseId)
        const teacher = state.users.find((item) => item.id === audit.teacherId)
        const text = (course?.title + ' ' + fullName(teacher)).toLowerCase()
        return audit.status === tab && text.includes(query.toLowerCase())
      }),
    [query, state.audits, state.courses, state.users, tab],
  )

  const decide = (audit: Audit, decision: AuditDecision, observations?: string) => {
    resolveAudit(audit.id, decision, observations)
    setSelectedId(null)
    setChangeOpen(false)
    setObservation('')
    toast.success(
      decision === 'APROBADA'
        ? 'Auditoría aprobada. Curso PUBLICADO.'
        : decision === 'RECHAZADA'
          ? 'Auditoría rechazada. Curso RECHAZADO.'
          : 'Observaciones enviadas. Estado: CAMBIOS SOLICITADOS.',
    )
  }

  return (
    <>
      <PageHeader eyebrow="Control de calidad" title="Auditorías" description="Revisá solicitudes de creación o modificación. No se asignan prioridades ni auditores individuales." />
      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="inline-flex w-fit rounded-xl border border-slate-200 bg-surface p-1">
          {(['PENDIENTE', 'RESUELTA'] as const).map((item) => (
            <button key={item} className={tab === item ? 'btn-primary min-h-9 px-4 py-1.5' : 'btn-ghost min-h-9 px-4 py-1.5'} onClick={() => setTab(item)}>
              {item === 'PENDIENTE' ? 'Pendientes' : 'Resueltas'}
            </button>
          ))}
        </div>
        <SearchInput value={query} onChange={setQuery} placeholder="Buscar curso o profesor" className="w-full sm:max-w-sm" />
      </div>

      {filtered.length ? (
        <div className="grid gap-4">
          {filtered.map((audit) => {
            const course = state.courses.find((item) => item.id === audit.courseId)
            const teacher = state.users.find((item) => item.id === audit.teacherId)
            return (
              <article key={audit.id} className="panel flex flex-col gap-4 lg:flex-row lg:items-center">
                <span className="grid size-12 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary"><ClipboardCheck size={22} /></span>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="truncate text-lg font-bold text-foreground">{course?.title}</h2>
                    {course && <CourseStatusBadge status={course.status} />}
                  </div>
                  <p className="mt-1 text-sm text-slate-500">Prof. {fullName(teacher)} · {audit.type} · {formatDate(audit.createdAt)}</p>
                </div>
                <StatusBadge status={audit.status} />
                <button className="btn-secondary" onClick={() => setSelectedId(audit.id)}><Eye size={16} /> Ver detalle</button>
              </article>
            )
          })}
        </div>
      ) : (
        <EmptyState title={tab === 'PENDIENTE' ? 'No hay auditorías pendientes' : 'No hay auditorías resueltas'} />
      )}

      <AuditDetail
        audit={selected}
        onClose={() => setSelectedId(null)}
        onApprove={() => selected && decide(selected, 'APROBADA')}
        onReject={() => selected && decide(selected, 'RECHAZADA')}
        onRequestChanges={() => setChangeOpen(true)}
      />

      <Modal
        open={changeOpen && Boolean(selected)}
        onClose={() => setChangeOpen(false)}
        title="Solicitar cambios"
        description="Las observaciones son obligatorias y quedarán visibles para el Profesor."
        footer={
          <>
            <button className="btn-secondary" onClick={() => setChangeOpen(false)}>Volver</button>
            <button className="btn-primary" disabled={!observation.trim()} onClick={() => selected && decide(selected, 'CAMBIOS SOLICITADOS', observation.trim())}>Enviar observaciones</button>
          </>
        }
      >
        <label className="block">
          <span className="label">Observaciones *</span>
          <textarea className="textarea" value={observation} onChange={(event) => setObservation(event.target.value)} placeholder="Indicá con claridad qué necesita corregirse." autoFocus />
        </label>
      </Modal>
    </>
  )
}

function AuditDetail({
  audit,
  onClose,
  onApprove,
  onReject,
  onRequestChanges,
}: {
  audit?: Audit
  onClose: () => void
  onApprove: () => void
  onReject: () => void
  onRequestChanges: () => void
}) {
  const { state } = useApp()
  const course = state.courses.find((item) => item.id === audit?.courseId)
  const teacher = state.users.find((item) => item.id === audit?.teacherId)
  return (
    <Modal
      open={Boolean(audit)}
      onClose={onClose}
      title={course?.title ?? 'Detalle de auditoría'}
      description={audit ? audit.type + ' · Prof. ' + fullName(teacher) : undefined}
      size="lg"
      footer={
        audit?.status === 'PENDIENTE' ? (
          <>
            <button className="btn-secondary text-red-400 hover:border-red-500/25 hover:bg-red-500/10" onClick={onReject}><XCircle size={17} /> Rechazar</button>
            <button className="btn-secondary text-amber-300 hover:border-amber-500/25 hover:bg-amber-500/10" onClick={onRequestChanges}><AlertTriangle size={17} /> Solicitar cambios</button>
            <button className="btn-primary" onClick={onApprove}><CheckCircle2 size={17} /> Aprobar</button>
          </>
        ) : <button className="btn-secondary" onClick={onClose}>Cerrar</button>
      }
    >
      {audit && course && (
        <div className="space-y-5">
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-xl bg-background-secondary p-4"><p className="text-xs font-bold uppercase tracking-wider text-slate-400">Estado</p><div className="mt-2"><CourseStatusBadge status={course.status} /></div></div>
            <div className="rounded-xl bg-background-secondary p-4"><p className="text-xs font-bold uppercase tracking-wider text-slate-400">Duración</p><p className="mt-2 font-bold text-foreground">{formatDuration(course.durationHours)}</p></div>
            <div className="rounded-xl bg-background-secondary p-4"><p className="text-xs font-bold uppercase tracking-wider text-slate-400">Módulos</p><p className="mt-2 font-bold text-foreground">{course.modules.length}</p></div>
          </div>
          <div>
            <h3 className="font-bold text-foreground">Descripción</h3>
            <p className="mt-2 text-sm leading-6 text-slate-600">{course.description || 'Sin descripción.'}</p>
          </div>
          <div>
            <h3 className="font-bold text-foreground">Módulos</h3>
            <div className="mt-3 space-y-2">
              {course.modules.map((module, index) => (
                <div key={module.id} className="rounded-xl border border-slate-200 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="font-bold text-slate-800">{index + 1}. {module.name}</p>
                    <span className="text-xs font-semibold text-slate-400">{formatDuration(module.durationHours)}</span>
                  </div>
                  <p className="mt-1 text-sm leading-6 text-slate-500">{module.description || 'Sin descripción.'}</p>
                </div>
              ))}
            </div>
          </div>
          {audit.observations && (
            <div className="rounded-xl border border-amber-500/25 bg-amber-500/10 p-4 text-sm text-amber-300">
              <p className="font-bold">Observaciones</p>
              <p className="mt-1 leading-6">{audit.observations}</p>
            </div>
          )}
        </div>
      )}
    </Modal>
  )
}
