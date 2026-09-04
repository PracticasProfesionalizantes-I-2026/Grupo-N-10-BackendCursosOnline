import type { AppState, Course, Enrollment, Module, ProgressRecord, Role, User } from '@/types'
import { progressStatus } from '@/utils/format'

const today = new Date('2026-09-03T15:00:00.000Z')
const isoDaysAgo = (days: number) => new Date(today.getTime() - days * 86400000).toISOString()

const person = (
  id: string,
  role: Role,
  firstName: string,
  lastName: string,
  email: string,
  daysAgo: number,
): User => ({
  id,
  role,
  firstName,
  lastName,
  email,
  password: 'demo123',
  dni: '30' + id.replace(/\D/g, '').padStart(6, '0'),
  phone: '+54 9 11 5555-' + id.replace(/\D/g, '').padStart(4, '0').slice(-4),
  address: 'Av. del Conocimiento ' + (100 + daysAgo),
  postalCode: 'C1000',
  createdAt: isoDaysAgo(daysAgo),
  active: true,
})

export const demoUsers: User[] = [
  person('s1', 'ALUMNO', 'Sofía', 'Martínez', 'alumno@lumen.demo', 210),
  person('s2', 'ALUMNO', 'Valentín', 'Pérez', 'valentin@mail.com', 195),
  person('s3', 'ALUMNO', 'Tomás', 'Gutiérrez', 'tomas@mail.com', 183),
  person('s4', 'ALUMNO', 'Bruno', 'Castro', 'bruno@mail.com', 171),
  person('s5', 'ALUMNO', 'Joaquín', 'Soto', 'joaquin@mail.com', 160),
  person('s6', 'ALUMNO', 'Martina', 'López', 'martina@mail.com', 148),
  person('s7', 'ALUMNO', 'Camila', 'Torres', 'camila@mail.com', 139),
  person('s8', 'ALUMNO', 'Nicolás', 'Ramos', 'nicolas@mail.com', 124),
  person('s9', 'ALUMNO', 'Julieta', 'Acosta', 'julieta@mail.com', 112),
  person('s10', 'ALUMNO', 'Mateo', 'Díaz', 'mateo.diaz@mail.com', 96),
  person('s11', 'ALUMNO', 'Emilia', 'Vega', 'emilia@mail.com', 62),
  person('s12', 'ALUMNO', 'Benjamín', 'Ruiz', 'benjamin@mail.com', 34),
  person('t1', 'PROFESOR', 'Lucas', 'Fernández', 'profesor@lumen.demo', 280),
  person('t2', 'PROFESOR', 'Mateo', 'Álvarez', 'mateo@lumen.demo', 265),
  person('t3', 'PROFESOR', 'Camilo', 'Rojas', 'camilo@lumen.demo', 244),
  person('t4', 'PROFESOR', 'Paula', 'Benítez', 'paula@lumen.demo', 232),
  person('a1', 'ADMINISTRADOR', 'Renato', 'Molina', 'admin@lumen.demo', 365),
  person('a2', 'ADMINISTRADOR', 'Irene', 'Suárez', 'irene@lumen.demo', 340),
]

const moduleNames = [
  ['Bienvenida y fundamentos', 'Conceptos esenciales y recorrido inicial.'],
  ['Método y herramientas', 'Aplicación guiada sobre un caso concreto.'],
  ['Práctica integradora', 'Actividad para consolidar lo aprendido.'],
  ['Cierre y próximos pasos', 'Síntesis, recursos y plan de continuidad.'],
]

const modulesFor = (courseId: string, subject: string, count = 4): Module[] =>
  moduleNames.slice(0, count).map(([name, description], index) => ({
    id: courseId + '-m' + (index + 1),
    name: index === 0 ? 'Introducción a ' + subject : name,
    description,
    durationHours: [1.5, 2, 2.5, 1][index],
    resources: index === 0 ? ['Lectura introductoria', 'Video de bienvenida'] : ['Guía práctica', 'Actividad aplicada'],
  }))

const course = (
  id: string,
  title: string,
  teacherId: string,
  status: Course['status'],
  category: string,
  level: string,
  modality: string,
  daysAgo: number,
  description: string,
): Course => {
  const modules = modulesFor(id, title, 4)
  return {
    id,
    title,
    teacherId,
    status,
    category,
    level,
    modality,
    maxCapacity: 35,
    description,
    objectives: ['Comprender los fundamentos principales', 'Aplicar herramientas en un proyecto concreto', 'Evaluar decisiones con criterios claros'],
    prerequisites: level === 'Inicial' ? ['No se requieren conocimientos previos'] : ['Conocimientos básicos del área'],
    modules,
    durationHours: modules.reduce((sum, item) => sum + item.durationHours, 0),
    createdAt: isoDaysAgo(daysAgo),
    updatedAt: isoDaysAgo(Math.max(1, daysAgo - 20)),
    publishedAt: status === 'PUBLICADO' || status === 'PAUSADO' || status === 'FINALIZADO' ? isoDaysAgo(Math.max(2, daysAgo - 30)) : undefined,
  }
}

