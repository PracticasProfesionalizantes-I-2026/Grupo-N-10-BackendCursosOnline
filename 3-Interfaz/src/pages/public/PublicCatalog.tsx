import { ArrowLeft, Compass } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { CourseCard } from '@/components/CourseCard'
import { EmptyState, Logo, SearchInput } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { courseService } from '@/services/courseService'
import { fullName } from '@/utils/format'

export function PublicCatalog() {
  const { state } = useApp()
  const [query, setQuery] = useState('')
  const [category, setCategory] = useState('Todas')
  const courses = courseService.getPublishedCourses(state)
  const categories = ['Todas', ...Array.from(new Set(courses.map((course) => course.category)))]
  const filtered = useMemo(
    () =>
      courses.filter((course) => {
        const teacher = state.users.find((user) => user.id === course.teacherId)
        const haystack = (course.title + ' ' + course.category + ' ' + fullName(teacher)).toLowerCase()
        return haystack.includes(query.toLowerCase()) && (category === 'Todas' || course.category === category)
      }),
    [category, courses, query, state.users],
  )

  return (
    <main className="min-h-screen bg-canvas">
      <header className="border-b border-slate-200 bg-surface">
        <div className="mx-auto flex h-20 max-w-7xl items-center px-5 sm:px-8">
          <Logo />
          <div className="ml-auto flex gap-2">
            <Link to="/" className="btn-ghost"><ArrowLeft size={16} /> Inicio</Link>
            <Link to="/login" className="btn-primary">Iniciar sesión</Link>
          </div>
        </div>
      </header>
      <section className="page-grid border-b border-slate-200 bg-surface px-5 py-16 sm:px-8">
        <div className="mx-auto max-w-7xl">
          <p className="eyebrow">Catálogo público</p>
          <h1 className="mt-3 font-display text-5xl font-semibold tracking-tight text-foreground sm:text-6xl">Explorá cursos publicados.</h1>
          <p className="mt-4 max-w-2xl text-base leading-7 text-slate-600">Encontrá un recorrido por título, categoría o profesor. Iniciá sesión como Alumno para solicitar tu inscripción.</p>
        </div>
      </section>
      <section className="mx-auto max-w-7xl px-5 py-10 sm:px-8">
        <div className="mb-7 flex flex-col gap-3 border-y border-slate-200 bg-background-secondary/40 py-5 sm:flex-row">
          <SearchInput value={query} onChange={setQuery} placeholder="Buscar por curso, categoría o profesor" className="flex-1" />
          <label>
            <span className="sr-only">Categoría</span>
            <select className="select min-w-48" value={category} onChange={(event) => setCategory(event.target.value)}>
              {categories.map((item) => <option key={item}>{item}</option>)}
            </select>
          </label>
        </div>
        <div className="mb-5 flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-600">{filtered.length} cursos disponibles</p>
          <Compass size={20} className="text-primary" />
        </div>
        {filtered.length ? (
          <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
            {filtered.map((course) => <CourseCard key={course.id} course={course} href="/login" actionLabel="Ingresar para inscribirme" />)}
          </div>
        ) : (
          <EmptyState />
        )}
      </section>
    </main>
  )
}
