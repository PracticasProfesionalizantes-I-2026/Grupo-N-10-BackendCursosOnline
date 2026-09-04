import { AnimatePresence, motion } from 'framer-motion'
import { ArrowLeft, ArrowRight, Check, Clock3, Plus, Save, Send, Trash2 } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { Course, CourseDraft, Module } from '@/types'
import { formatDuration, uid } from '@/utils/format'

const emptyModule = (): Module => ({
  id: uid('module'),
  name: '',
  description: '',
  durationHours: 1,
  resources: [''],
})

const defaultDraft: CourseDraft = {
  title: '',
  description: '',
  category: 'Desarrollo',
  level: 'Inicial',
  modality: 'Autogestionado',
  maxCapacity: 30,
  objectives: [],
  prerequisites: [],
  modules: [emptyModule()],
  durationHours: 1,
}

const lines = (value: string) => value.split('\n').map((item) => item.trim()).filter(Boolean)

export function CourseForm({
  initial,
  onSave,
}: {
  initial?: Course
  onSave: (draft: CourseDraft, submit: boolean) => void
}) {
  const [step, setStep] = useState(1)
  const [draft, setDraft] = useState<CourseDraft>(() =>
    initial
      ? {
          title: initial.title,
          description: initial.description,
          category: initial.category,
          level: initial.level,
          modality: initial.modality,
          maxCapacity: initial.maxCapacity,
          objectives: initial.objectives,
          prerequisites: initial.prerequisites,
          modules: initial.modules.map((item) => ({ ...item, resources: [...item.resources] })),
          durationHours: initial.durationHours,
        }
      : defaultDraft,
  )
  const [errors, setErrors] = useState<string[]>([])
  const duration = useMemo(() => draft.modules.reduce((sum, module) => sum + Number(module.durationHours || 0), 0), [draft.modules])
  const normalized = (): CourseDraft => ({ ...draft, durationHours: duration })

  const setField = <K extends keyof CourseDraft>(field: K, value: CourseDraft[K]) => {
    setDraft((current) => ({ ...current, [field]: value }))
    setErrors([])
  }

  const updateModule = (id: string, field: keyof Module, value: string | number | string[]) => {
    setDraft((current) => ({
      ...current,
      modules: current.modules.map((module) => (module.id === id ? { ...module, [field]: value } : module)),
    }))
    setErrors([])
  }

  const validate = () => {
    const next: string[] = []
    if (!draft.title.trim()) next.push('Ingresá el título del curso.')
    if (!draft.category.trim()) next.push('Seleccioná una categoría.')
    if (!draft.level.trim()) next.push('Seleccioná un nivel.')
    if (!draft.modality.trim()) next.push('Seleccioná una modalidad.')
    if (draft.maxCapacity <= 0) next.push('El cupo máximo debe ser mayor a cero.')
    if (!draft.modules.length) next.push('Agregá al menos un módulo.')
    draft.modules.forEach((module, index) => {
      if (!module.name.trim()) next.push('Completá el nombre del módulo ' + (index + 1) + '.')
      if (module.durationHours <= 0) next.push('La duración del módulo ' + (index + 1) + ' debe ser mayor a cero.')
      if (!module.resources.some((resource) => resource.trim())) next.push('Agregá contenido o recursos al módulo ' + (index + 1) + '.')
    })
    setErrors(next)
    return next.length === 0
  }

  const submit = () => {
    if (!validate()) {
      setStep(draft.title.trim() ? 2 : 1)
      return
    }
    onSave(normalized(), true)
  }

  return (
    <div className="space-y-6">
      <ol className="panel flex items-center gap-3 overflow-x-auto" aria-label="Pasos del curso">
        {[
          [1, 'Información'],
          [2, 'Módulos'],
          [3, 'Revisión'],
        ].map(([number, label], index) => (
          <li key={String(label)} className="flex min-w-0 flex-1 items-center gap-3">
            <button
              onClick={() => setStep(number as number)}
              className={'flex min-w-max items-center gap-2 rounded-xl px-3 py-2 text-sm font-bold transition ' + (step === number ? 'bg-primary-soft text-primary' : Number(number) < step ? 'text-emerald-300' : 'text-slate-400')}
            >
              <span className={'grid size-8 place-items-center rounded-full ' + (Number(number) < step ? 'bg-emerald-500/10' : step === number ? 'bg-primary text-white' : 'bg-surface-raised')}>
                {Number(number) < step ? <Check size={15} /> : number}
              </span>
              {label}
            </button>
            {index < 2 && <span className="h-px flex-1 bg-surface-hover" />}
          </li>
        ))}
      </ol>

      {errors.length > 0 && (
        <div role="alert" className="rounded-xl border border-red-500/25 bg-red-500/10 p-4 text-sm text-red-300">
          <p className="font-bold">Revisá la información antes de enviar:</p>
          <ul className="mt-2 list-disc space-y-1 pl-5">{errors.map((error) => <li key={error}>{error}</li>)}</ul>
        </div>
      )}

      <AnimatePresence mode="wait">
        <motion.section key={step} initial={{ opacity: 0, x: 10 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -10 }} transition={{ duration: 0.18 }} className="panel">
          {step === 1 && (
            <>
              <div className="mb-6">
                <p className="eyebrow">Información general</p>
                <h2 className="mt-2 text-2xl font-bold text-foreground">Presentá el curso con claridad</h2>
              </div>
              <div className="grid gap-5 md:grid-cols-2">
                <label className="block md:col-span-2">
                  <span className="label">Título *</span>
                  <input className="input" value={draft.title} onChange={(event) => setField('title', event.target.value)} placeholder="Ej. Diseño de productos digitales" />
                </label>
                <label className="block md:col-span-2">
                  <span className="label">Descripción</span>
                  <textarea className="textarea" value={draft.description} onChange={(event) => setField('description', event.target.value)} placeholder="Explicá qué aprenderán los alumnos." />
                </label>
                <label className="block">
                  <span className="label">Categoría *</span>
                  <select className="select" value={draft.category} onChange={(event) => setField('category', event.target.value)}>
                    {['Desarrollo', 'Diseño', 'Datos', 'Herramientas'].map((item) => <option key={item}>{item}</option>)}
                  </select>
                </label>
                <label className="block">
                  <span className="label">Nivel *</span>
                  <select className="select" value={draft.level} onChange={(event) => setField('level', event.target.value)}>
                    {['Inicial', 'Intermedio', 'Avanzado'].map((item) => <option key={item}>{item}</option>)}
                  </select>
                </label>
                <label className="block">
                  <span className="label">Modalidad *</span>
                  <select className="select" value={draft.modality} onChange={(event) => setField('modality', event.target.value)}>
                    {['Autogestionado', 'Asincrónico'].map((item) => <option key={item}>{item}</option>)}
                  </select>
                </label>
                <label className="block">
                  <span className="label">Cupo máximo *</span>
                  <input className="input" type="number" min={1} value={draft.maxCapacity} onChange={(event) => setField('maxCapacity', Number(event.target.value))} />
                </label>
                <label className="block">
                  <span className="label">Objetivos de aprendizaje</span>
                  <textarea className="textarea" value={draft.objectives.join('\n')} onChange={(event) => setField('objectives', lines(event.target.value))} placeholder={'Un objetivo por línea'} />
                </label>
                <label className="block">
                  <span className="label">Requisitos previos sugeridos</span>
                  <textarea className="textarea" value={draft.prerequisites.join('\n')} onChange={(event) => setField('prerequisites', lines(event.target.value))} placeholder={'Un requisito por línea'} />
                </label>
              </div>
            </>
          )}

          {step === 2 && (
            <>
              <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
                <div>
                  <p className="eyebrow">Contenido</p>
                  <h2 className="mt-2 text-2xl font-bold text-foreground">Organizá los módulos</h2>
                  <p className="mt-1 text-sm text-slate-500">La duración total se calcula automáticamente.</p>
                </div>
                <div className="rounded-xl bg-primary-soft px-4 py-3 text-orange-200">
                  <p className="text-[10px] font-bold uppercase tracking-wider">Duración total</p>
                  <p className="mt-1 flex items-center gap-2 text-lg font-bold"><Clock3 size={18} /> {formatDuration(duration)}</p>
                </div>
              </div>
              <div className="space-y-5">
                {draft.modules.map((module, index) => (
                  <ModuleEditor
                    key={module.id}
                    module={module}
                    index={index}
                    canDelete={draft.modules.length > 1}
                    onChange={updateModule}
                    onDelete={() => setDraft((current) => ({ ...current, modules: current.modules.filter((item) => item.id !== module.id) }))}
                  />
                ))}
              </div>
              <button className="btn-secondary mt-5 w-full border-dashed" onClick={() => setDraft((current) => ({ ...current, modules: [...current.modules, emptyModule()] }))}>
                <Plus size={17} /> Agregar módulo
              </button>
            </>
          )}

          {step === 3 && (
            <>
              <div className="mb-6">
                <p className="eyebrow">Revisión</p>
                <h2 className="mt-2 text-2xl font-bold text-foreground">Todo listo para enviar</h2>
                <p className="mt-1 text-sm text-slate-500">Revisá la síntesis. El curso quedará EN REVISIÓN hasta la decisión administrativa.</p>
              </div>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                {[
                  ['Título', draft.title || 'Sin título'],
                  ['Categoría', draft.category],
                  ['Módulos', String(draft.modules.length)],
                  ['Duración', formatDuration(duration)],
                ].map(([label, value]) => (
                  <div key={label} className="rounded-xl border border-slate-200 bg-background-secondary p-4">
                    <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{label}</p>
                    <p className="mt-1.5 font-bold text-slate-800">{value}</p>
                  </div>
                ))}
              </div>
              <div className="mt-5 rounded-xl border border-primary-border bg-primary-soft p-4 text-sm leading-6 text-orange-200">
                Los cambios no se publican automáticamente. Un administrador debe aprobar la auditoría.
              </div>
            </>
          )}
        </motion.section>
      </AnimatePresence>

      <div className="sticky bottom-4 z-10 flex flex-col-reverse gap-3 rounded-xl border border-slate-300 bg-background-secondary/95 p-3 shadow-soft backdrop-blur sm:flex-row sm:items-center sm:justify-between">
        <button className="btn-secondary" onClick={() => onSave(normalized(), false)}><Save size={17} /> Guardar como borrador</button>
        <div className="flex gap-2">
          {step > 1 && <button className="btn-ghost flex-1 sm:flex-none" onClick={() => setStep((value) => value - 1)}><ArrowLeft size={16} /> Atrás</button>}
          {step < 3 ? (
            <button className="btn-primary flex-1 sm:flex-none" onClick={() => setStep((value) => value + 1)}>Continuar <ArrowRight size={16} /></button>
          ) : (
            <button className="btn-primary flex-1 sm:flex-none" onClick={submit}><Send size={17} /> Enviar a revisión</button>
          )}
        </div>
      </div>
    </div>
  )
}

