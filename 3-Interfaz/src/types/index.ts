export type Role = 'ALUMNO' | 'PROFESOR' | 'ADMINISTRADOR'

export type CourseStatus =
  | 'BORRADOR'
  | 'EN REVISIÓN'
  | 'CAMBIOS SOLICITADOS'
  | 'RECHAZADO'
  | 'PUBLICADO'
  | 'PAUSADO'
  | 'FINALIZADO'

export type EnrollmentStatus = 'PENDIENTE' | 'APROBADA' | 'RECHAZADA' | 'CANCELADA'
export type ProgressStatus = 'NO INICIADO' | 'EN PROGRESO' | 'COMPLETADO'
export type AuditType = 'Creación' | 'Modificación'
export type AuditStatus = 'PENDIENTE' | 'RESUELTA'
export type AuditDecision = 'APROBADA' | 'CAMBIOS SOLICITADOS' | 'RECHAZADA'

export interface User {
  id: string
  email: string
  password: string
  role: Role
  firstName: string
  lastName: string
  dni: string
  phone: string
  address: string
  postalCode: string
  createdAt: string
  active: boolean
}

export interface Module {
  id: string
  name: string
  description: string
  durationHours: number
  resources: string[]
}

export interface CourseVersion {
  title: string
  description: string
  category: string
  level: string
  modality: string
  maxCapacity: number
  objectives: string[]
  prerequisites: string[]
  modules: Module[]
  durationHours: number
}

export interface Course extends CourseVersion {
  id: string
  teacherId: string
  status: CourseStatus
  createdAt: string
  updatedAt: string
  publishedAt?: string
  adminObservation?: string
  publishedVersion?: CourseVersion
}

export interface Enrollment {
  id: string
  studentId: string
  courseId: string
  status: EnrollmentStatus
  requestedAt: string
  resolvedAt?: string
}

export interface ProgressRecord {
  id: string
  studentId: string
  courseId: string
  completedModuleIds: string[]
  percentage: number
  status: ProgressStatus
  lastActivityAt: string
}

export interface Audit {
  id: string
  courseId: string
  teacherId: string
  type: AuditType
  status: AuditStatus
  createdAt: string
  resolvedAt?: string
  decision?: AuditDecision
  observations?: string
}

export interface Activity {
  id: string
  audience: Role | 'TODOS'
  userId?: string
  message: string
  createdAt: string
}

export interface Notification {
  id: string
  userId: string
  title: string
  body: string
  createdAt: string
  read: boolean
}

export interface AppState {
  users: User[]
  courses: Course[]
  enrollments: Enrollment[]
  progressRecords: ProgressRecord[]
  audits: Audit[]
  recentActivity: Activity[]
  notifications: Notification[]
  currentUserId: string | null
}

export type CourseDraft = Omit<Course, 'id' | 'teacherId' | 'status' | 'createdAt' | 'updatedAt'>

export type AppAction =
  | { type: 'LOGIN'; userId: string }
  | { type: 'LOGOUT' }
  | { type: 'REGISTER'; user: User }
  | { type: 'CREATE_COURSE'; course: Course; audit?: Audit; activity: Activity }
  | { type: 'UPDATE_COURSE'; course: Course; audit?: Audit; activity: Activity }
  | { type: 'RESOLVE_AUDIT'; auditId: string; decision: AuditDecision; observations?: string; date: string }
  | { type: 'REQUEST_ENROLLMENT'; enrollment: Enrollment }
  | { type: 'UPDATE_ENROLLMENT'; enrollmentId: string; status: EnrollmentStatus; date: string }
  | { type: 'UPDATE_PROGRESS'; progress: ProgressRecord }
  | { type: 'SET_COURSE_STATUS'; courseId: string; status: CourseStatus; date: string }
  | { type: 'ADMIN_EDIT_COURSE'; courseId: string; title: string; description: string; maxCapacity: number; date: string }
  | { type: 'TOGGLE_USER_STATUS'; userId: string }
  | { type: 'MARK_NOTIFICATIONS_READ'; userId: string }
  | { type: 'RESET'; state: AppState }