export const demoCourses: Course[] = [
  course('c1', 'Fundamentos de UX Design', 't1', 'PUBLICADO', 'Diseño', 'Inicial', 'Autogestionado', 190, 'Aprendé a investigar, idear y validar experiencias digitales centradas en las personas.'),
  course('c2', 'React Avanzado y Patrones', 't2', 'PUBLICADO', 'Desarrollo', 'Avanzado', 'Autogestionado', 175, 'Profundizá en composición, rendimiento y patrones mantenibles para aplicaciones React.'),
  course('c3', 'Introducción a React', 't2', 'PUBLICADO', 'Desarrollo', 'Inicial', 'Autogestionado', 160, 'Construí interfaces modernas con componentes, propiedades, estado y eventos.'),
  course('c4', 'Fundamentos de JavaScript', 't3', 'PUBLICADO', 'Desarrollo', 'Inicial', 'Autogestionado', 150, 'Dominá la base del lenguaje que impulsa la web moderna con práctica progresiva.'),
  course('c5', 'Diseño UX/UI desde cero', 't1', 'PAUSADO', 'Diseño', 'Inicial', 'Autogestionado', 145, 'Un recorrido completo desde la investigación hasta un prototipo navegable.'),
  course('c6', 'SQL para principiantes', 't1', 'BORRADOR', 'Datos', 'Inicial', 'Autogestionado', 84, 'Aprendé a consultar y organizar información con SQL desde los fundamentos.'),
  course('c7', 'Desarrollo Web con HTML y CSS', 't1', 'EN REVISIÓN', 'Desarrollo', 'Inicial', 'Autogestionado', 46, 'Creá sitios accesibles y responsivos con una base sólida de HTML y CSS.'),
  {
    ...course('c8', 'Introducción a TypeScript', 't1', 'CAMBIOS SOLICITADOS', 'Desarrollo', 'Intermedio', 'Autogestionado', 41, 'Incorporá tipado estático para crear aplicaciones JavaScript más confiables.'),
    adminObservation: 'Revisá los objetivos de aprendizaje y completá la descripción del módulo 2.',
    publishedAt: isoDaysAgo(92),
  },
  course('c9', 'Git y GitHub', 't4', 'RECHAZADO', 'Herramientas', 'Inicial', 'Autogestionado', 38, 'Organizá el trabajo en equipo mediante control de versiones y colaboración.'),
  course('c10', 'Diseño de Interfaces con Figma', 't4', 'FINALIZADO', 'Diseño', 'Intermedio', 'Autogestionado', 280, 'Diseñá interfaces consistentes, prototipos y componentes reutilizables.'),
  course('c11', 'Fundamentos de Bases de Datos', 't3', 'PUBLICADO', 'Datos', 'Inicial', 'Autogestionado', 132, 'Comprendé modelos, relaciones y decisiones básicas de almacenamiento de datos.'),
  course('c12', 'Accesibilidad Web Aplicada', 't4', 'PUBLICADO', 'Diseño', 'Intermedio', 'Autogestionado', 92, 'Creá experiencias digitales perceptibles, operables y comprensibles.'),
  course('c13', 'APIs REST con Node.js', 't3', 'PUBLICADO', 'Desarrollo', 'Intermedio', 'Autogestionado', 74, 'Diseñá APIs claras y mantenibles a partir de recursos, rutas y validaciones.'),
]

const enrollmentSeed: Array<[string, string, string, Enrollment['status'], number]> = [
  ['e1', 's1', 'c1', 'APROBADA', 120],
  ['e2', 's1', 'c2', 'APROBADA', 110],
  ['e3', 's1', 'c3', 'PENDIENTE', 4],
  ['e4', 's1', 'c4', 'RECHAZADA', 35],
  ['e5', 's1', 'c11', 'CANCELADA', 60],
  ['e6', 's2', 'c1', 'APROBADA', 118],
  ['e7', 's3', 'c1', 'APROBADA', 106],
  ['e8', 's4', 'c1', 'APROBADA', 99],
  ['e9', 's5', 'c1', 'APROBADA', 87],
  ['e10', 's6', 'c2', 'APROBADA', 82],
  ['e11', 's7', 'c2', 'APROBADA', 76],
  ['e12', 's8', 'c3', 'APROBADA', 70],
  ['e13', 's9', 'c3', 'APROBADA', 65],
  ['e14', 's10', 'c4', 'APROBADA', 54],
  ['e15', 's11', 'c4', 'PENDIENTE', 3],
  ['e16', 's12', 'c11', 'PENDIENTE', 2],
  ['e17', 's2', 'c12', 'APROBADA', 48],
  ['e18', 's3', 'c13', 'APROBADA', 45],
  ['e19', 's4', 'c11', 'RECHAZADA', 39],
  ['e20', 's5', 'c2', 'CANCELADA', 37],
  ['e21', 's6', 'c3', 'APROBADA', 34],
  ['e22', 's7', 'c4', 'APROBADA', 31],
  ['e23', 's8', 'c12', 'PENDIENTE', 1],
  ['e24', 's9', 'c13', 'APROBADA', 29],
]

