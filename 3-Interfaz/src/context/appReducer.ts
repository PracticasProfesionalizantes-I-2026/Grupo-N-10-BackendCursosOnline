import type { AppAction, AppState, AuditDecision, CourseStatus } from '@/types'
import { canTransitionEnrollment } from '../services/courseService.ts'
import { progressStatus, uid } from '../utils/format.ts'

export const appReducer = (state: AppState, action: AppAction): AppState => {
  switch (action.type) {
    case 'LOGIN':
      return { ...state, currentUserId: action.userId }
    case 'LOGOUT':
      return { ...state, currentUserId: null }
    case 'REGISTER':
      return { ...state, users: [...state.users, action.user], currentUserId: action.user.id }
    case 'CREATE_COURSE':
      return {
        ...state,
        courses: [action.course, ...state.courses],
        audits: action.audit ? [action.audit, ...state.audits] : state.audits,
        recentActivity: [action.activity, ...state.recentActivity],
      }
    case 'UPDATE_COURSE':
      return {
        ...state,
        courses: state.courses.map((course) => (course.id === action.course.id ? action.course : course)),
        audits: action.audit ? [action.audit, ...state.audits] : state.audits,
        recentActivity: [action.activity, ...state.recentActivity],
      }
    case 'RESOLVE_AUDIT': {
      const audit = state.audits.find((item) => item.id === action.auditId)
      const course = state.courses.find((item) => item.id === audit?.courseId)
      if (audit?.status !== 'PENDIENTE' || course?.status !== 'EN REVISIÓN' ||
        (action.decision === 'CAMBIOS SOLICITADOS' && !action.observations?.trim())) return state
      const statusByDecision: Record<AuditDecision, CourseStatus> = {
        APROBADA: 'PUBLICADO',
        'CAMBIOS SOLICITADOS': 'CAMBIOS SOLICITADOS',
        RECHAZADA: 'RECHAZADO',
      }
      const nextCourse = {
        ...course,
        status: statusByDecision[action.decision],
        adminObservation: action.observations,
        publishedAt: action.decision === 'APROBADA' ? action.date : course.publishedAt,
        publishedVersion: action.decision === 'APROBADA' ? undefined : course.publishedVersion,
        updatedAt: action.date,
      }
      return {
        ...state,
        courses: state.courses.map((item) => (item.id === nextCourse.id ? nextCourse : item)),
        audits: state.audits.map((item) =>
          item.id === action.auditId
            ? { ...item, status: 'RESUELTA', decision: action.decision, observations: action.observations, resolvedAt: action.date }
            : item,
        ),
        notifications: [
          {
            id: uid('not'),
            userId: audit.teacherId,
            title: action.decision === 'APROBADA' ? 'Curso aprobado' : action.decision === 'RECHAZADA' ? 'Curso rechazado' : 'Cambios solicitados',
            body: course.title + ' tiene una nueva decisión de auditoría.',
            createdAt: action.date,
            read: false,
          },
          ...state.notifications,
        ],
        recentActivity: [
          { id: uid('act'), audience: 'ADMINISTRADOR', message: course.title + ' pasó a ' + statusByDecision[action.decision] + '.', createdAt: action.date },
          ...state.recentActivity,
        ],
      }
    }
    case 'REQUEST_ENROLLMENT':
      return {
        ...state,
        enrollments: [action.enrollment, ...state.enrollments],
        notifications: [
          {
            id: uid('not'),
            userId: action.enrollment.studentId,
            title: 'Solicitud enviada',
            body: 'Tu solicitud quedó PENDIENTE y espera revisión administrativa.',
            createdAt: action.enrollment.requestedAt,
            read: false,
          },
          ...state.notifications,
        ],
      }
    case 'UPDATE_ENROLLMENT': {
      const enrollment = state.enrollments.find((item) => item.id === action.enrollmentId)
      if (!enrollment || !canTransitionEnrollment(enrollment.status, action.status)) return state
      const updated = { ...enrollment, status: action.status, resolvedAt: action.date }
      const hasProgress = state.progressRecords.some(
        (item) => item.enrollmentId === enrollment.id,
      )
      const newProgress =
        action.status === 'APROBADA' && !hasProgress
          ? {
              id: uid('progress'),
              enrollmentId: enrollment.id,
              studentId: enrollment.studentId,
              courseId: enrollment.courseId,
              completedModuleIds: [],
              percentage: 0,
              status: progressStatus(0),
              lastActivityAt: action.date,
            }
          : undefined
      return {
        ...state,
        enrollments: state.enrollments.map((item) => (item.id === updated.id ? updated : item)),
        progressRecords: newProgress ? [newProgress, ...state.progressRecords] : state.progressRecords,
        notifications: [
          {
            id: uid('not'),
            userId: enrollment.studentId,
            title: 'Inscripción ' + action.status.toLowerCase(),
            body:
              action.status === 'APROBADA'
                ? 'Ya podés acceder al curso desde Mis cursos.'
                : action.status === 'CANCELADA'
                  ? 'El acceso al curso fue retirado.'
                  : 'Consultá el detalle en Mis solicitudes.',
            createdAt: action.date,
            read: false,
          },
          ...state.notifications,
        ],
      }
    }
    case 'UPDATE_PROGRESS':
      return {
        ...state,
        progressRecords: state.progressRecords.some((item) => item.id === action.progress.id)
          ? state.progressRecords.map((item) => (item.id === action.progress.id ? action.progress : item))
          : [action.progress, ...state.progressRecords],
        recentActivity: [
          {
            id: uid('act'),
            audience: 'PROFESOR',
            message: 'Un alumno actualizó su progreso al ' + action.progress.percentage + ' %.',
            createdAt: action.progress.lastActivityAt,
          },
          ...state.recentActivity,
        ],
      }
    case 'SET_COURSE_STATUS':
      return {
        ...state,
        courses: state.courses.map((item) => {
          if (item.id !== action.courseId) return item
          if (action.status !== 'PUBLICADO' && action.status !== 'PAUSADO' && action.status !== 'FINALIZADO') return item
          const hasWorkingStatus = item.status !== 'PUBLICADO' && item.status !== 'PAUSADO' && item.status !== 'FINALIZADO'
          return {
            ...item,
            status: hasWorkingStatus ? item.status : action.status,
            operationalStatus: action.status === 'PUBLICADO' ? undefined : action.status,
            updatedAt: action.date,
          }
        }),
      }
    case 'TOGGLE_USER_STATUS':
      return {
        ...state,
        users: state.users.map((item) => (item.id === action.userId ? { ...item, active: !item.active } : item)),
      }
    case 'MARK_NOTIFICATIONS_READ':
      return {
        ...state,
        notifications: state.notifications.map((item) =>
          item.userId === action.userId ? { ...item, read: true } : item,
        ),
      }
    case 'RESET':
      return action.state
    default:
      return state
  }
}
