import assert from 'node:assert/strict'
import test from 'node:test'
import { appReducer } from '../src/context/appReducer.ts'
import {
  calculatePublishedProgress, canEditWorkingCourse, canRequestEnrollment, courseService, getEnrollmentProgress, getPublication, getWorkingEditBase, linkLegacyProgress,
} from '../src/services/courseService.ts'
import { createDemoCredential, matchesDemoCredential, migrateDemoUsers } from '../src/services/demoCredentials.ts'
import { dateInputValue, reportDateRange } from '../src/utils/format.ts'

const date = '2026-11-05T12:00:00Z'
const old = { id: 'old', studentId: 's', courseId: 'c', status: 'APROBADA', requestedAt: date }
const course = {
  id: 'c', status: 'EN REVISIÓN', title: 'Borrador', modules: [{ id: 'borrador' }],
  publishedVersion: { title: 'Publicado', modules: [{ id: 'publicado' }] },
}
const state = {
  courses: [course], enrollments: [old], notifications: [],
  progressRecords: [{ id: 'p', enrollmentId: old.id, studentId: 's', courseId: 'c', completedModuleIds: ['publicado'], percentage: 100 }],
}

test('catalog and approved students use only published content during review', () => {
  assert.equal(courseService.getPublishedCourses(state)[0].title, 'Publicado')
  assert.equal(getPublication(course).title, 'Publicado')
  assert.deepEqual(courseService.getApprovedStudentCourses(state, 's')[0].modules.map(x => x.id), ['publicado'])
  assert.equal(getEnrollmentProgress(state, old).percentage, 100)
  assert.equal(calculatePublishedProgress(courseService.getApprovedStudentCourses(state, 's')[0], ['borrador']).percentage, 0)
  assert.equal(canRequestEnrollment(course), true)
  assert.equal(canRequestEnrollment({ ...course, publishedVersion: undefined }), false)
})

test('a closed course stays out of the catalog without removing existing approved access', () => {
  for (const status of ['PAUSADO', 'FINALIZADO']) {
    const closed = { ...course, status }
    const snapshot = { ...state, courses: [closed] }
    assert.equal(canRequestEnrollment(closed), false)
    assert.deepEqual(courseService.getPublishedCourses(snapshot), [])
    assert.equal(courseService.getApprovedStudentCourses(snapshot, 's')[0].status, status)
  }
})

test('editing a paused course keeps the publication paused until explicit resume', () => {
  const paused = { ...course, status: 'PAUSADO', title: 'Publicado', modules: [{ id: 'publicado' }], publishedVersion: undefined }
  const draft = { ...paused, ...getWorkingEditBase(paused), status: 'EN REVISIÓN', title: 'Borrador' }
  const before = { ...state, courses: [draft] }
  assert.equal(canRequestEnrollment(draft), false)
  assert.equal(courseService.getApprovedStudentCourses(before, 's')[0].title, 'Publicado')
  const resumed = appReducer(before, { type: 'SET_COURSE_STATUS', courseId: draft.id, status: 'PUBLICADO', date })
  assert.equal(resumed.courses[0].status, 'EN REVISIÓN')
  assert.equal(courseService.getPublishedCourses(resumed)[0].title, 'Publicado')
})

test('editing and review guards preserve an earlier publication and a closed audit', () => {
  const pending = { id: 'review', courseId: course.id, status: 'PENDIENTE' }
  const submitted = { ...state, audits: [pending], recentActivity: [] }
  const decide = { type: 'RESOLVE_AUDIT', auditId: pending.id, date }
  assert.equal(canEditWorkingCourse(course), false)
  assert.equal(appReducer(submitted, { ...decide, decision: 'CAMBIOS SOLICITADOS', observations: ' ' }), submitted)
  const rejected = appReducer(submitted, { ...decide, decision: 'RECHAZADA' })
  assert.equal(canEditWorkingCourse(rejected.courses[0]), false)
  assert.equal(getPublication(rejected.courses[0]).title, 'Publicado')
  assert.equal(appReducer(rejected, { ...decide, decision: 'APROBADA' }), rejected)
  const changes = appReducer(submitted, { ...decide, decision: 'CAMBIOS SOLICITADOS', observations: 'Corregir módulo' })
  assert.equal(canEditWorkingCourse(changes.courses[0]), true)
})

test('cancellation and a new approval preserve history without reusing progress', () => {
  const canceled = appReducer(state, { type: 'UPDATE_ENROLLMENT', enrollmentId: old.id, status: 'CANCELADA', date })
  const pending = { ...old, id: 'new', status: 'PENDIENTE' }
  const reapplied = appReducer(canceled, { type: 'REQUEST_ENROLLMENT', enrollment: pending })
  assert.equal(appReducer(reapplied, { type: 'UPDATE_ENROLLMENT', enrollmentId: pending.id, status: 'CANCELADA', date }), reapplied)
  const approved = appReducer(reapplied, { type: 'UPDATE_ENROLLMENT', enrollmentId: pending.id, status: 'APROBADA', date })
  assert.equal(approved.progressRecords.find(item => item.enrollmentId === old.id).percentage, 100)
  assert.equal(getEnrollmentProgress(approved, approved.enrollments.find(item => item.id === pending.id)).percentage, 0)
  assert.equal(getEnrollmentProgress(approved, approved.enrollments.find(item => item.id === old.id)), undefined)
})

test('legacy progress is attributed only when the enrollment is unambiguous', () => {
  const legacy = { ...state.progressRecords[0], enrollmentId: undefined }
  assert.equal(linkLegacyProgress({ ...state, progressRecords: [legacy] })[0].enrollmentId, old.id)
  const ambiguous = { ...state, progressRecords: [legacy], enrollments: [old, { ...old, id: 'old-2', status: 'CANCELADA' }] }
  assert.deepEqual(linkLegacyProgress(ambiguous), [legacy])
})

test('reports include the final millisecond of the local day and exclude the next midnight', () => {
  const includes = reportDateRange('2026-11-05', '2026-11-05')
  assert.equal(includes('2026-11-05T23:59:59.999'), true)
  assert.equal(includes('2026-11-06T00:00:00'), false)
  assert.equal(dateInputValue(new Date(2026, 10, 5, 23, 59)), '2026-11-05')
})

test('demo credential migration preserves access without serializing the password', async () => {
  const password = 'synthetic-not-stored!'
  const oldUser = { id: 'local', email: 'local@example.test', password }
  const seed = { id: 'seed', email: 'seed@example.test', demoAccount: true }
  const [migrated, demo] = await migrateDemoUsers([oldUser, { ...seed, password }], [seed])
  assert.equal(JSON.stringify([migrated, demo]).includes(password), false)
  assert.equal(await matchesDemoCredential(password, migrated.credential), true)
  assert.equal(await matchesDemoCredential('incorrecta', migrated.credential), false)
  assert.notEqual((await createDemoCredential(password)).salt, migrated.credential.salt)
  assert.equal(demo.demoAccount, true)
})
