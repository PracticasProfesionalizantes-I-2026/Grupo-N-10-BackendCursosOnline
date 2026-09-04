import { AlertTriangle, ArrowRight, BookOpen, Clock3, FileEdit, FilePlus2, PauseCircle, PlayCircle, Users } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, Navigate, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'
import { CourseCard } from '@/components/CourseCard'
import { CourseForm } from '@/components/CourseForm'
import { CourseStatusBadge, EmptyState, MetricCard, PageHeader, SearchInput } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { courseService } from '@/services/courseService'
import { formatDate, formatDuration } from '@/utils/format'

export function TeacherDashboard() {
  const { state, currentUser } = useApp()
  if (!currentUser) return null
  const courses = courseService.getTeacherCourses(state, currentUser.id)
  const ownCourseIds = new Set(courses.map((item) => item.id))
  const activeStudents = new Set(
    state.enrollments
      .filter((item) => ownCourseIds.has(item.courseId) && item.status === 'APROBADA')
      .map((item) => item.studentId),
  ).size
  const activity = state.recentActivity
    .filter((item) => item.audience === 'TODOS' || item.audience === 'PROFESOR')
    .slice(0, 5)

  return (
    <>
      <PageHeader
        eyebrow="Panel del profesor"
        title="Tus cursos, con claridad"
        description={'Hola, ' + currentUser.firstName + '. Gestioná contenidos, revisiones y el avance de tus alumnos.'}
        actions={<Link to="/teacher/create" className="btn-primary"><FilePlus2 size={17} /> Crear curso</Link>}
      />
      <section className="grid border-y border-slate-200 py-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Cursos publicados" value={courses.filter((item) => item.status === 'PUBLICADO').length} icon={BookOpen} tone="emerald" />
        <MetricCard label="Borradores" value={courses.filter((item) => item.status === 'BORRADOR').length} icon={FileEdit} tone="primary" />
        <MetricCard label="En revisión" value={courses.filter((item) => item.status === 'EN REVISIÓN').length} icon={Clock3} tone="amber" />
        <MetricCard label="Alumnos activos" value={activeStudents} icon={Users} tone="neutral" />
      </section>

      <section className="mt-10 grid gap-7 xl:grid-cols-[1.35fr_.65fr]">
        <article className="border-y border-slate-200 py-6">
          <div className="flex items-end justify-between gap-4">
            <div>
              <p className="eyebrow">Contenido</p>
              <h2 className="mt-2 font-display text-3xl font-semibold text-foreground">Mis cursos</h2>
            </div>
            <Link to="/teacher/courses" className="text-sm font-bold text-primary">Ver todos →</Link>
          </div>
          <div className="mt-5 divide-y divide-slate-100">
            {courses.slice(0, 5).map((course) => {
              const approved = state.enrollments.filter((item) => item.courseId === course.id && item.status === 'APROBADA').length
              return (
                <div key={course.id} className="flex flex-col gap-3 py-4 first:pt-0 sm:flex-row sm:items-center">
                  <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary"><BookOpen size={20} /></span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-bold text-slate-800">{course.title}</p>
                    <p className="mt-1 text-xs text-slate-400">{course.modules.length} módulos · {approved} alumnos</p>
                  </div>
                  <CourseStatusBadge status={course.status} />
                  <Link to={'/teacher/courses/' + course.id + '/edit'} className="btn-ghost min-h-10 px-3">Abrir <ArrowRight size={15} /></Link>
                </div>
              )
            })}
          </div>
        </article>
        <article className="border-t border-slate-200 pt-6 xl:border-l xl:border-t-0 xl:pl-7 xl:pt-0">
          <div className="flex items-center justify-between">
            <div>
              <p className="eyebrow">Novedades</p>
              <h2 className="mt-2 text-xl font-bold text-foreground">Actividad reciente</h2>
            </div>
            <Clock3 size={20} className="text-primary" />
          </div>
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

export function TeacherCourses() {
  const { state, currentUser, setCourseStatus } = useApp()
  const [searchParams] = useSearchParams()
  const [query, setQuery] = useState(searchParams.get('q') ?? '')
  const [status, setStatus] = useState('TODOS')
  const courses = useMemo(
    () => (currentUser ? courseService.getTeacherCourses(state, currentUser.id) : []),
    [currentUser, state],
  )
  const filtered = useMemo(
    () => courses.filter((course) => course.title.toLowerCase().includes(query.toLowerCase()) && (status === 'TODOS' || course.status === status)),
    [courses, query, status],
  )
  if (!currentUser) return null

  return (
    <>
      <PageHeader
        eyebrow="Gestión de contenido"
        title="Mis cursos"
        description="Solo podés editar cursos propios. Los cambios enviados pasan nuevamente por auditoría."
        actions={<Link to="/teacher/create" className="btn-primary"><FilePlus2 size={17} /> Crear curso</Link>}
      />
      <section className="mb-7 grid gap-3 border-y border-slate-200 bg-background-secondary/40 py-5 sm:grid-cols-[1fr_240px]">
        <SearchInput value={query} onChange={setQuery} placeholder="Buscar por título" />
        <select className="select" value={status} onChange={(event) => setStatus(event.target.value)} aria-label="Filtrar por estado">
          {['TODOS', 'BORRADOR', 'EN REVISIÓN', 'CAMBIOS SOLICITADOS', 'RECHAZADO', 'PUBLICADO', 'PAUSADO', 'FINALIZADO'].map((item) => <option key={item}>{item === 'TODOS' ? 'Todos los estados' : item}</option>)}
        </select>
      </section>
      {filtered.length ? (
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {filtered.map((course) => (
            <div key={course.id} className="relative">
              <CourseCard course={course} href={'/teacher/courses/' + course.id + '/edit'} actionLabel={course.status === 'FINALIZADO' ? 'Consultar' : 'Editar curso'} showStatus />
              {(course.status === 'PUBLICADO' || course.status === 'PAUSADO') && (
                <button
                  className="btn-secondary absolute right-3 top-[8.7rem] z-10 min-h-9 px-3 text-xs"
                  onClick={() => {
                    const next = course.status === 'PUBLICADO' ? 'PAUSADO' : 'PUBLICADO'
                    setCourseStatus(course.id, next)
                    toast.success(next === 'PAUSADO' ? 'Curso pausado.' : 'Curso reactivado.')
                  }}
                >
                  {course.status === 'PUBLICADO' ? <PauseCircle size={15} /> : <PlayCircle size={15} />}
                  {course.status === 'PUBLICADO' ? 'Pausar' : 'Reactivar'}
                </button>
              )}
            </div>
          ))}
        </div>
      ) : (
        <EmptyState title="No hay cursos con estos filtros" action={<Link to="/teacher/create" className="btn-primary">Crear curso</Link>} />
      )}
    </>
  )
}

export function TeacherCreateCourse() {
  const { createCourse } = useApp()
  const navigate = useNavigate()
  return (
    <>
      <PageHeader eyebrow="Nuevo contenido" title="Crear curso" description="Prepará la propuesta, organizá al menos un módulo y enviala a revisión cuando esté lista." backTo="/teacher/courses" />
      <CourseForm
        onSave={(draft, submit) => {
          const course = createCourse(draft, submit)
          toast.success(submit ? 'Curso enviado. Estado: EN REVISIÓN' : 'Curso guardado como BORRADOR')
          navigate('/teacher/courses/' + course.id + '/edit')
        }}
      />
    </>
  )
}

export function TeacherEditCourse() {
  const { state, currentUser, updateCourse } = useApp()
  const { courseId } = useParams()
  const navigate = useNavigate()
  const course = state.courses.find((item) => item.id === courseId)
  if (!course || !currentUser || course.teacherId !== currentUser.id) return <Navigate to="/teacher/courses" replace />

  return (
    <>
      <PageHeader
        eyebrow="Edición de curso"
        title={course.title}
        description={course.status === 'FINALIZADO' ? 'Este curso está FINALIZADO y no admite modificaciones.' : 'Editá el contenido y volvé a enviarlo para su revisión administrativa.'}
        backTo="/teacher/courses"
        actions={<CourseStatusBadge status={course.status} />}
      />

      {course.adminObservation && (
        <div className="mb-6 flex items-start gap-3 rounded-xl border border-amber-500/25 bg-amber-500/10 p-5 text-amber-300">
          <AlertTriangle className="mt-0.5 shrink-0" size={21} />
          <div>
            <p className="font-bold">Observaciones del Administrador</p>
            <p className="mt-1 text-sm leading-6">{course.adminObservation}</p>
          </div>
        </div>
      )}

      {course.publishedVersion && (
        <div className="mb-6 rounded-xl border border-primary-border bg-primary-soft p-5 text-orange-100">
          <p className="font-bold">Versión actualmente publicada</p>
          <p className="mt-1 text-sm leading-6">La versión anterior sigue visible para alumnos mientras estos cambios esperan una decisión.</p>
        </div>
      )}

      {course.status === 'FINALIZADO' ? (
        <section className="panel">
          <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <div><dt className="text-xs font-bold uppercase tracking-wider text-slate-400">Estado</dt><dd className="mt-2"><CourseStatusBadge status={course.status} /></dd></div>
            <div><dt className="text-xs font-bold uppercase tracking-wider text-slate-400">Módulos</dt><dd className="mt-2 font-bold text-foreground">{course.modules.length}</dd></div>
            <div><dt className="text-xs font-bold uppercase tracking-wider text-slate-400">Duración</dt><dd className="mt-2 font-bold text-foreground">{formatDuration(course.durationHours)}</dd></div>
            <div><dt className="text-xs font-bold uppercase tracking-wider text-slate-400">Actualizado</dt><dd className="mt-2 font-bold text-foreground">{formatDate(course.updatedAt)}</dd></div>
          </dl>
        </section>
      ) : (
        <CourseForm
          initial={course}
          onSave={(draft, submit) => {
            const updated = updateCourse(course.id, draft, submit)
            if (!updated) {
              toast.error('No tenés permiso para editar este curso.')
              return
            }
            toast.success(submit ? 'Cambios enviados. Estado: EN REVISIÓN' : 'Cambios guardados como BORRADOR')
            navigate('/teacher/courses')
          }}
        />
      )}
    </>
  )
}