function ModuleEditor({
  module,
  index,
  canDelete,
  onChange,
  onDelete,
}: {
  module: Module
  index: number
  canDelete: boolean
  onChange: (id: string, field: keyof Module, value: string | number | string[]) => void
  onDelete: () => void
}) {
  return (
    <article className="rounded-xl border border-slate-200 bg-background-secondary/50 p-4 sm:p-5">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="grid size-9 place-items-center rounded-lg border border-primary-border bg-primary-soft text-sm font-bold text-primary">{index + 1}</span>
          <h3 className="font-bold text-foreground">Módulo {index + 1}</h3>
        </div>
        <button className="btn-ghost min-h-10 px-3 text-red-500 hover:bg-red-500/10 hover:text-red-300" onClick={onDelete} disabled={!canDelete} aria-label={'Eliminar módulo ' + (index + 1)}>
          <Trash2 size={17} />
        </button>
      </div>
      <div className="grid gap-4 md:grid-cols-[1fr_180px]">
        <label className="block">
          <span className="label">Nombre *</span>
          <input className="input" value={module.name} onChange={(event) => onChange(module.id, 'name', event.target.value)} placeholder="Nombre del módulo" />
        </label>
        <label className="block">
          <span className="label">Duración (horas) *</span>
          <input className="input" type="number" min={0.5} step={0.5} value={module.durationHours} onChange={(event) => onChange(module.id, 'durationHours', Number(event.target.value))} />
        </label>
        <label className="block md:col-span-2">
          <span className="label">Descripción</span>
          <textarea className="textarea min-h-20" value={module.description} onChange={(event) => onChange(module.id, 'description', event.target.value)} placeholder="Qué trabajarán los alumnos en este módulo." />
        </label>
        <label className="block md:col-span-2">
          <span className="label">Contenido / recursos *</span>
          <textarea className="textarea min-h-20" value={module.resources.join('\n')} onChange={(event) => onChange(module.id, 'resources', event.target.value.split('\n'))} placeholder={'Un recurso por línea\nEj. Lectura introductoria\nEj. Video práctico'} />
        </label>
      </div>
    </article>
  )
}
