import { BookOpen, Edit3, Eye, PauseCircle, PlayCircle, Power, StopCircle, UserRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import type { CourseStatus, Role } from '@/types'
import { CourseStatusBadge, EmptyState, Modal, PageHeader, SearchInput } from '@/components/UI'
import { useApp } from '@/hooks/useApp'
import { canEditWorkingCourse, canRecoverWorkingDraft, getCourseDisplayStatus } from '@/services/courseService'
import { formatDate, formatDuration, fullName } from '@/utils/format'

const roleLabel: Record<Role, string> = {
  ALUMNO: 'Alumno',
  PROFESOR: 'Profesor',
  ADMINISTRADOR: 'Administrador',
}

export function AdminUsers() {
  const { state, currentUser, toggleUserStatus } = useApp()
  const [query, setQuery] = useState('')
  const [role, setRole] = useState('TODOS')
  const rows = useMemo(
    () =>
      state.users.filter((user) => {
        const text = (fullName(user) + ' ' + user.email).toLowerCase()
        return text.includes(query.toLowerCase()) && (role === 'TODOS' || user.role === role)
      }),
    [query, role, state.users],
  )

  return (
    <>
      <PageHeader eyebrow="Gestión del sistema" title="Usuarios" description="Consultá cuentas registradas y su estado. El Administrador no dispone de registro público." />
      <section className="mb-7 grid gap-3 border-y border-slate-200 bg-background-secondary/40 py-5 sm:grid-cols-[1fr_260px]">
        <SearchInput value={query} onChange={setQuery} placeholder="Buscar por nombre o email" />
        <select className="select" value={role} onChange={(event) => setRole(event.target.value)} aria-label="Filtrar por rol">
          <option value="TODOS">Todos</option>
          <option value="ALUMNO">Alumnos</option>
          <option value="PROFESOR">Profesores</option>
          <option value="ADMINISTRADOR">Administradores</option>
        </select>
      </section>
      {rows.length ? (
        <div className="table-wrap bg-surface">
          <table className="data-table">
            <thead><tr><th>Nombre</th><th>Email</th><th>Rol</th><th>Fecha de registro</th><th>Estado</th><th>Acción</th></tr></thead>
            <tbody>
              {rows.map((user) => (
                <tr key={user.id}>
                  <td>
                    <div className="flex items-center gap-3">
                      <span className="grid size-9 place-items-center rounded-xl bg-primary-soft text-primary"><UserRound size={17} /></span>
                      <span className="font-bold text-slate-800">{fullName(user)}</span>
                    </div>
                  </td>
                  <td>{user.email}</td>
                  <td><span className="rounded-full bg-surface-raised px-2.5 py-1 text-xs font-bold text-slate-600">{roleLabel[user.role]}</span></td>
                  <td>{formatDate(user.createdAt)}</td>
                  <td>
                    <span className={'inline-flex items-center gap-2 text-sm font-bold ' + (user.active ? 'text-emerald-300' : 'text-slate-400')}>
                      <span className="size-2 rounded-full bg-current" /> {user.active ? 'Habilitado' : 'Deshabilitado'}
                    </span>
                  </td>
                  <td>
                    <button
                      className="btn-ghost min-h-10 px-3"
                      disabled={user.id === currentUser?.id}
                      onClick={() => {
                        toggleUserStatus(user.id)
                        toast.success(user.active ? 'Cuenta deshabilitada.' : 'Cuenta habilitada.')
                      }}
                    >
                      <Power size={16} /> {user.active ? 'Deshabilitar' : 'Habilitar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <EmptyState />
      )}
    </>
  )
}

export function AdminCourses() {
  const { state, setCourseStatus } = useApp()
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('TODOS')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const filtered = useMemo(
    () =>
      state.courses.filter((course) => {
        const teacher = state.users.find((item) => item.id === course.teacherId)
        const text = (course.title + ' ' + fullName(teacher)).toLowerCase()
        return text.includes(query.toLowerCase()) && (status === 'TODOS' || getCourseDisplayStatus(course) === status)
      }),
    [query, state.courses, state.users, status],
  )
  const selected = state.courses.find((item) => item.id === selectedId)

  const changeStatus = (courseId: string, next: CourseStatus) => {
    setCourseStatus(courseId, next)
    toast.success('Estado actualizado a ' + next + '.')
  }

  return (
    <>
      <PageHeader eyebrow="Gestión del sistema" title="Cursos" description="Consultá y administrá cursos respetando las transiciones definidas." />
      <section className="mb-7 grid gap-3 border-y border-slate-200 bg-background-secondary/40 py-5 sm:grid-cols-[1fr_280px]">
        <SearchInput value={query} onChange={setQuery} placeholder="Buscar por curso o profesor" />
        <select className="select" value={status} onChange={(event) => setStatus(event.target.value)} aria-label="Filtrar por estado">
          {['TODOS', 'BORRADOR', 'EN REVISIÓN', 'CAMBIOS SOLICITADOS', 'RECHAZADO', 'PUBLICADO', 'PAUSADO', 'FINALIZADO'].map((item) => <option key={item}>{item === 'TODOS' ? 'Todos los estados' : item}</option>)}
        </select>
      </section>
      {filtered.length ? (
        <div className="table-wrap bg-surface">
          <table className="data-table">
            <thead><tr><th>Curso</th><th>Profesor</th><th>Estado</th><th>Alumnos</th><th>Fecha</th><th>Acciones</th></tr></thead>
            <tbody>
              {filtered.map((course) => {
                const displayStatus = getCourseDisplayStatus(course)
                const teacher = state.users.find((item) => item.id === course.teacherId)
                const students = state.enrollments.filter((item) => item.courseId === course.id && item.status === 'APROBADA').length
                return (
                  <tr key={course.id}>
                    <td>
                      <div className="flex items-center gap-3">
                        <span className="grid size-9 place-items-center rounded-xl bg-primary-soft text-primary"><BookOpen size={17} /></span>
                        <span className="font-bold text-slate-800">{course.title}</span>
                      </div>
                    </td>
                    <td>{fullName(teacher)}</td>
                    <td><CourseStatusBadge status={displayStatus} /></td>
                    <td>{students}</td>
                    <td>{formatDate(course.createdAt)}</td>
                    <td>
                      <div className="flex flex-wrap gap-1">
                        <button className="btn-ghost min-h-9 px-2.5 text-xs" onClick={() => setSelectedId(course.id)}><Eye size={15} /> Consultar</button>
                        {canEditWorkingCourse(course) && <Link className="btn-ghost min-h-9 px-2.5 text-xs" to={course.id + '/edit'}><Edit3 size={15} /> Editar</Link>}
                        {canRecoverWorkingDraft(course) && <Link className="btn-ghost min-h-9 px-2.5 text-xs" to={course.id + '/edit'}>Recuperar borrador</Link>}
                        {displayStatus === 'PUBLICADO' && <button className="btn-ghost min-h-9 px-2.5 text-xs text-amber-300" onClick={() => changeStatus(course.id, 'PAUSADO')}><PauseCircle size={15} /> Pausar</button>}
                        {displayStatus === 'PAUSADO' && <button className="btn-ghost min-h-9 px-2.5 text-xs text-emerald-300" onClick={() => changeStatus(course.id, 'PUBLICADO')}><PlayCircle size={15} /> Reactivar</button>}
                        {(displayStatus === 'PUBLICADO' || displayStatus === 'PAUSADO') && <button className="btn-ghost min-h-9 px-2.5 text-xs text-red-400" onClick={() => changeStatus(course.id, 'FINALIZADO')}><StopCircle size={15} /> Finalizar</button>}
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
        open={Boolean(selected)}
        onClose={() => setSelectedId(null)}
        title={selected?.title ?? 'Detalle del curso'}
        description={fullName(state.users.find((item) => item.id === selected?.teacherId))}
        size="lg"
        footer={<button className="btn-secondary" onClick={() => setSelectedId(null)}>Cerrar</button>}
      >
        {selected && (
          <div className="space-y-5">
            <div className="grid gap-3 sm:grid-cols-4">
              <div className="rounded-xl bg-background-secondary p-3"><p className="text-xs font-bold text-slate-400">Estado</p><div className="mt-2"><CourseStatusBadge status={getCourseDisplayStatus(selected)} /></div></div>
              <div className="rounded-xl bg-background-secondary p-3"><p className="text-xs font-bold text-slate-400">Módulos</p><p className="mt-2 font-bold text-foreground">{selected.modules.length}</p></div>
              <div className="rounded-xl bg-background-secondary p-3"><p className="text-xs font-bold text-slate-400">Duración</p><p className="mt-2 font-bold text-foreground">{formatDuration(selected.durationHours)}</p></div>
              <div className="rounded-xl bg-background-secondary p-3"><p className="text-xs font-bold text-slate-400">Cupo</p><p className="mt-2 font-bold text-foreground">{selected.maxCapacity}</p></div>
            </div>
            <p className="text-sm leading-6 text-slate-600">{selected.description}</p>
            <div>
              <h3 className="font-bold text-foreground">Módulos</h3>
              <div className="mt-3 space-y-2">
                {selected.modules.map((module, index) => (
                  <div key={module.id} className="flex items-center gap-3 rounded-xl border border-slate-200 p-3">
                    <span className="grid size-8 place-items-center rounded-lg bg-primary-soft text-xs font-bold text-primary">{index + 1}</span>
                    <p className="flex-1 text-sm font-semibold text-slate-700">{module.name}</p>
                    <span className="text-xs text-slate-400">{formatDuration(module.durationHours)}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </Modal>
    </>
  )
}
