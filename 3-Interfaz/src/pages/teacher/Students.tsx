import { Eye, GraduationCap, UserRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { EmptyState, Modal, PageHeader, ProgressBar, ProgressStatusBadge, SearchInput } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { courseService } from '@/services/courseService'
import { formatDate, fullName } from '@/utils/format'

export function TeacherStudents() {
  const { state, currentUser } = useApp()
  const [query, setQuery] = useState('')
  const [courseId, setCourseId] = useState('TODOS')
  const [selectedProgressId, setSelectedProgressId] = useState<string | null>(null)
  const ownCourses = useMemo(
    () => (currentUser ? courseService.getTeacherCourses(state, currentUser.id) : []),
    [currentUser, state],
  )
  const rows = useMemo(
    () => {
      const ownIds = new Set(ownCourses.map((item) => item.id))
      return state.enrollments
        .filter((item) => item.status === 'APROBADA' && ownIds.has(item.courseId) && (courseId === 'TODOS' || item.courseId === courseId))
        .map((enrollment) => {
          const student = state.users.find((item) => item.id === enrollment.studentId)
          const course = state.courses.find((item) => item.id === enrollment.courseId)
          const progress = state.progressRecords.find((item) => item.studentId === enrollment.studentId && item.courseId === enrollment.courseId)
          return { enrollment, student, course, progress }
        })
        .filter((row) => (fullName(row.student) + ' ' + row.course?.title).toLowerCase().includes(query.toLowerCase()))
    },
    [courseId, ownCourses, query, state.courses, state.enrollments, state.progressRecords, state.users],
  )
  if (!currentUser) return null
  const selected = rows.find((row) => row.progress?.id === selectedProgressId)

  return (
    <>
      <PageHeader eyebrow="Seguimiento" title="Alumnos" description="Consultá únicamente el progreso de alumnos inscriptos en tus propios cursos." />
      <section className="mb-7 grid gap-3 border-y border-slate-200 bg-background-secondary/40 py-5 sm:grid-cols-[1fr_280px]">
        <SearchInput value={query} onChange={setQuery} placeholder="Buscar por alumno o curso" />
        <select className="select" value={courseId} onChange={(event) => setCourseId(event.target.value)} aria-label="Filtrar por curso">
          <option value="TODOS">Todos mis cursos</option>
          {ownCourses.map((course) => <option key={course.id} value={course.id}>{course.title}</option>)}
        </select>
      </section>

      {rows.length ? (
        <div className="table-wrap bg-surface">
          <table className="data-table">
            <thead><tr><th>Alumno</th><th>Curso</th><th>Progreso</th><th>Estado</th><th>Última actividad</th><th>Detalle</th></tr></thead>
            <tbody>
              {rows.map(({ enrollment, student, course, progress }) => (
                <tr key={enrollment.id}>
                  <td>
                    <div className="flex items-center gap-3">
                      <span className="grid size-9 place-items-center rounded-xl bg-primary-soft text-primary"><UserRound size={17} /></span>
                      <div><p className="font-bold text-slate-800">{fullName(student)}</p><p className="mt-0.5 text-xs text-slate-400">{student?.email}</p></div>
                    </div>
                  </td>
                  <td className="font-semibold">{course?.title}</td>
                  <td>
                    <div className="flex min-w-40 items-center gap-3">
                      <ProgressBar value={progress?.percentage ?? 0} className="flex-1" />
                      <span className="w-10 text-right font-bold text-foreground">{progress?.percentage ?? 0} %</span>
                    </div>
                  </td>
                  <td><ProgressStatusBadge status={progress?.status ?? 'NO INICIADO'} /></td>
                  <td>{formatDate(progress?.lastActivityAt)}</td>
                  <td>
                    <button className="btn-ghost min-h-10 px-3" onClick={() => setSelectedProgressId(progress?.id ?? '')} disabled={!progress}><Eye size={16} /> Ver</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <EmptyState title="No hay alumnos con estos filtros" description="Los alumnos aparecerán cuando sus inscripciones a tus cursos estén APROBADAS." />
      )}

      <Modal open={Boolean(selected)} onClose={() => setSelectedProgressId(null)} title={fullName(selected?.student)} description={selected?.course?.title}>
        {selected && (
          <div>
            <div className="rounded-xl bg-surface-raised p-5 text-white">
              <div className="flex items-center justify-between">
                <span className="grid size-11 place-items-center rounded-xl bg-surface/10"><GraduationCap size={21} /></span>
                <span className="font-display text-4xl font-semibold">{selected.progress?.percentage ?? 0} %</span>
              </div>
              <ProgressBar value={selected.progress?.percentage ?? 0} className="mt-5 bg-surface/15" />
              <div className="mt-4 flex items-center justify-between text-sm">
                <ProgressStatusBadge status={selected.progress?.status ?? 'NO INICIADO'} />
                <span className="text-orange-100">{selected.progress?.completedModuleIds.length ?? 0} / {selected.course?.modules.length} módulos</span>
              </div>
            </div>
            <div className="mt-5 space-y-2">
              {selected.course?.modules.map((module, index) => {
                const complete = selected.progress?.completedModuleIds.includes(module.id)
                return (
                  <div key={module.id} className="flex items-center gap-3 rounded-xl bg-background-secondary p-3">
                    <span className={'grid size-8 place-items-center rounded-lg text-xs font-bold ' + (complete ? 'bg-emerald-500/10 text-emerald-300' : 'bg-surface-hover text-slate-500')}>{index + 1}</span>
                    <p className="flex-1 text-sm font-semibold text-slate-700">{module.name}</p>
                    <span className="text-xs font-bold text-slate-400">{complete ? 'Completado' : 'Pendiente'}</span>
                  </div>
                )
              })}
            </div>
          </div>
        )}
      </Modal>
    </>
  )
}
