import type { AppState, Course } from '@/types'
import { publishedCourseView } from '@/utils/format'

export const courseService = {
  getPublishedCourses(state: AppState): Course[] {
    return state.courses
      .filter((course) => course.status === 'PUBLICADO' || Boolean(course.publishedVersion))
      .map(publishedCourseView)
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
    return state.courses.filter((course) => ids.has(course.id))
  },
}
