import { motion } from 'framer-motion'
import { ArrowRight, BookOpen, CheckCircle2, Clock3, Compass, GraduationCap, Layers3 } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { CourseCard } from '@/components/CourseCard'
import {
  CourseArtwork,
  EmptyState,
  EnrollmentStatusBadge,
  LumenOrb,
  MetricCard,
  PageHeader,
  ProgressBar,
  ProgressStatusBadge,
  SearchInput,
} from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { courseService } from '@/services/courseService'
import { formatDate, formatDuration, fullName } from '@/utils/format'

export function StudentDashboard() {
  const { state, currentUser } = useApp()
  if (!currentUser) return null

  const approved = state.enrollments.filter((item) => item.studentId === currentUser.id && item.status === 'APROBADA')
  const pending = state.enrollments.filter((item) => item.studentId === currentUser.id && item.status === 'PENDIENTE')
  const progress = state.progressRecords.filter((item) => item.studentId === currentUser.id)
  const completed = progress.filter((item) => item.status === 'COMPLETADO').length
  const average = progress.length ? Math.round(progress.reduce((sum, item) => sum + item.percentage, 0) / progress.length) : 0
  const activeProgress = [...progress].filter((item) => item.status !== 'COMPLETADO').sort((a, b) => b.percentage - a.percentage)[0] ?? progress[0]
  const activeCourse = state.courses.find((item) => item.id === activeProgress?.courseId)
  const teacher = state.users.find((item) => item.id === activeCourse?.teacherId)
  const explore = courseService
    .getPublishedCourses(state)
    .filter((course) => !state.enrollments.some((item) => item.studentId === currentUser.id && item.courseId === course.id && (item.status === 'PENDIENTE' || item.status === 'APROBADA')))
    .slice(0, 3)

  return (
    <>
      <motion.section
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
        className="relative mb-2 min-h-[20rem] overflow-hidden border-b border-slate-200 pb-10 pt-3 sm:min-h-[23rem] sm:pb-12 sm:pt-7"
      >
        <LumenOrb className="-right-48 -top-72 sm:-right-24 sm:-top-64" />
        <div className="ambient-glow -right-20 -top-20 opacity-70" aria-hidden="true" />
        <div className="relative z-10 max-w-4xl">
          <p className="text-lg text-foreground-secondary">Hola, {currentUser.firstName}</p>
          <h1 className="text-balance mt-3 font-display text-5xl font-semibold leading-[0.98] tracking-[-0.055em] text-foreground sm:text-6xl xl:text-7xl">
            Seguí construyendo
            <span className="block">tu mejor <span className="text-primary">versión</span></span>
          </h1>
          <p className="mt-6 max-w-xl text-base leading-7 text-muted">El conocimiento no tiene límites. Explorá, aprendé y llevá tus ideas más lejos con Lumen.</p>
          <Link to="/student/explore" className="btn-primary mt-7"><Compass size={17} /> Explorar cursos</Link>
        </div>
        <div className="absolute bottom-12 right-8 hidden space-y-4 text-xs font-semibold uppercase tracking-[0.55em] text-foreground-secondary xl:block">
          <p>Aprender</p>
          <p>Conectar</p>
          <p>Crecer</p>
          <div className="section-line mt-6 w-36" />
        </div>
      </motion.section>

      <section className="grid border-b border-slate-200 py-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Cursos activos" value={approved.length} icon={BookOpen} tone="primary" />
        <MetricCard label="Solicitudes pendientes" value={pending.length} icon={Clock3} tone="amber" />
        <MetricCard label="Cursos completados" value={completed} icon={CheckCircle2} tone="emerald" />
        <MetricCard label="Progreso promedio" value={average + ' %'} icon={GraduationCap} tone="neutral" />
      </section>

      <section className="mt-12">
        <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="eyebrow">Tu recorrido</p>
            <h2 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-foreground">Continuá aprendiendo</h2>
          </div>
          <Link to="/student/my-courses" className="inline-flex items-center gap-2 text-sm font-bold text-primary hover:text-orange-300">
            Ver todos mis cursos <ArrowRight size={16} />
          </Link>
        </div>

        <div className="grid gap-6 xl:grid-cols-[1.45fr_.55fr]">
          {activeCourse && activeProgress ? (
            <motion.article
              initial={{ opacity: 0, y: 14 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.08, duration: 0.45 }}
              className="group grid overflow-hidden border border-slate-300 bg-surface md:grid-cols-[minmax(220px,.72fr)_1.28fr]"
            >
              <CourseArtwork courseId={activeCourse.id} category={activeCourse.category} className="min-h-56 border-b border-slate-300 md:min-h-full md:border-b-0 md:border-r" />
              <div className="flex flex-col justify-center p-6 sm:p-8">
                <div className="flex flex-wrap items-center gap-3">
                  <p className="eyebrow">{activeCourse.category}</p>
                  <ProgressStatusBadge status={activeProgress.status} />
                </div>
                <h3 className="mt-3 text-3xl font-semibold leading-tight tracking-[-0.035em] text-foreground">{activeCourse.title}</h3>
                <p className="mt-2 text-sm text-muted">Prof. {fullName(teacher)}</p>
                <div className="mt-7 flex items-center justify-between gap-4">
                  <span className="text-sm text-muted">{activeProgress.completedModuleIds.length} de {activeCourse.modules.length} módulos</span>
                  <span className="text-lg font-semibold text-primary">{activeProgress.percentage} %</span>
                </div>
                <ProgressBar value={activeProgress.percentage} className="mt-3" />
                <div className="mt-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="flex flex-wrap gap-4 text-xs text-muted-dark">
                    <span className="inline-flex items-center gap-1.5"><Layers3 size={14} /> {activeCourse.modules.length} módulos</span>
                    <span className="inline-flex items-center gap-1.5"><Clock3 size={14} /> {formatDuration(activeCourse.durationHours)}</span>
                  </div>
                  <Link to={'/student/learn/' + activeCourse.id} className="btn-primary">
                    Continuar <ArrowRight className="transition-transform group-hover:translate-x-1" size={17} />
                  </Link>
                </div>
              </div>
            </motion.article>
          ) : (
            <EmptyState title="Todavía no tenés cursos activos" description="Explorá el catálogo y enviá una solicitud de inscripción." action={<Link to="/student/explore" className="btn-primary">Explorar cursos</Link>} />
          )}

          <article className="border-t border-slate-300 pt-5 xl:border-l xl:border-t-0 xl:pl-6 xl:pt-0">
            <div className="flex items-center justify-between">
              <div>
                <p className="eyebrow">Inscripciones</p>
                <h2 className="mt-2 text-xl font-semibold text-foreground">Estado reciente</h2>
              </div>
              <Clock3 size={20} className="text-primary" />
            </div>
            <div className="mt-5 divide-y divide-border-subtle">
              {state.enrollments.filter((item) => item.studentId === currentUser.id).slice(0, 3).map((enrollment) => {
                const course = state.courses.find((item) => item.id === enrollment.courseId)
                return (
                  <div key={enrollment.id} className="py-4 first:pt-0">
                    <p className="truncate text-sm font-bold text-foreground">{course?.title}</p>
                    <div className="mt-2 flex items-center justify-between gap-2">
                      <EnrollmentStatusBadge status={enrollment.status} />
                      <span className="text-[11px] text-muted-dark">{formatDate(enrollment.requestedAt)}</span>
                    </div>
                  </div>
                )
              })}
            </div>
            <Link to="/student/requests" className="btn-ghost mt-3 w-full">Ver todas <ArrowRight size={16} /></Link>
          </article>
        </div>
      </section>

      <section className="mt-14 border-t border-slate-200 pt-9">
        <div className="mb-6 flex items-end justify-between gap-4">
          <div>
            <p className="eyebrow">Nuevos recorridos</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-[-0.035em] text-foreground">Explorar nuevos cursos</h2>
          </div>
          <Link to="/student/explore" className="text-sm font-bold text-primary hover:text-orange-300">Ver catálogo →</Link>
        </div>
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {explore.map((course) => <CourseCard key={course.id} course={course} href={'/student/courses/' + course.id} actionLabel="Ver detalle" />)}
        </div>
      </section>
    </>
  )
}

