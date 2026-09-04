import { createContext, useEffect, useMemo, useReducer, type ReactNode } from 'react'
import { createDemoState, DEMO_STATE_KEY } from '@/data/seed'
import type { AppAction, AppState, AuditDecision, Course, CourseDraft, CourseStatus, EnrollmentStatus, Role, User } from '@/types'
import { courseVersion, progressStatus, uid } from '@/utils/format'

const loadState = (): AppState => {
  try {
    const saved = localStorage.getItem(DEMO_STATE_KEY)
    return saved ? (JSON.parse(saved) as AppState) : createDemoState()
  } catch {
    return createDemoState()
  }
}

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
      if (!audit) return state
      const statusByDecision: Record<AuditDecision, CourseStatus> = {
        APROBADA: 'PUBLICADO',
        'CAMBIOS SOLICITADOS': 'CAMBIOS SOLICITADOS',
        RECHAZADA: 'RECHAZADO',
      }
      const course = state.courses.find((item) => item.id === audit.courseId)
      const nextCourse = course
        ? {
            ...course,
            status: statusByDecision[action.decision],
            adminObservation: action.observations,
            publishedAt: action.decision === 'APROBADA' ? action.date : course.publishedAt,
            publishedVersion: action.decision === 'APROBADA' ? undefined : course.publishedVersion,
            updatedAt: action.date,
          }
        : undefined
      return {
        ...state,
        courses: nextCourse ? state.courses.map((item) => (item.id === nextCourse.id ? nextCourse : item)) : state.courses,
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
            body: (course?.title ?? 'Tu curso') + ' tiene una nueva decisión de auditoría.',
            createdAt: action.date,
            read: false,
          },
          ...state.notifications,
        ],
        recentActivity: [
          { id: uid('act'), audience: 'ADMINISTRADOR', message: (course?.title ?? 'El curso') + ' pasó a ' + statusByDecision[action.decision] + '.', createdAt: action.date },
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
      if (!enrollment) return state
      const updated = { ...enrollment, status: action.status, resolvedAt: action.date }
      const hasProgress = state.progressRecords.some(
        (item) => item.courseId === enrollment.courseId && item.studentId === enrollment.studentId,
      )
      const newProgress =
        action.status === 'APROBADA' && !hasProgress
          ? {
              id: uid('progress'),
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
        courses: state.courses.map((item) =>
          item.id === action.courseId ? { ...item, status: action.status, updatedAt: action.date } : item,
        ),
      }
    case 'ADMIN_EDIT_COURSE':
      return {
        ...state,
        courses: state.courses.map((item) =>
          item.id === action.courseId
            ? { ...item, title: action.title, description: action.description, maxCapacity: action.maxCapacity, updatedAt: action.date }
            : item,
        ),
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

interface RegisterPayload {
  role: Extract<Role, 'ALUMNO' | 'PROFESOR'>
  email: string
  password: string
  firstName: string
  lastName: string
  dni: string
  phone: string
  address: string
  postalCode: string
}

interface AppContextValue {
  state: AppState
  currentUser?: User
  login: (email: string, password: string) => User | undefined
  loginAs: (role: Role) => User
  logout: () => void
  register: (payload: RegisterPayload) => User
  createCourse: (draft: CourseDraft, submit: boolean) => Course
  updateCourse: (courseId: string, draft: CourseDraft, submit: boolean) => Course | undefined
  resolveAudit: (auditId: string, decision: AuditDecision, observations?: string) => void
  requestEnrollment: (courseId: string) => boolean
  updateEnrollment: (enrollmentId: string, status: EnrollmentStatus) => void
  toggleModule: (courseId: string, moduleId: string) => void
  setCourseStatus: (courseId: string, status: CourseStatus) => void
  adminEditCourse: (courseId: string, values: { title: string; description: string; maxCapacity: number }) => void
  toggleUserStatus: (userId: string) => void
  markNotificationsRead: () => void
  resetDemoData: () => void
}

export const AppContext = createContext<AppContextValue | undefined>(undefined)

export function AppProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(appReducer, undefined, loadState)
  const currentUser = state.users.find((user) => user.id === state.currentUserId)

  useEffect(() => {
    localStorage.setItem(DEMO_STATE_KEY, JSON.stringify(state))
  }, [state])

  useEffect(() => {
    const webDocument = document as Document & {
      modelContext?: {
        registerTool: (
          tool: {
            name: string
            title: string
            description: string
            inputSchema: object
            annotations: { readOnlyHint: boolean; untrustedContentHint: boolean }
            execute: () => object
          },
          options?: { signal?: AbortSignal },
        ) => void | Promise<void>
      }
    }
    if (!webDocument.modelContext?.registerTool) return
    const lifecycle = new AbortController()
    try {
      void Promise.resolve(
        webDocument.modelContext.registerTool(
          {
            name: 'reset_lumen_demo_data',
            title: 'Restablecer demostración de Lumen',
            description: 'Restablece todos los datos locales de la demostración de Lumen a su estado inicial y cierra la sesión actual.',
            inputSchema: { type: 'object', properties: {}, additionalProperties: false },
            annotations: { readOnlyHint: false, untrustedContentHint: false },
            execute: () => {
              const fresh = createDemoState()
              dispatch({ type: 'RESET', state: fresh })
              return {
                status: 'restored',
                users: fresh.users.length,
                courses: fresh.courses.length,
                enrollments: fresh.enrollments.length,
              }
            },
          },
          { signal: lifecycle.signal },
        ),
      ).catch(() => undefined)
    } catch {
      return
    }
    return () => lifecycle.abort()
  }, [])

  const value = useMemo<AppContextValue>(() => {
    const now = () => new Date().toISOString()
    return {
      state,
      currentUser,
      login: (email, password) => {
        const user = state.users.find(
          (item) => item.email.toLowerCase() === email.trim().toLowerCase() && item.password === password && item.active,
        )
        if (user) dispatch({ type: 'LOGIN', userId: user.id })
        return user
      },
      loginAs: (role) => {
        const emailByRole: Record<Role, string> = {
          ALUMNO: 'alumno@lumen.demo',
          PROFESOR: 'profesor@lumen.demo',
          ADMINISTRADOR: 'admin@lumen.demo',
        }
        const user = state.users.find((item) => item.email === emailByRole[role])!
        dispatch({ type: 'LOGIN', userId: user.id })
        return user
      },
      logout: () => dispatch({ type: 'LOGOUT' }),
      register: (payload) => {
        const user: User = {
          ...payload,
          id: uid('user'),
          createdAt: now(),
          active: true,
        }
        dispatch({ type: 'REGISTER', user })
        return user
      },
      createCourse: (draft, submit) => {
        if (!currentUser || currentUser.role !== 'PROFESOR') throw new Error('Acción no permitida')
        const date = now()
        const course: Course = {
          ...draft,
          id: uid('course'),
          teacherId: currentUser.id,
          status: submit ? 'EN REVISIÓN' : 'BORRADOR',
          createdAt: date,
          updatedAt: date,
        }
        const audit = submit
          ? { id: uid('audit'), courseId: course.id, teacherId: currentUser.id, type: 'Creación' as const, status: 'PENDIENTE' as const, createdAt: date }
          : undefined
        dispatch({
          type: 'CREATE_COURSE',
          course,
          audit,
          activity: {
            id: uid('act'),
            audience: 'PROFESOR',
            userId: currentUser.id,
            message: submit ? course.title + ' fue enviado a revisión.' : course.title + ' se guardó como borrador.',
            createdAt: date,
          },
        })
        return course
      },
      updateCourse: (courseId, draft, submit) => {
        if (!currentUser) return undefined
        const original = state.courses.find((item) => item.id === courseId)
        const ownsCourse = original?.teacherId === currentUser.id
        if (!original || (currentUser.role === 'PROFESOR' && !ownsCourse) || original.status === 'FINALIZADO') return undefined
        const date = now()
        const preservePublished = original.status === 'PUBLICADO' ? courseVersion(original) : original.publishedVersion
        const updated: Course = {
          ...original,
          ...draft,
          status: submit ? 'EN REVISIÓN' : 'BORRADOR',
          publishedVersion: preservePublished,
          adminObservation: submit ? undefined : original.adminObservation,
          updatedAt: date,
        }
        const audit = submit
          ? {
              id: uid('audit'),
              courseId: original.id,
              teacherId: original.teacherId,
              type: original.publishedAt || preservePublished ? ('Modificación' as const) : ('Creación' as const),
              status: 'PENDIENTE' as const,
              createdAt: date,
            }
          : undefined
        dispatch({
          type: 'UPDATE_COURSE',
          course: updated,
          audit,
          activity: {
            id: uid('act'),
            audience: currentUser.role,
            userId: currentUser.id,
            message: submit ? updated.title + ' volvió a revisión.' : 'Se guardaron los cambios de ' + updated.title + '.',
            createdAt: date,
          },
        })
        return updated
      },
      resolveAudit: (auditId, decision, observations) =>
        dispatch({ type: 'RESOLVE_AUDIT', auditId, decision, observations, date: now() }),
      requestEnrollment: (courseId) => {
        if (!currentUser || currentUser.role !== 'ALUMNO') return false
        const course = state.courses.find((item) => item.id === courseId)
        const duplicate = state.enrollments.some(
          (item) =>
            item.courseId === courseId &&
            item.studentId === currentUser.id &&
            (item.status === 'PENDIENTE' || item.status === 'APROBADA'),
        )
        if (!course || course.status !== 'PUBLICADO' || duplicate) return false
        dispatch({
          type: 'REQUEST_ENROLLMENT',
          enrollment: {
            id: uid('enrollment'),
            studentId: currentUser.id,
            courseId,
            status: 'PENDIENTE',
            requestedAt: now(),
          },
        })
        return true
      },
      updateEnrollment: (enrollmentId, status) =>
        dispatch({ type: 'UPDATE_ENROLLMENT', enrollmentId, status, date: now() }),
      toggleModule: (courseId, moduleId) => {
        if (!currentUser || currentUser.role !== 'ALUMNO') return
        const approved = state.enrollments.some(
          (item) => item.studentId === currentUser.id && item.courseId === courseId && item.status === 'APROBADA',
        )
        const course = state.courses.find((item) => item.id === courseId)
        if (!approved || !course) return
        const existing = state.progressRecords.find(
          (item) => item.studentId === currentUser.id && item.courseId === courseId,
        )
        const completed = new Set(existing?.completedModuleIds ?? [])
        if (completed.has(moduleId)) completed.delete(moduleId)
        else completed.add(moduleId)
        const percentage = Math.round((completed.size / course.modules.length) * 100)
        dispatch({
          type: 'UPDATE_PROGRESS',
          progress: {
            id: existing?.id ?? uid('progress'),
            studentId: currentUser.id,
            courseId,
            completedModuleIds: Array.from(completed),
            percentage,
            status: progressStatus(percentage),
            lastActivityAt: now(),
          },
        })
      },
      setCourseStatus: (courseId, status) =>
        dispatch({ type: 'SET_COURSE_STATUS', courseId, status, date: now() }),
      adminEditCourse: (courseId, values) =>
        dispatch({ type: 'ADMIN_EDIT_COURSE', courseId, ...values, date: now() }),
      toggleUserStatus: (userId) => dispatch({ type: 'TOGGLE_USER_STATUS', userId }),
      markNotificationsRead: () => {
        if (currentUser) dispatch({ type: 'MARK_NOTIFICATIONS_READ', userId: currentUser.id })
      },
      resetDemoData: () => dispatch({ type: 'RESET', state: createDemoState() }),
    }
  }, [currentUser, state])

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>
}
