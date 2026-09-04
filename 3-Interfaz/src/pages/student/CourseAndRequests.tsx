import { CheckCircle2, Clock3, GraduationCap, Layers3, Send, UserRound, XCircle } from 'lucide-react'
import { useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { EmptyState, EnrollmentStatusBadge, InfoPill, Modal, PageHeader } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { courseService } from '@/services/courseService'
import { formatDate, formatDuration, fullName } from '@/utils/format'

export function StudentCourseDetail() {
  const { state, currentUser, requestEnrollment } = useApp()
  const { courseId } = useParams()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const course = courseService.getPublishedCourses(state).find((item) => item.id === courseId)
  if (!course || !currentUser) return <Navigate to="/student/explore" replace />
  const teacher = state.users.find((item) => item.id === course.teacherId)
  const enrollment = state.enrollments.find(
    (item) => item.studentId === currentUser.id && item.courseId === course.id && (item.status === 'PENDIENTE' || item.status === 'APROBADA'),
  )
  const approvedCount = state.enrollments.filter((item) => item.courseId === course.id && item.status === 'APROBADA').length

  const request = () => {
    const created = requestEnrollment(course.id)
    setConfirmOpen(false)
    if (created) toast.success('Solicitud enviada. Estado: PENDIENTE')
    else toast.error('No se pudo crear la solicitud. Revisá si ya existe una inscripción.')
  }

  return (
    <>
      <PageHeader
        eyebrow={course.category}
        title={course.title}
        description={course.description}
        backTo="/student/explore"
        actions={
          enrollment ? (
            enrollment.status === 'APROBADA' ? (
              <Link to={'/student/learn/' + course.id} className="btn-primary">Continuar curso</Link>
            ) : (
              <div className="rounded-xl border border-amber-500/25 bg-amber-500/10 px-4 py-3">
                <EnrollmentStatusBadge status={enrollment.status} />
              </div>
            )
          ) : (
            <button className="btn-primary" onClick={() => setConfirmOpen(true)}><Send size={17} /> Solicitar inscripción</button>
          )
        }
      />

      <div className="grid gap-6 xl:grid-cols-[1fr_360px]">
        <div className="space-y-6">
          <section className="panel">
            <div className="flex flex-wrap gap-2">
              <InfoPill icon={GraduationCap}>{course.level}</InfoPill>
              <InfoPill>{course.modality}</InfoPill>
              <InfoPill icon={Clock3}>{formatDuration(course.durationHours)}</InfoPill>
              <InfoPill icon={Layers3}>{course.modules.length} módulos</InfoPill>
            </div>
            <div className="mt-7 grid gap-7 md:grid-cols-2">
              <div>
                <h2 className="text-lg font-bold text-foreground">Objetivos de aprendizaje</h2>
                <ul className="mt-3 space-y-3">
                  {course.objectives.map((item) => (
                    <li key={item} className="flex gap-3 text-sm leading-6 text-slate-600">
                      <CheckCircle2 className="mt-0.5 shrink-0 text-emerald-300" size={17} /> {item}
                    </li>
                  ))}
                </ul>
              </div>
              <div>
                <h2 className="text-lg font-bold text-foreground">Requisitos previos sugeridos</h2>
                <ul className="mt-3 space-y-3">
                  {course.prerequisites.map((item) => (
                    <li key={item} className="flex gap-3 text-sm leading-6 text-slate-600">
                      <CheckCircle2 className="mt-0.5 shrink-0 text-primary" size={17} /> {item}
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </section>

          <section className="panel">
            <div className="mb-5 flex items-center justify-between">
              <div>
                <p className="eyebrow">Contenido</p>
                <h2 className="mt-2 text-xl font-bold text-foreground">Módulos del curso</h2>
              </div>
              <span className="text-sm font-semibold text-slate-500">{formatDuration(course.durationHours)} en total</span>
            </div>
            <div className="space-y-3">
              {course.modules.map((module, index) => (
                <article key={module.id} className="rounded-xl border border-slate-200 p-4 transition hover:border-primary-border hover:bg-primary-soft">
                  <div className="flex items-start gap-4">
                    <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-surface-raised text-sm font-bold text-slate-600">{index + 1}</span>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <h3 className="font-bold text-slate-800">{module.name}</h3>
                        <span className="text-xs font-semibold text-slate-400">{formatDuration(module.durationHours)}</span>
                      </div>
                      <p className="mt-1 text-sm leading-6 text-slate-500">{module.description}</p>
                      <p className="mt-2 text-xs text-slate-400">{module.resources.length} recursos previstos</p>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          </section>
        </div>

        <aside className="space-y-5">
          <section className="panel sticky top-28">
            <span className="grid size-12 place-items-center rounded-xl bg-primary-soft text-primary"><UserRound size={22} /></span>
            <p className="mt-5 text-xs font-bold uppercase tracking-wider text-slate-400">Profesor</p>
            <h2 className="mt-1 text-xl font-bold text-foreground">{fullName(teacher)}</h2>
            <dl className="mt-5 space-y-3 border-t border-slate-100 pt-5 text-sm">
              <div className="flex justify-between gap-4"><dt className="text-slate-500">Cupo máximo</dt><dd className="font-bold text-slate-800">{course.maxCapacity}</dd></div>
              <div className="flex justify-between gap-4"><dt className="text-slate-500">Inscripciones</dt><dd className="font-bold text-slate-800">{approvedCount}</dd></div>
              <div className="flex justify-between gap-4"><dt className="text-slate-500">Modalidad</dt><dd className="font-bold text-slate-800">{course.modality}</dd></div>
            </dl>
            {!enrollment && <button className="btn-primary mt-6 w-full" onClick={() => setConfirmOpen(true)}>Solicitar inscripción</button>}
            {enrollment?.status === 'PENDIENTE' && <p className="mt-5 rounded-xl bg-amber-500/10 p-3 text-xs leading-5 text-amber-300">Tu solicitud espera revisión. Todavía no tenés acceso al contenido.</p>}
          </section>
        </aside>
      </div>

      <Modal
        open={confirmOpen}
        onClose={() => setConfirmOpen(false)}
        title="Solicitar inscripción"
        description="La solicitud quedará PENDIENTE hasta que un administrador la revise."
        footer={
          <>
            <button className="btn-secondary" onClick={() => setConfirmOpen(false)}>Volver</button>
            <button className="btn-primary" onClick={request}>Confirmar solicitud</button>
          </>
        }
      >
        <div className="rounded-xl bg-background-secondary p-4">
          <p className="font-bold text-foreground">{course.title}</p>
          <p className="mt-1 text-sm text-slate-500">Prof. {fullName(teacher)} · {course.modules.length} módulos</p>
        </div>
      </Modal>
    </>
  )
}

export function StudentRequests() {
  const { state, currentUser, updateEnrollment } = useApp()
  const [filter, setFilter] = useState('TODAS')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  if (!currentUser) return null
  const items = state.enrollments
    .filter((item) => item.studentId === currentUser.id && (filter === 'TODAS' || item.status === filter))
    .sort((a, b) => b.requestedAt.localeCompare(a.requestedAt))
  const selected = state.enrollments.find((item) => item.id === selectedId)
  const selectedCourse = state.courses.find((item) => item.id === selected?.courseId)

  const cancel = () => {
    if (!selected) return
    updateEnrollment(selected.id, 'CANCELADA')
    setSelectedId(null)
    toast.success('La inscripción fue cancelada y el acceso quedó retirado.')
  }

  return (
    <>
      <PageHeader eyebrow="Inscripciones" title="Mis solicitudes" description="Consultá el estado de cada solicitud. Podés cancelar solicitudes pendientes o inscripciones aprobadas." />
      <div className="mb-6 flex flex-wrap gap-2" role="group" aria-label="Filtrar solicitudes">
        {['TODAS', 'PENDIENTE', 'APROBADA', 'RECHAZADA', 'CANCELADA'].map((status) => (
          <button key={status} onClick={() => setFilter(status)} className={filter === status ? 'btn-primary' : 'btn-secondary'}>
            {status === 'TODAS' ? 'Todas' : status}
          </button>
        ))}
      </div>
      {items.length ? (
        <div className="grid gap-4">
          {items.map((enrollment) => {
            const course = state.courses.find((item) => item.id === enrollment.courseId)
            const teacher = state.users.find((item) => item.id === course?.teacherId)
            return (
              <article key={enrollment.id} className="panel flex flex-col gap-4 md:flex-row md:items-center">
                <span className="grid size-12 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
                  {enrollment.status === 'APROBADA' ? <CheckCircle2 size={22} /> : enrollment.status === 'RECHAZADA' || enrollment.status === 'CANCELADA' ? <XCircle size={22} /> : <Clock3 size={22} />}
                </span>
                <div className="min-w-0 flex-1">
                  <h2 className="truncate text-lg font-bold text-foreground">{course?.title}</h2>
                  <p className="mt-1 text-sm text-slate-500">Prof. {fullName(teacher)} · Solicitada el {formatDate(enrollment.requestedAt)}</p>
                </div>
                <EnrollmentStatusBadge status={enrollment.status} />
                {(enrollment.status === 'PENDIENTE' || enrollment.status === 'APROBADA') && (
                  <button className="btn-secondary text-red-400 hover:border-red-500/25 hover:bg-red-500/10 hover:text-red-300" onClick={() => setSelectedId(enrollment.id)}>Cancelar</button>
                )}
              </article>
            )
          })}
        </div>
      ) : (
        <EmptyState title="No hay solicitudes con este estado" />
      )}

      <Modal
        open={Boolean(selected)}
        onClose={() => setSelectedId(null)}
        title="Cancelar inscripción"
        description="El estado cambiará a CANCELADA y, si tenías acceso, se retirará inmediatamente."
        footer={
          <>
            <button className="btn-secondary" onClick={() => setSelectedId(null)}>Volver</button>
            <button className="btn-danger" onClick={cancel}>Confirmar cancelación</button>
          </>
        }
      >
        <p className="text-sm leading-6 text-slate-600">Vas a cancelar tu inscripción a <strong className="text-slate-900">{selectedCourse?.title}</strong>.</p>
      </Modal>
    </>
  )
}
