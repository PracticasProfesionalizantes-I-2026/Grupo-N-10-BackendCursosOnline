import { motion } from 'framer-motion'
import {
  ArrowRight,
  BookOpen,
  CheckCircle2,
  ClipboardCheck,
  LayoutDashboard,
  Menu,
  Play,
  Users,
} from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Logo, LumenOrb, ProgressBar } from '@/components/UI'
import { CourseCard } from '@/components/CourseCard'
import { useApp } from '@/hooks/useApp'
import { courseService } from '@/services/courseService'

const reveal = {
  hidden: { opacity: 0, y: 18 },
  visible: { opacity: 1, y: 0 },
}

export function Landing() {
  const { state } = useApp()
  const [menuOpen, setMenuOpen] = useState(false)
  const featured = courseService.getPublishedCourses(state).slice(0, 3)

  return (
    <div className="min-h-screen overflow-hidden bg-background text-foreground">
      <header className="fixed inset-x-0 top-0 z-40 border-b border-slate-200 bg-background/88 backdrop-blur-xl">
        <div className="mx-auto flex h-20 max-w-7xl items-center px-5 sm:px-8">
          <Logo />
          <nav className="ml-auto hidden items-center gap-8 md:flex" aria-label="Navegación pública">
            <a href="#cursos" className="text-sm font-semibold text-muted hover:text-primary">Cursos</a>
            <a href="#plataforma" className="text-sm font-semibold text-muted hover:text-primary">Plataforma</a>
            <Link to="/login" className="text-sm font-semibold text-foreground-secondary hover:text-primary">Iniciar sesión</Link>
            <Link to="/registro" className="btn-primary">Crear cuenta <ArrowRight size={16} /></Link>
          </nav>
          <button className="btn-secondary ml-auto px-3 md:hidden" onClick={() => setMenuOpen((value) => !value)} aria-label="Abrir menú">
            <Menu size={20} />
          </button>
        </div>
        {menuOpen && (
          <nav className="border-t border-slate-200 bg-background-secondary px-5 py-4 md:hidden">
            <div className="mx-auto grid max-w-7xl gap-2">
              <a href="#cursos" className="btn-ghost justify-start" onClick={() => setMenuOpen(false)}>Cursos</a>
              <a href="#plataforma" className="btn-ghost justify-start" onClick={() => setMenuOpen(false)}>Plataforma</a>
              <Link to="/login" className="btn-secondary">Iniciar sesión</Link>
              <Link to="/registro" className="btn-primary">Crear cuenta</Link>
            </div>
          </nav>
        )}
      </header>

      <main>
        <section className="page-grid relative flex min-h-screen items-center overflow-hidden px-5 pb-20 pt-32 sm:px-8 sm:pt-36">
          <div className="ambient-glow -right-40 top-20 opacity-70" aria-hidden="true" />
          <div className="relative mx-auto grid w-full max-w-7xl items-center gap-14 lg:grid-cols-[1.05fr_.95fr]">
            <div className="relative z-10">
              <motion.p
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                className="eyebrow"
              >
                Sistema de gestión de cursos online
              </motion.p>
              <motion.h1
                initial={{ opacity: 0, y: 18 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.06, duration: 0.52, ease: [0.22, 1, 0.36, 1] }}
                className="text-balance mt-6 max-w-4xl font-display text-6xl font-semibold leading-[0.93] tracking-[-0.065em] sm:text-7xl lg:text-[5.6rem]"
              >
                Aprendé lo que importa.
                <span className="mt-2 block text-primary">Avanzá a tu ritmo.</span>
              </motion.h1>
              <motion.p
                initial={{ opacity: 0, y: 18 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.12, duration: 0.5 }}
                className="mt-7 max-w-xl text-lg leading-8 text-muted"
              >
                Cursos, inscripciones y progreso coordinados en una experiencia clara para alumnos, profesores y administradores.
              </motion.p>
              <motion.div
                initial={{ opacity: 0, y: 18 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.18, duration: 0.5 }}
                className="mt-8 flex flex-col gap-3 sm:flex-row"
              >
                <Link to="/cursos" className="btn-primary px-6">Explorar cursos <ArrowRight size={17} /></Link>
                <Link to="/login" className="btn-secondary px-6"><Play size={16} /> Iniciar sesión</Link>
              </motion.div>
              <div className="mt-9 flex flex-wrap gap-x-6 gap-y-3 text-sm text-muted">
                <span className="inline-flex items-center gap-2"><CheckCircle2 size={16} className="text-primary" /> Cursos por módulos</span>
                <span className="inline-flex items-center gap-2"><CheckCircle2 size={16} className="text-primary" /> Progreso visible</span>
                <span className="inline-flex items-center gap-2"><CheckCircle2 size={16} className="text-primary" /> Tres roles coordinados</span>
              </div>
            </div>

            <motion.div
              initial={{ opacity: 0, scale: 0.96 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ delay: 0.18, duration: 0.65, ease: [0.22, 1, 0.36, 1] }}
              className="relative hidden min-h-[38rem] lg:block"
              aria-hidden="true"
            >
              <LumenOrb className="-right-20 -top-16 w-[40rem]" />
              <div className="absolute left-4 top-24 w-[29rem] border border-slate-300 bg-background-secondary/90 p-6 backdrop-blur">
                <div className="flex items-center justify-between border-b border-slate-200 pb-4">
                  <div>
                    <p className="eyebrow">Alumno</p>
                    <p className="mt-2 text-2xl font-semibold tracking-[-0.03em]">Hola, Sofía</p>
                  </div>
                  <span className="grid size-10 place-items-center rounded-full border border-primary-border bg-primary-soft text-xs font-bold text-primary">SM</span>
                </div>
                <div className="grid grid-cols-3 border-b border-slate-200 py-4">
                  {[['2', 'Activos'], ['1', 'Pendiente'], ['75 %', 'Progreso']].map(([value, label], index) => (
                    <div key={label} className={index ? 'border-l border-slate-200 pl-4' : ''}>
                      <p className="text-xl font-semibold text-foreground">{value}</p>
                      <p className="mt-1 text-xs text-muted-dark">{label}</p>
                    </div>
                  ))}
                </div>
                <div className="course-art mt-5 min-h-48 border border-slate-200 p-5">
                  <p className="relative z-10 text-[10px] font-bold uppercase tracking-[0.2em] text-orange-300">Continuar aprendiendo</p>
                  <p className="relative z-10 mt-4 max-w-xs text-2xl font-semibold leading-tight">Fundamentos de UX Design</p>
                  <p className="relative z-10 mt-2 text-xs text-muted">Módulo 3 de 4 · Prof. Lucas Fernández</p>
                  <div className="relative z-10 mt-8 flex items-center gap-3">
                    <ProgressBar value={50} className="flex-1" />
                    <span className="text-xs font-bold text-primary">50 %</span>
                  </div>
                </div>
              </div>
              <div className="absolute bottom-10 right-0 space-y-5 text-xs font-semibold uppercase tracking-[0.58em] text-foreground-secondary">
                <p>Aprender</p>
                <p>Conectar</p>
                <p>Crecer</p>
                <div className="section-line w-40" />
              </div>
            </motion.div>
          </div>
        </section>

        <section id="cursos" className="border-t border-slate-200 bg-background-secondary px-5 py-24 sm:px-8">
          <motion.div
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true, amount: 0.15 }}
            variants={reveal}
            transition={{ duration: 0.48 }}
            className="mx-auto max-w-7xl"
          >
            <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <p className="eyebrow">Cursos destacados</p>
                <h2 className="text-balance mt-4 max-w-3xl text-4xl font-semibold leading-tight tracking-[-0.045em] sm:text-5xl">Un próximo paso para cada interés.</h2>
                <p className="mt-4 max-w-2xl text-base leading-7 text-muted">Recorridos claros, organizados por módulos y listos para avanzar a tu propio ritmo.</p>
              </div>
              <Link to="/cursos" className="btn-secondary shrink-0">Ver catálogo <ArrowRight size={16} /></Link>
            </div>
            <div className="mt-12 grid gap-5 md:grid-cols-2 lg:grid-cols-3">
              {featured.map((course) => (
                <CourseCard key={course.id} course={course} href="/login" actionLabel="Conocer el curso" />
              ))}
            </div>
          </motion.div>
        </section>

        <section id="plataforma" className="px-5 py-24 sm:px-8">
          <div className="mx-auto max-w-7xl">
            <div className="grid gap-14 lg:grid-cols-[.75fr_1.25fr] lg:items-start">
              <motion.div initial="hidden" whileInView="visible" viewport={{ once: true, amount: 0.2 }} variants={reveal} transition={{ duration: 0.48 }}>
                <p className="eyebrow">Una experiencia coordinada</p>
                <h2 className="text-balance mt-4 text-4xl font-semibold leading-tight tracking-[-0.045em] sm:text-5xl">Claridad para aprender y gestionar.</h2>
                <p className="mt-6 text-base leading-8 text-muted">
                  Cada rol encuentra lo que necesita sin ruido: cursos y progreso para alumnos, contenidos y seguimiento para profesores, revisiones y gestión para administradores.
                </p>
                <Link to="/registro" className="btn-primary mt-8">Crear mi cuenta <ArrowRight size={17} /></Link>
              </motion.div>

              <div className="border-t border-slate-300">
                {[
                  [BookOpen, '01', 'Aprendizaje organizado', 'Cursos estructurados por módulos con recursos y progreso visible.'],
                  [ClipboardCheck, '02', 'Inscripciones claras', 'Solicitudes con estados precisos y acceso únicamente tras aprobación.'],
                  [LayoutDashboard, '03', 'Gestión simple', 'Paneles enfocados, seguimiento y decisiones fáciles de revisar.'],
                  [Users, '04', 'Roles coordinados', 'Una plataforma coherente para alumnos, profesores y administradores.'],
                ].map(([Icon, number, title, text]) => {
                  const ItemIcon = Icon as typeof BookOpen
                  return (
                    <motion.article
                      key={String(title)}
                      initial={{ opacity: 0, y: 12 }}
                      whileInView={{ opacity: 1, y: 0 }}
                      viewport={{ once: true, amount: 0.4 }}
                      whileHover={{ x: 4 }}
                      className="group grid gap-4 border-b border-slate-200 py-6 sm:grid-cols-[48px_1fr_1.25fr] sm:items-center"
                    >
                      <span className="text-xs font-bold tracking-[0.2em] text-primary">{String(number)}</span>
                      <h3 className="flex items-center gap-3 text-lg font-bold text-foreground"><ItemIcon size={19} className="text-muted-dark group-hover:text-primary" /> {String(title)}</h3>
                      <p className="text-sm leading-6 text-muted">{String(text)}</p>
                    </motion.article>
                  )
                })}
              </div>
            </div>
          </div>
        </section>

        <section className="px-5 pb-24 sm:px-8">
          <motion.div
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true, amount: 0.3 }}
            variants={reveal}
            transition={{ duration: 0.48 }}
            className="page-grid relative mx-auto max-w-7xl overflow-hidden border-y border-slate-300 bg-surface px-6 py-16 sm:px-12"
          >
            <LumenOrb className="-right-60 -top-64" />
            <div className="relative z-10 max-w-3xl">
              <p className="eyebrow">El próximo paso</p>
              <h2 className="mt-5 text-balance text-4xl font-semibold leading-tight tracking-[-0.045em] sm:text-5xl">Tu próximo aprendizaje puede empezar <span className="text-primary">hoy.</span></h2>
              <p className="mt-4 max-w-xl text-base leading-7 text-muted">Explorá cursos publicados y encontrá un recorrido que acompañe tu progreso.</p>
              <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                <Link to="/cursos" className="btn-primary">Explorar cursos <ArrowRight size={17} /></Link>
                <Link to="/login" className="btn-secondary">Iniciar sesión</Link>
              </div>
            </div>
          </motion.div>
        </section>
      </main>

      <footer className="border-t border-slate-200 px-5 py-8 sm:px-8">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <Logo />
          <p className="text-sm text-muted">Lumen — Sistema de Gestión de Cursos Online · 2026</p>
        </div>
      </footer>
    </div>
  )
}

