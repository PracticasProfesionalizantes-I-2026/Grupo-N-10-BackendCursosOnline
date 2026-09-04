import { CheckCircle2, UserRound, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import type { EnrollmentStatus } from '@/types'
import { EmptyState, EnrollmentStatusBadge, Modal, PageHeader, SearchInput } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { formatDate, fullName } from '@/utils/format'

type PendingAction = { id: string; status: EnrollmentStatus } | null

export function AdminEnrollments() {
  const { state, updateEnrollment } = useApp()
  const [query, setQuery] = useState('')
  const [filter, setFilter] = useState('TODAS')
  const [pendingAction, setPendingAction] = useState<PendingAction>(null)

  const rows = useMemo(
    () =>
      state.enrollments
        .filter((enrollment) => {
          const student = state.users.find((item) => item.id === enrollment.studentId)
          const course = state.courses.find((item) => item.id === enrollment.courseId)
          const text = (fullName(student) + ' ' + course?.title).toLowerCase()
          return text.includes(query.toLowerCase()) && (filter === 'TODAS' || enrollment.status === filter)
        })
        .sort((a, b) => b.requestedAt.localeCompare(a.requestedAt)),
    [filter, query, state.courses, state.enrollments, state.users],
  )
  const target = state.enrollments.find((item) => item.id === pendingAction?.id)
  const targetStudent = state.users.find((item) => item.id === target?.studentId)
  const targetCourse = state.courses.find((item) => item.id === target?.courseId)

  const confirm = () => {
    if (!pendingAction) return
    updateEnrollment(pendingAction.id, pendingAction.status)
    toast.success(
      pendingAction.status === 'APROBADA'
        ? 'Inscripción APROBADA. El curso ya está disponible para el Alumno.'
        : pendingAction.status === 'CANCELADA'
          ? 'Inscripción CANCELADA. El acceso fue retirado.'
          : 'Inscripción RECHAZADA.',
    )
    setPendingAction(null)
  }

  return (
    <>
      <PageHeader eyebrow="Gestión académica" title="Solicitudes de inscripción" description="Aprobá, rechazá o cancelá inscripciones. Cada cambio impacta inmediatamente en el acceso del Alumno." />
      <section className="mb-7 grid gap-3 border-y border-slate-200 bg-background-secondary/40 py-5 sm:grid-cols-[1fr_260px]">
        <SearchInput value={query} onChange={setQuery} placeholder="Buscar por alumno o curso" />
        <select className="select" value={filter} onChange={(event) => setFilter(event.target.value)} aria-label="Filtrar por estado">
          {['TODAS', 'PENDIENTE', 'APROBADA', 'RECHAZADA', 'CANCELADA'].map((item) => <option key={item}>{item === 'TODAS' ? 'Todos los estados' : item}</option>)}
        </select>
      </section>

      {rows.length ? (
        <div className="table-wrap bg-surface">
          <table className="data-table">
            <thead><tr><th>Alumno</th><th>Curso</th><th>Fecha</th><th>Estado</th><th>Acciones</th></tr></thead>
            <tbody>
              {rows.map((enrollment) => {
                const student = state.users.find((item) => item.id === enrollment.studentId)
                const course = state.courses.find((item) => item.id === enrollment.courseId)
                return (
                  <tr key={enrollment.id}>
                    <td>
                      <div className="flex items-center gap-3">
                        <span className="grid size-9 place-items-center rounded-xl bg-primary-soft text-primary"><UserRound size={17} /></span>
                        <div><p className="font-bold text-slate-800">{fullName(student)}</p><p className="mt-0.5 text-xs text-slate-400">{student?.email}</p></div>
                      </div>
                    </td>
                    <td className="font-semibold text-slate-800">{course?.title}</td>
                    <td>{formatDate(enrollment.requestedAt)}</td>
                    <td><EnrollmentStatusBadge status={enrollment.status} /></td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        {enrollment.status === 'PENDIENTE' && (
                          <>
                            <button className="btn-primary min-h-9 px-3 py-1.5 text-xs" onClick={() => setPendingAction({ id: enrollment.id, status: 'APROBADA' })}><CheckCircle2 size={15} /> Aprobar</button>
                            <button className="btn-secondary min-h-9 px-3 py-1.5 text-xs text-red-400" onClick={() => setPendingAction({ id: enrollment.id, status: 'RECHAZADA' })}><XCircle size={15} /> Rechazar</button>
                          </>
                        )}
                        {enrollment.status === 'APROBADA' && (
                          <button className="btn-secondary min-h-9 px-3 py-1.5 text-xs text-red-400" onClick={() => setPendingAction({ id: enrollment.id, status: 'CANCELADA' })}>Cancelar acceso</button>
                        )}
                        {(enrollment.status === 'RECHAZADA' || enrollment.status === 'CANCELADA') && <span className="text-xs text-slate-400">Sin acciones disponibles</span>}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      ) : (
        <EmptyState />
      )}

      <Modal
        open={Boolean(pendingAction)}
        onClose={() => setPendingAction(null)}
        title={
          pendingAction?.status === 'APROBADA'
            ? 'Aprobar inscripción'
            : pendingAction?.status === 'CANCELADA'
              ? 'Cancelar inscripción'
              : 'Rechazar inscripción'
        }
        footer={
          <>
            <button className="btn-secondary" onClick={() => setPendingAction(null)}>Volver</button>
            <button className={pendingAction?.status === 'APROBADA' ? 'btn-primary' : 'btn-danger'} onClick={confirm}>Confirmar</button>
          </>
        }
      >
        <div className="rounded-xl bg-background-secondary p-4">
          <p className="font-bold text-foreground">{fullName(targetStudent)}</p>
          <p className="mt-1 text-sm text-slate-500">{targetCourse?.title}</p>
        </div>
        {pendingAction?.status === 'APROBADA' && <p className="mt-4 text-sm leading-6 text-slate-600">El curso aparecerá inmediatamente en “Mis cursos” y se creará un progreso en 0 %.</p>}
        {pendingAction?.status === 'CANCELADA' && <p className="mt-4 text-sm leading-6 text-red-300">El Alumno perderá el acceso activo al curso.</p>}
      </Modal>
    </>
  )
}
