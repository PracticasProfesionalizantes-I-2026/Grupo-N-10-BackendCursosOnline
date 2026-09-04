import { BookOpen, Check, CheckCircle2, Circle, FileText, PlayCircle, TrendingUp } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { CourseCard } from '@/components/CourseCard'
import { EmptyState, MetricCard, PageHeader, ProgressBar, ProgressStatusBadge } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { courseService } from '@/services/courseService'
import { formatDate, formatDuration, fullName } from '@/utils/format'

export function StudentMyCourses() {
  const { state, currentUser } = useApp()
  if (!currentUser) return null
  const courses = courseService.getApprovedStudentCourses(state, currentUser.id)
  return (
    <>
      <PageHeader eyebrow="Aprendizaje" title="Mis cursos" description="Acá aparecen únicamente los cursos con inscripción APROBADA." />
      {courses.length ? (
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {courses.map((course) => {
            const progress = state.progressRecords.find((item) => item.studentId === currentUser.id && item.courseId === course.id)
            return <CourseCard key={course.id} course={course} href={'/student/learn/' + course.id} progress={progress?.percentage ?? 0} actionLabel="Continuar" />
          })}
        </div>
      ) : (
        <EmptyState title="Todavía no tenés cursos aprobados" description="Cuando un administrador apruebe tu solicitud, el curso aparecerá acá." action={<Link to="/student/explore" className="btn-primary">Explorar cursos</Link>} />
      )}
    </>
  )
}

export function StudentProgress() {
  const { state, currentUser } = useApp()
  if (!currentUser) return null
  const approvedIds = new Set(
    state.enrollments
      .filter((item) => item.studentId === currentUser.id && item.status === 'APROBADA')
      .map((item) => item.courseId),
  )
  const records = state.progressRecords.filter((item) => item.studentId === currentUser.id && approvedIds.has(item.courseId))
  const average = records.length ? Math.round(records.reduce((sum, item) => sum + item.percentage, 0) / records.length) : 0
  const completed = records.filter((item) => item.status === 'COMPLETADO').length

  return (
    <>
      <PageHeader eyebrow="Seguimiento" title="Mi progreso" description="Tu avance se calcula según los módulos que marcaste como completados." />
      <div className="grid gap-4 sm:grid-cols-3">
        <MetricCard label="Progreso promedio" value={average + ' %'} icon={TrendingUp} />
        <MetricCard label="Cursos en progreso" value={records.filter((item) => item.status === 'EN PROGRESO').length} icon={PlayCircle} tone="neutral" />
        <MetricCard label="Completados" value={completed} icon={CheckCircle2} tone="emerald" />
      </div>
      <section className="mt-7 space-y-4">
        {records.map((record) => {
          const course = state.courses.find((item) => item.id === record.courseId)
          const teacher = state.users.find((item) => item.id === course?.teacherId)
          if (!course) return null
          return (
            <article key={record.id} className="panel">
              <div className="flex flex-col gap-4 md:flex-row md:items-center">
                <span className="grid size-12 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary"><BookOpen size={21} /></span>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-3">
                    <h2 className="text-lg font-bold text-foreground">{course.title}</h2>
                    <ProgressStatusBadge status={record.status} />
                  </div>
                  <p className="mt-1 text-sm text-slate-500">Prof. {fullName(teacher)} · Última actividad: {formatDate(record.lastActivityAt)}</p>
                </div>
                <p className="font-display text-3xl font-semibold text-foreground">{record.percentage} %</p>
              </div>
              <ProgressBar value={record.percentage} className="mt-5" />
              <div className="mt-3 flex items-center justify-between text-xs text-slate-500">
                <span>{record.completedModuleIds.length} de {course.modules.length} módulos completados</span>
                <Link className="font-bold text-primary" to={'/student/learn/' + course.id}>Abrir curso →</Link>
              </div>
            </article>
          )
        })}
        {!records.length && <EmptyState title="Sin progreso para mostrar" description="Necesitás una inscripción APROBADA para comenzar un curso." />}
      </section>
    </>
  )
}

