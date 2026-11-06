import type { AppState, Course, Enrollment, EnrollmentStatus, Module, ProgressRecord } from '@/types'
import { courseVersion, progressStatus, publishedCourseView, uid } from '../utils/format.ts'

export const getOperationalStatus = (course: Course) =>
  course.operationalStatus ??
    (course.status === 'PAUSADO' || course.status === 'FINALIZADO' ? course.status : 'ACTIVO')

export const getCourseDisplayStatus = (course: Course) => {
  const operational = getOperationalStatus(course)
  return operational === 'ACTIVO' ? course.status : operational
}

export const canEditWorkingCourse = (course: Course) =>
  getOperationalStatus(course) !== 'FINALIZADO' &&
  (course.status === 'BORRADOR' || course.status === 'PUBLICADO' ||
    course.status === 'CAMBIOS SOLICITADOS' || course.status === 'PAUSADO')

export const canRecoverWorkingDraft = (course: Course) =>
  course.status === 'RECHAZADO' && getOperationalStatus(course) !== 'FINALIZADO'

export const recoverWorkingDraft = (course: Course, date: string): Course | undefined =>
  canRecoverWorkingDraft(course)
    ? { ...course, ...structuredClone(courseVersion(course)), status: 'BORRADOR', updatedAt: date }
    : undefined

export const identifyWorkingModules = (course: Course, modules: Module[]) => {
  const published = new Map(getPublication(course)?.modules.map((module) => [module.id, module]))
  return modules.map((module) => {
    const previous = published.get(module.id)
    if (!previous || (module.name === previous.name && module.description === previous.description &&
      module.durationHours === previous.durationHours && module.resources.length === previous.resources.length &&
      module.resources.every((resource, index) => resource === previous.resources[index]))) return module
    return { ...module, id: uid('module') }
  })
}

export const getPublication = (course?: Course): Course | undefined => {
  if (!course) return undefined
  if (course.publishedVersion) return publishedCourseView(course)
  if (course.status === 'PUBLICADO' || course.status === 'PAUSADO' || course.status === 'FINALIZADO') return course
  return undefined
}

export const canRequestEnrollment = (course: Course | undefined): course is Course =>
  Boolean(course && getOperationalStatus(course) === 'ACTIVO' && getPublication(course))

export const getWorkingEditBase = (course: Course) => ({
  publishedVersion: course.publishedVersion ?? (getPublication(course) ? courseVersion(course) : undefined),
  operationalStatus: course.operationalStatus ??
    (course.status === 'PAUSADO' || course.status === 'FINALIZADO' ? course.status : undefined),
})

export const canTransitionEnrollment = (current: EnrollmentStatus, next: EnrollmentStatus) =>
  (current === 'PENDIENTE' && (next === 'APROBADA' || next === 'RECHAZADA')) ||
  (current === 'APROBADA' && next === 'CANCELADA')

export const getApprovedEnrollment = (state: AppState, studentId: string | undefined, courseId: string | undefined) =>
  state.enrollments.find((item) => item.studentId === studentId && item.courseId === courseId && item.status === 'APROBADA')

export const calculatePublishedProgress = (course: Course, completed: Iterable<string>) => {
  const ids = new Set(completed)
  const completedModuleIds = course.modules.filter((module) => ids.has(module.id)).map((module) => module.id)
  const count = completedModuleIds.length
  const total = course.modules.length
  const percentage = count === 0 ? 0 : count === total ? 100 : Math.min(99, Math.max(1, Math.round(count * 100 / total)))
  return { completedModuleIds, percentage, status: progressStatus(percentage) }
}

export const getEnrollmentProgress = (state: AppState, enrollment?: Enrollment): ProgressRecord | undefined => {
  if (!enrollment || enrollment.status !== 'APROBADA') return undefined
  const course = getPublication(state.courses.find((item) => item.id === enrollment.courseId))
  const record = state.progressRecords.find((item) => item.enrollmentId === enrollment.id)
  return course && record ? { ...record, ...calculatePublishedProgress(course, record.completedModuleIds) } : undefined
}

export const linkLegacyProgress = (state: AppState): ProgressRecord[] =>
  state.progressRecords.map((record) => {
    if (record.enrollmentId) return record
    const matches = state.enrollments.filter((item) =>
      item.studentId === record.studentId && item.courseId === record.courseId &&
      (item.status === 'APROBADA' || item.status === 'CANCELADA'),
    )
    return matches.length === 1 ? { ...record, enrollmentId: matches[0].id } : record
  })

export const courseService = {
  getPublishedCourses(state: AppState): Course[] {
    return state.courses
      .filter(canRequestEnrollment)
      .map((course) => ({ ...getPublication(course)!, status: 'PUBLICADO' }))
  },
  getTeacherCourses(state: AppState, teacherId: string): Course[] {
    return state.courses.filter((course) => course.teacherId === teacherId)
  },
  getApprovedStudentCourses(state: AppState, studentId: string): Course[] {
    const ids = new Set(
      state.enrollments
        .filter((item) => item.studentId === studentId && item.status === 'APROBADA')
        .map((item) => item.courseId),
    )
    return state.courses.filter((course) => ids.has(course.id)).flatMap((course) => {
      const published = getPublication(course)
      return published ? [{ ...published, status: getCourseDisplayStatus(published) }] : []
    })
  },
}
