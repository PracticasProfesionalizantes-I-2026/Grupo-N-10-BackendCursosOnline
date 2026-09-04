import type { Course, CourseVersion, ProgressStatus, User } from '@/types'

export const uid = (prefix: string) =>
  prefix + '-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 7)

export const fullName = (user?: User) =>
  user ? user.firstName + ' ' + user.lastName : 'Usuario'

export const initials = (user?: User) =>
  user ? (user.firstName[0] + user.lastName[0]).toUpperCase() : 'LU'

export const formatDate = (date?: string) =>
  date
    ? new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(date))
    : '—'

export const formatShortDate = (date?: string) =>
  date
    ? new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short' }).format(new Date(date))
    : '—'

export const formatDuration = (hours: number) => {
  if (hours < 1) return Math.round(hours * 60) + ' min'
  const whole = Math.floor(hours)
  const minutes = Math.round((hours - whole) * 60)
  return minutes ? whole + ' h ' + minutes + ' min' : whole + ' h'
}

export const progressStatus = (percentage: number): ProgressStatus => {
  if (percentage <= 0) return 'NO INICIADO'
  if (percentage >= 100) return 'COMPLETADO'
  return 'EN PROGRESO'
}

export const courseVersion = (course: Course): CourseVersion => ({
  title: course.title,
  description: course.description,
  category: course.category,
  level: course.level,
  modality: course.modality,
  maxCapacity: course.maxCapacity,
  objectives: course.objectives,
  prerequisites: course.prerequisites,
  modules: course.modules,
  durationHours: course.durationHours,
})

export const publishedCourseView = (course: Course): Course => {
  if (!course.publishedVersion) return course
  return { ...course, ...course.publishedVersion, status: 'PUBLICADO' }
}

export const dateInputValue = (date: Date) => date.toISOString().slice(0, 10)
