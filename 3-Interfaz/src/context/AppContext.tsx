import { createContext, useEffect, useMemo, useReducer, useState, type ReactNode } from 'react'
import { createDemoState, DEMO_STATE_KEY, demoUsers, matchesSeedPassword } from '@/data/seed'
import type { AppState, AuditDecision, Course, CourseDraft, CourseStatus, EnrollmentStatus, Role, User } from '@/types'
import { courseVersion, uid } from '@/utils/format'
import {
  calculatePublishedProgress, canEditWorkingCourse, canRequestEnrollment,
  getApprovedEnrollment, getPublication, getWorkingEditBase, identifyWorkingModules, linkLegacyProgress, recoverWorkingDraft,
} from '@/services/courseService'
import { appReducer } from './appReducer'
import { createDemoCredential, matchesDemoCredential, migrateDemoUsers } from '@/services/demoCredentials'

const loadState = async (): Promise<AppState> => {
  let state: AppState
  try {
    const saved = localStorage.getItem(DEMO_STATE_KEY)
    if (!saved) return createDemoState()
    state = JSON.parse(saved) as AppState
    if (!Array.isArray(state.users) || !Array.isArray(state.enrollments) || !Array.isArray(state.progressRecords))
      return createDemoState()
  } catch {
    return createDemoState()
  }
  return { ...state, users: await migrateDemoUsers(state.users, demoUsers), progressRecords: linkLegacyProgress(state) }
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
  login: (email: string, password: string) => Promise<User | undefined>
  loginAs: (role: Role) => User
  logout: () => void
  register: (payload: RegisterPayload) => Promise<User>
  createCourse: (draft: CourseDraft, submit: boolean) => Course
  updateCourse: (courseId: string, draft: CourseDraft, submit: boolean) => Course | undefined
  recoverCourse: (courseId: string) => boolean
  resolveAudit: (auditId: string, decision: AuditDecision, observations?: string) => void
  requestEnrollment: (courseId: string) => boolean
  updateEnrollment: (enrollmentId: string, status: EnrollmentStatus) => void
  toggleModule: (courseId: string, moduleId: string) => void
  setCourseStatus: (courseId: string, status: CourseStatus) => void
  toggleUserStatus: (userId: string) => void
  markNotificationsRead: () => void
  resetDemoData: () => void
}

export const AppContext = createContext<AppContextValue | undefined>(undefined)

export function AppProvider({ children }: { children: ReactNode }) {
  const [initialState, setInitialState] = useState<AppState>()
  const [error, setError] = useState(false)
  useEffect(() => {
    let active = true
    void loadState().then((state) => { if (active) setInitialState(state) },
      () => { if (active) setError(true) })
    return () => { active = false }
  }, [])
  if (error) return <p role="alert">No pudimos abrir la demostración. Probá en un navegador con conexión segura.</p>
  if (!initialState) return <p role="status">Cargando demostración…</p>
  return <AppSessionProvider initialState={initialState}>{children}</AppSessionProvider>
}