export const demoEnrollments: Enrollment[] = enrollmentSeed.map(([id, studentId, courseId, status, daysAgo]) => ({
  id,
  studentId,
  courseId,
  status,
  requestedAt: isoDaysAgo(daysAgo),
  resolvedAt: status === 'PENDIENTE' ? undefined : isoDaysAgo(Math.max(0, daysAgo - 2)),
}))

const progress = (id: string, studentId: string, courseId: string, completed: number, daysAgo: number): ProgressRecord => {
  const courseItem = demoCourses.find((item) => item.id === courseId)!
  const completedModuleIds = courseItem.modules.slice(0, completed).map((item) => item.id)
  const percentage = Math.round((completedModuleIds.length / courseItem.modules.length) * 100)
  return {
    id,
    studentId,
    courseId,
    completedModuleIds,
    percentage,
    status: progressStatus(percentage),
    lastActivityAt: isoDaysAgo(daysAgo),
  }
}

export const demoProgress: ProgressRecord[] = [
  progress('p1', 's1', 'c1', 2, 1),
  progress('p2', 's1', 'c2', 4, 8),
  progress('p3', 's2', 'c1', 4, 1),
  progress('p4', 's3', 'c1', 3, 2),
  progress('p5', 's4', 'c1', 1, 4),
  progress('p6', 's5', 'c1', 2, 7),
  progress('p7', 's6', 'c2', 2, 2),
  progress('p8', 's7', 'c2', 1, 5),
  progress('p9', 's8', 'c3', 3, 1),
  progress('p10', 's9', 'c3', 2, 9),
  progress('p11', 's10', 'c4', 1, 3),
  progress('p12', 's6', 'c3', 0, 12),
  progress('p13', 's7', 'c4', 4, 1),
  progress('p14', 's2', 'c12', 2, 6),
  progress('p15', 's3', 'c13', 1, 3),
  progress('p16', 's9', 'c13', 3, 2),
]

export const createDemoState = (): AppState => ({
  users: demoUsers,
  courses: demoCourses,
  enrollments: demoEnrollments,
  progressRecords: demoProgress,
  audits: [
    { id: 'au1', courseId: 'c7', teacherId: 't1', type: 'Creación', status: 'PENDIENTE', createdAt: isoDaysAgo(2) },
    { id: 'au2', courseId: 'c8', teacherId: 't1', type: 'Modificación', status: 'RESUELTA', createdAt: isoDaysAgo(8), resolvedAt: isoDaysAgo(5), decision: 'CAMBIOS SOLICITADOS', observations: 'Revisá los objetivos de aprendizaje y completá la descripción del módulo 2.' },
    { id: 'au3', courseId: 'c9', teacherId: 't4', type: 'Creación', status: 'RESUELTA', createdAt: isoDaysAgo(11), resolvedAt: isoDaysAgo(9), decision: 'RECHAZADA' },
  ],
  recentActivity: [
    { id: 'ac1', audience: 'PROFESOR', userId: 't1', message: 'Se solicitaron cambios en Introducción a TypeScript.', createdAt: isoDaysAgo(5) },
    { id: 'ac2', audience: 'PROFESOR', userId: 't1', message: 'Sofía avanzó en Fundamentos de UX Design.', createdAt: isoDaysAgo(1) },
    { id: 'ac3', audience: 'ADMINISTRADOR', message: 'Nueva solicitud para Introducción a React.', createdAt: isoDaysAgo(1) },
    { id: 'ac4', audience: 'TODOS', message: 'Accesibilidad Web Aplicada fue publicado.', createdAt: isoDaysAgo(7) },
  ],
  notifications: [
    { id: 'n1', userId: 's1', title: 'Seguí aprendiendo', body: 'Tenés un módulo pendiente en Fundamentos de UX Design.', createdAt: isoDaysAgo(1), read: false },
    { id: 'n2', userId: 't1', title: 'Cambios solicitados', body: 'Revisá las observaciones de Introducción a TypeScript.', createdAt: isoDaysAgo(5), read: false },
    { id: 'n3', userId: 'a1', title: 'Auditoría pendiente', body: 'Desarrollo Web con HTML y CSS espera revisión.', createdAt: isoDaysAgo(2), read: false },
  ],
  currentUserId: null,
})

export const DEMO_STATE_KEY = 'lumen-demo-state'