export function StudentCoursePlayer() {
  const { state, currentUser, toggleModule } = useApp()
  const { courseId } = useParams()
  const [selectedModuleId, setSelectedModuleId] = useState<string | undefined>()
  const course = state.courses.find((item) => item.id === courseId)
  const approved = state.enrollments.some(
    (item) => item.studentId === currentUser?.id && item.courseId === courseId && item.status === 'APROBADA',
  )
  const progress = state.progressRecords.find(
    (item) => item.studentId === currentUser?.id && item.courseId === courseId,
  )
  const selected = useMemo(
    () => course?.modules.find((item) => item.id === selectedModuleId) ?? course?.modules[0],
    [course, selectedModuleId],
  )
  if (!course || !currentUser || !approved) return <Navigate to="/student/my-courses" replace />

  const completed = new Set(progress?.completedModuleIds ?? [])
  const percentage = progress?.percentage ?? 0
  const status = progress?.status ?? 'NO INICIADO'

  const mark = () => {
    if (!selected) return
    const wasComplete = completed.has(selected.id)
    toggleModule(course.id, selected.id)
    toast.success(wasComplete ? 'El módulo volvió a pendiente.' : 'Módulo completado. Tu progreso fue actualizado.')
  }

  return (
    <>
      <PageHeader
        eyebrow="Experiencia de aprendizaje"
        title={course.title}
        description={'Prof. ' + fullName(state.users.find((item) => item.id === course.teacherId))}
        backTo="/student/my-courses"
      />
      <div className="grid gap-6 xl:grid-cols-[360px_1fr]">
        <aside className="panel h-fit xl:sticky xl:top-28">
          <div className="flex items-center justify-between">
            <ProgressStatusBadge status={status} />
            <span className="text-sm font-bold text-foreground">{percentage} %</span>
          </div>
          <ProgressBar value={percentage} className="mt-3" />
          <p className="mt-3 text-xs text-slate-500">{completed.size} de {course.modules.length} módulos completados</p>
          <div className="mt-6 space-y-2">
            {course.modules.map((module, index) => {
              const isComplete = completed.has(module.id)
              const isSelected = selected?.id === module.id
              return (
                <button
                  key={module.id}
                  onClick={() => setSelectedModuleId(module.id)}
                  className={'flex w-full items-start gap-3 rounded-xl border p-3 text-left transition ' + (isSelected ? 'border-primary-border bg-primary-soft' : 'border-transparent hover:bg-background-secondary')}
                >
                  <span className={'mt-0.5 grid size-8 shrink-0 place-items-center rounded-xl ' + (isComplete ? 'bg-emerald-500/10 text-emerald-300' : 'bg-surface-raised text-slate-500')}>
                    {isComplete ? <Check size={16} /> : <span className="text-xs font-bold">{index + 1}</span>}
                  </span>
                  <span className="min-w-0">
                    <span className="block text-sm font-bold text-slate-800">{module.name}</span>
                    <span className="mt-0.5 block text-xs text-slate-400">{formatDuration(module.durationHours)}</span>
                  </span>
                </button>
              )
            })}
          </div>
        </aside>

        {selected && (
          <section className="card overflow-hidden">
            <div className="aspect-video min-h-64 course-art p-8 text-white sm:p-12">
              <div className="flex h-full flex-col items-center justify-center text-center">
                <span className="grid size-20 place-items-center rounded-full border border-white/20 bg-surface/10 backdrop-blur"><PlayCircle size={38} /></span>
                <p className="mt-6 text-xs font-bold uppercase tracking-[0.18em] text-orange-200">Contenido simulado</p>
                <h2 className="mt-3 font-display text-3xl font-semibold tracking-tight sm:text-4xl">{selected.name}</h2>
                <p className="mt-3 max-w-xl text-sm leading-6 text-orange-100">{selected.description}</p>
              </div>
            </div>
            <div className="p-6 sm:p-8">
              <div className="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="eyebrow">Recursos del módulo</p>
                  <h3 className="mt-2 text-xl font-bold text-foreground">Material para aprender</h3>
                </div>
                <button onClick={mark} className={completed.has(selected.id) ? 'btn-secondary' : 'btn-primary'}>
                  {completed.has(selected.id) ? <><Circle size={17} /> Marcar como pendiente</> : <><CheckCircle2 size={17} /> Marcar como completado</>}
                </button>
              </div>
              <div className="mt-6 grid gap-3 sm:grid-cols-2">
                {selected.resources.map((resource, index) => (
                  <div key={resource} className="flex items-center gap-3 rounded-xl border border-slate-200 p-4">
                    <span className="grid size-10 place-items-center rounded-xl bg-primary-soft text-primary">{index % 2 ? <PlayCircle size={19} /> : <FileText size={19} />}</span>
                    <div>
                      <p className="text-sm font-bold text-slate-800">{resource}</p>
                      <p className="mt-0.5 text-xs text-slate-400">{index % 2 ? 'Video simulado' : 'Lectura simulada'}</p>
                    </div>
                  </div>
                ))}
              </div>
              {percentage === 100 && (
                <div className="mt-6 flex items-center gap-3 rounded-xl border border-emerald-500/25 bg-emerald-500/10 p-4 text-emerald-300">
                  <CheckCircle2 size={22} />
                  <div><p className="font-bold">Curso completado</p><p className="text-sm">Alcanzaste el 100 % del progreso definido.</p></div>
                </div>
              )}
            </div>
          </section>
        )}
      </div>
    </>
  )
}
