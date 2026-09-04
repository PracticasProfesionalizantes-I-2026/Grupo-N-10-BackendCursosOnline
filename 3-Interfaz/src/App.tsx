import { lazy, Suspense } from 'react'
import { MotionConfig } from 'framer-motion'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppProvider } from '@/context/AppContext'
import { PublicLayout } from '@/layouts/PublicLayout'
import { Landing } from '@/pages/public/Landing'
import { Login } from '@/pages/public/Login'
import { Register } from '@/pages/public/Register'
import { PublicCatalog } from '@/pages/public/PublicCatalog'
import { AppShell } from '@/layouts/AppShell'
import { RoleGuard } from '@/components/RoleGuard'

const StudentDashboard = lazy(() => import('@/pages/student/Dashboard').then((module) => ({ default: module.StudentDashboard })))
const StudentCatalog = lazy(() => import('@/pages/student/Dashboard').then((module) => ({ default: module.StudentCatalog })))
const StudentCourseDetail = lazy(() => import('@/pages/student/CourseAndRequests').then((module) => ({ default: module.StudentCourseDetail })))
const StudentRequests = lazy(() => import('@/pages/student/CourseAndRequests').then((module) => ({ default: module.StudentRequests })))
const StudentCoursePlayer = lazy(() => import('@/pages/student/Learning').then((module) => ({ default: module.StudentCoursePlayer })))
const StudentMyCourses = lazy(() => import('@/pages/student/Learning').then((module) => ({ default: module.StudentMyCourses })))
const StudentProgress = lazy(() => import('@/pages/student/Learning').then((module) => ({ default: module.StudentProgress })))
const TeacherDashboard = lazy(() => import('@/pages/teacher/DashboardAndCourses').then((module) => ({ default: module.TeacherDashboard })))
const TeacherCourses = lazy(() => import('@/pages/teacher/DashboardAndCourses').then((module) => ({ default: module.TeacherCourses })))
const TeacherCreateCourse = lazy(() => import('@/pages/teacher/DashboardAndCourses').then((module) => ({ default: module.TeacherCreateCourse })))
const TeacherEditCourse = lazy(() => import('@/pages/teacher/DashboardAndCourses').then((module) => ({ default: module.TeacherEditCourse })))
const TeacherStudents = lazy(() => import('@/pages/teacher/Students').then((module) => ({ default: module.TeacherStudents })))
const AdminDashboard = lazy(() => import('@/pages/admin/DashboardAndAudits').then((module) => ({ default: module.AdminDashboard })))
const AdminAudits = lazy(() => import('@/pages/admin/DashboardAndAudits').then((module) => ({ default: module.AdminAudits })))
const AdminEnrollments = lazy(() => import('@/pages/admin/Enrollments').then((module) => ({ default: module.AdminEnrollments })))
const AdminCourses = lazy(() => import('@/pages/admin/UsersAndCourses').then((module) => ({ default: module.AdminCourses })))
const AdminUsers = lazy(() => import('@/pages/admin/UsersAndCourses').then((module) => ({ default: module.AdminUsers })))
const AdminReports = lazy(() => import('@/pages/admin/Reports').then((module) => ({ default: module.AdminReports })))

export default function App() {
  return (
    <BrowserRouter>
      <AppProvider>
        <MotionConfig reducedMotion="user">
        <Suspense fallback={<div className="grid min-h-screen place-items-center bg-canvas"><div className="size-10 animate-spin rounded-full border-2 border-slate-300 border-t-primary" aria-label="Cargando" /></div>}>
          <Routes>
          <Route element={<PublicLayout />}>
            <Route path="/" element={<Landing />} />
            <Route path="/login" element={<Login />} />
            <Route path="/registro" element={<Register />} />
            <Route path="/cursos" element={<PublicCatalog />} />
          </Route>
          <Route element={<RoleGuard role="ALUMNO" />}>
            <Route element={<AppShell />}>
              <Route path="/student" element={<StudentDashboard />} />
              <Route path="/student/explore" element={<StudentCatalog />} />
              <Route path="/student/courses/:courseId" element={<StudentCourseDetail />} />
              <Route path="/student/requests" element={<StudentRequests />} />
              <Route path="/student/my-courses" element={<StudentMyCourses />} />
              <Route path="/student/progress" element={<StudentProgress />} />
              <Route path="/student/learn/:courseId" element={<StudentCoursePlayer />} />
            </Route>
          </Route>
          <Route element={<RoleGuard role="PROFESOR" />}>
            <Route element={<AppShell />}>
              <Route path="/teacher" element={<TeacherDashboard />} />
              <Route path="/teacher/courses" element={<TeacherCourses />} />
              <Route path="/teacher/create" element={<TeacherCreateCourse />} />
              <Route path="/teacher/courses/:courseId/edit" element={<TeacherEditCourse />} />
              <Route path="/teacher/students" element={<TeacherStudents />} />
            </Route>
          </Route>
          <Route element={<RoleGuard role="ADMINISTRADOR" />}>
            <Route element={<AppShell />}>
              <Route path="/admin" element={<AdminDashboard />} />
              <Route path="/admin/audits" element={<AdminAudits />} />
              <Route path="/admin/enrollments" element={<AdminEnrollments />} />
              <Route path="/admin/users" element={<AdminUsers />} />
              <Route path="/admin/courses" element={<AdminCourses />} />
              <Route path="/admin/reports" element={<AdminReports />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
        </MotionConfig>
      </AppProvider>
    </BrowserRouter>
  )
}