export function StudentCatalog() {
  const { state } = useApp()
  const [searchParams] = useSearchParams()
  const [query, setQuery] = useState(searchParams.get('q') ?? '')
  const [category, setCategory] = useState('Todas')
  const [level, setLevel] = useState('Todos')
  const [modality, setModality] = useState('Todas')
  const courses = courseService.getPublishedCourses(state)
  const categories = ['Todas', ...Array.from(new Set(courses.map((item) => item.category)))]
  const levels = ['Todos', ...Array.from(new Set(courses.map((item) => item.level)))]
  const modalities = ['Todas', ...Array.from(new Set(courses.map((item) => item.modality)))]

  const filtered = useMemo(
    () =>
      courses.filter((course) => {
        const teacher = state.users.find((user) => user.id === course.teacherId)
        const text = (course.title + ' ' + course.category + ' ' + fullName(teacher)).toLowerCase()
        return (
          text.includes(query.toLowerCase()) &&
          (category === 'Todas' || course.category === category) &&
          (level === 'Todos' || course.level === level) &&
          (modality === 'Todas' || course.modality === modality)
        )
      }),
    [category, courses, level, modality, query, state.users],
  )

  return (
    <>
      <PageHeader eyebrow="Catálogo" title="Explorar cursos" description="Todos los cursos que ves están PUBLICADOS y disponibles para solicitar inscripción." />
      <section className="mb-7 border-y border-slate-300 bg-background-secondary/60 py-5">
        <div className="grid gap-3 lg:grid-cols-[1.5fr_repeat(3,.65fr)]">
          <SearchInput value={query} onChange={setQuery} placeholder="Buscar por título, categoría o profesor" />
          <select className="select" value={category} onChange={(event) => setCategory(event.target.value)} aria-label="Filtrar por categoría">
            {categories.map((item) => <option key={item}>{item}</option>)}
          </select>
          <select className="select" value={level} onChange={(event) => setLevel(event.target.value)} aria-label="Filtrar por nivel">
            {levels.map((item) => <option key={item}>{item}</option>)}
          </select>
          <select className="select" value={modality} onChange={(event) => setModality(event.target.value)} aria-label="Filtrar por modalidad">
            {modalities.map((item) => <option key={item}>{item}</option>)}
          </select>
        </div>
      </section>
      <p className="mb-5 text-sm font-semibold text-muted">{filtered.length} {filtered.length === 1 ? 'curso encontrado' : 'cursos encontrados'}</p>
      {filtered.length ? (
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {filtered.map((course) => <CourseCard key={course.id} course={course} href={'/student/courses/' + course.id} actionLabel="Ver detalle" />)}
        </div>
      ) : (
        <EmptyState />
      )}
    </>
  )
}