function AppSessionProvider({ children, initialState }: { children: ReactNode; initialState: AppState }) {
  const [state, dispatch] = useReducer(appReducer, initialState)
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
      login: async (email, password) => {
        const user = state.users.find(
          (item) => item.email.toLowerCase() === email.trim().toLowerCase() && item.active,
        )
        if (!user || !(user.demoAccount ? matchesSeedPassword(password) : await matchesDemoCredential(password, user.credential)))
          return undefined
        dispatch({ type: 'LOGIN', userId: user.id })
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
      register: async ({ password, ...profile }) => {
        const credential = await createDemoCredential(password)
        const user: User = {
          ...profile,
          credential,
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
          ? { id: uid('audit'), courseId: course.id, teacherId: currentUser.id, type: 'Creación' as const,
              status: 'PENDIENTE' as const, createdAt: date, submission: structuredClone(courseVersion(course)) }
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
        if (!original || !canEditWorkingCourse(original) ||
          !(currentUser.role === 'ADMINISTRADOR' || (currentUser.role === 'PROFESOR' && ownsCourse))) return undefined
        const occupied = state.enrollments.filter((item) =>
          item.courseId === courseId && item.status === 'APROBADA').length
        if (draft.maxCapacity < original.maxCapacity && draft.maxCapacity < occupied) return undefined
        const date = now()
        const editBase = getWorkingEditBase(original)
        const updated: Course = {
          ...original,
          ...draft,
          modules: identifyWorkingModules(original, draft.modules),
          status: submit ? 'EN REVISIÓN' : 'BORRADOR',
          ...editBase,
          adminObservation: submit ? undefined : original.adminObservation,
          updatedAt: date,
        }
        const audit = submit
          ? {
              id: uid('audit'),
              courseId: original.id,
              teacherId: original.teacherId,
              type: original.publishedAt || editBase.publishedVersion ? ('Modificación' as const) : ('Creación' as const),
              status: 'PENDIENTE' as const,
              createdAt: date,
              submission: structuredClone(courseVersion(updated)),
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
      recoverCourse: (courseId) => {
        const course = state.courses.find((item) => item.id === courseId)
        if (!currentUser || !course || !(currentUser.role === 'ADMINISTRADOR' ||
          (currentUser.role === 'PROFESOR' && course.teacherId === currentUser.id))) return false
        const date = now()
        const draft = recoverWorkingDraft(course, date)
        if (!draft) return false
        dispatch({ type: 'UPDATE_COURSE', course: draft, activity: {
          id: uid('act'), audience: currentUser.role, userId: currentUser.id,
          message: 'Se recuperó un borrador de ' + course.title + '.', createdAt: date,
        } })
        return true
      },
      resolveAudit: (auditId, decision, observations) => {
        if (currentUser?.role === 'ADMINISTRADOR')
          dispatch({ type: 'RESOLVE_AUDIT', auditId, decision, observations, date: now() })
      },
      requestEnrollment: (courseId) => {
        if (!currentUser || currentUser.role !== 'ALUMNO') return false
        const course = state.courses.find((item) => item.id === courseId)
        const duplicate = state.enrollments.some(
          (item) =>
            item.courseId === courseId &&
            item.studentId === currentUser.id &&
            (item.status === 'PENDIENTE' || item.status === 'APROBADA'),
        )
        if (!canRequestEnrollment(course) || duplicate) return false
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
      updateEnrollment: (enrollmentId, status) => {
        const enrollment = state.enrollments.find((item) => item.id === enrollmentId)
        if (!enrollment || !currentUser ||
          (currentUser.role !== 'ADMINISTRADOR' &&
            !(currentUser.role === 'ALUMNO' && status === 'CANCELADA' && enrollment.studentId === currentUser.id))) return
        const course = state.courses.find((item) => item.id === enrollment.courseId)
        if (status === 'APROBADA' && (!course || course.operationalStatus === 'FINALIZADO' ||
          state.enrollments.filter((item) => item.courseId === enrollment.courseId && item.status === 'APROBADA').length >=
            (getPublication(course)?.maxCapacity ?? course.maxCapacity))) return
        dispatch({ type: 'UPDATE_ENROLLMENT', enrollmentId, status, date: now() })
      },
      toggleModule: (courseId, moduleId) => {
        if (!currentUser || currentUser.role !== 'ALUMNO') return
        const approved = getApprovedEnrollment(state, currentUser.id, courseId)
        const course = getPublication(state.courses.find((item) => item.id === courseId))
        if (!approved || !course?.modules.some((item) => item.id === moduleId)) return
        const existing = state.progressRecords.find(
          (item) => item.enrollmentId === approved.id,
        )
        const completed = new Set(existing?.completedModuleIds ?? [])
        if (completed.has(moduleId)) completed.delete(moduleId)
        else completed.add(moduleId)
        const { percentage, status } = calculatePublishedProgress(course, completed)
        dispatch({
          type: 'UPDATE_PROGRESS',
          progress: {
            id: existing?.id ?? uid('progress'),
            enrollmentId: approved.id,
            studentId: currentUser.id,
            courseId,
            completedModuleIds: Array.from(completed),
            percentage,
            status,
            lastActivityAt: now(),
          },
        })
      },
      setCourseStatus: (courseId, status) =>
        dispatch({ type: 'SET_COURSE_STATUS', courseId, status, date: now() }),
      toggleUserStatus: (userId) => dispatch({ type: 'TOGGLE_USER_STATUS', userId }),
      markNotificationsRead: () => {
        if (currentUser) dispatch({ type: 'MARK_NOTIFICATIONS_READ', userId: currentUser.id })
      },
      resetDemoData: () => dispatch({ type: 'RESET', state: createDemoState() }),
    }
  }, [currentUser, state])

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>
}
