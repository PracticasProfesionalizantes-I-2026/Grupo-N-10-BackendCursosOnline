import { motion } from 'framer-motion'
import { ArrowRight, BookOpen, Clock3, GraduationCap, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { Course } from '@/types'
import { fullName, formatDuration } from '@/utils/format'
import { CourseArtwork, CourseStatusBadge, InfoPill, ProgressBar, ProgressStatusBadge } from '@/components/UI'
import { useApp } from '@/hooks/useApp'

export function CourseCard({
  course,
  href,
  progress,
  actionLabel = 'Ver curso',
  showStatus = false,
}: {
  course: Course
  href: string
  progress?: number
  actionLabel?: string
  showStatus?: boolean
}) {
  const { state } = useApp()
  const teacher = state.users.find((user) => user.id === course.teacherId)
  const progressState = progress === undefined ? undefined : progress === 0 ? 'NO INICIADO' : progress === 100 ? 'COMPLETADO' : 'EN PROGRESO'

  return (
    <motion.article
      layout
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      whileHover={{ y: -3 }}
      transition={{ duration: 0.38, ease: [0.22, 1, 0.36, 1] }}
      className="card group flex h-full flex-col overflow-hidden transition-colors hover:border-primary-border"
    >
      <CourseArtwork courseId={course.id} category={course.category} className="h-36 border-b border-slate-200" />
      <div className="flex flex-1 flex-col p-5">
        <div className="flex min-h-7 flex-wrap items-center justify-between gap-2">
          <div className="flex flex-wrap gap-2">
            <InfoPill icon={GraduationCap}>{course.level}</InfoPill>
            <InfoPill>{course.modality}</InfoPill>
          </div>
          {showStatus && <CourseStatusBadge status={course.status} />}
        </div>

        <h3 className="mt-5 text-xl font-semibold leading-7 tracking-[-0.025em] text-foreground">{course.title}</h3>
        <p className="mt-2 line-clamp-2 text-sm leading-6 text-muted">{course.description}</p>
        <p className="mt-4 text-sm font-medium text-foreground-secondary">Prof. {fullName(teacher)}</p>

        <div className="mt-4 flex flex-wrap gap-x-4 gap-y-2 text-xs text-muted-dark">
          <span className="inline-flex items-center gap-1.5"><Clock3 size={14} /> {formatDuration(course.durationHours)}</span>
          <span className="inline-flex items-center gap-1.5"><BookOpen size={14} /> {course.modules.length} módulos</span>
          <span className="inline-flex items-center gap-1.5"><Users size={14} /> {course.maxCapacity} cupos</span>
        </div>

        {progress !== undefined && progressState && (
          <div className="mt-5 border-t border-slate-200 pt-4">
            <div className="mb-2 flex items-center justify-between gap-3">
              <ProgressStatusBadge status={progressState} />
              <span className="text-sm font-bold text-primary">{progress} %</span>
            </div>
            <ProgressBar value={progress} />
          </div>
        )}

        <Link
          to={href}
          className="mt-5 inline-flex items-center justify-between border-t border-slate-200 pt-4 text-sm font-bold text-foreground transition hover:text-primary"
        >
          {actionLabel}
          <ArrowRight className="text-primary transition-transform group-hover:translate-x-1" size={17} />
        </Link>
      </div>
    </motion.article>
  )
}

