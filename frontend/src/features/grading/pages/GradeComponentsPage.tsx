import { useEffect, useState } from 'react'
import { ListChecks, Lock, Pencil, Plus, Trash2 } from 'lucide-react'
import { useCourses } from '@/features/courses/hooks'
import { useTerms } from '@/features/terms/hooks'
import { errorMessage } from '@/shared/api/problem'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { GradeComponentFormDialog } from '../components/GradeComponentFormDialog'
import { num } from '../components/gradingForms'
import { WeightBar } from '../components/WeightBar'
import { useDeleteGradeComponent, useGradeComponents } from '../hooks'
import type { GradeComponent } from '../types'

type DialogState = { open: false } | { open: true; component?: GradeComponent }

/** PB-08 / T31: the admin defines each course's grade components per term (name, max grade, weight; total ≤ 100%). */
export function GradeComponentsPage() {
  const terms = useTerms()
  const courses = useCourses({ pageSize: 100 })
  const [termId, setTermId] = useState<string | null>(null)
  const [courseId, setCourseId] = useState('')
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const [deleting, setDeleting] = useState<GradeComponent | null>(null)
  const remove = useDeleteGradeComponent()
  const saved = useSavedMessage()

  useEffect(() => {
    if (termId === null && terms.data) setTermId(String(terms.data.find((t) => t.isCurrent)?.id ?? terms.data[0]?.id ?? ''))
  }, [termId, terms.data])

  const list = useGradeComponents(courseId ? Number(courseId) : undefined, termId ? Number(termId) : undefined)
  const course = courses.data?.items.find((c) => String(c.id) === courseId)
  const items = list.data?.items ?? []
  const total = list.data?.totalWeight ?? 0
  const locked = list.data?.isFinalized ?? false
  const weightLeft = (editing?: GradeComponent) => 100 - total + (editing?.weight ?? 0)
  const term = terms.data?.find((t) => String(t.id) === termId)

  // Why "New component" can't be used right now, shown under the button (a disabled button alone says nothing).
  const blocked =
    !course || !termId ? 'Choose a course first.'
    : list.isPending ? null
    : locked ? 'Locked: grades are finalized.'
    : total >= 100 ? 'Weights already total 100%.'
    : null
  const newDisabled = !course || !termId || list.isPending || locked || total >= 100

  const confirmDelete = () => {
    if (!deleting) return
    remove.mutate(deleting.id, {
      onSuccess: () => {
        saved.show(`${deleting.name} deleted.`)
        setDeleting(null)
      },
    })
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={term?.name ?? 'Academics'}
        title="Grade components"
        icon={ListChecks}
        actions={
          <div className="flex flex-col gap-1.5 sm:items-end">
            <Button
              onClick={() => setDialog({ open: true })}
              disabled={newDisabled}
              aria-describedby={blocked ? 'new-component-hint' : undefined}
            >
              <Plus className="size-4" aria-hidden /> New component
            </Button>
            {blocked && (
              <p id="new-component-hint" className="text-[13px] text-muted">
                {blocked}
              </p>
            )}
          </div>
        }
      >
        What each course is graded on this term, out of how much, and how much each part weighs.
      </PageHeader>

      {saved.message && <Alert tone="success">{saved.message}</Alert>}

      <Card>
        <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-2 sm:px-5">
          <SelectField label="Term" value={termId ?? ''} onChange={(e) => setTermId(e.target.value)} disabled={!terms.data}>
            {terms.isPending && <option value="">Loading…</option>}
            {terms.data?.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
                {t.isCurrent ? ' (current)' : ''}
              </option>
            ))}
          </SelectField>
          <SelectField label="Course" value={courseId} onChange={(e) => setCourseId(e.target.value)} disabled={!courses.data}>
            <option value="">{courses.isPending ? 'Loading…' : 'Choose a course'}</option>
            {courses.data?.items.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} · {c.name}
              </option>
            ))}
          </SelectField>
        </div>

        {terms.isError || courses.isError ? (
          <QueryError
            error={terms.error ?? courses.error}
            onRetry={() => {
              void terms.refetch()
              void courses.refetch()
            }}
          />
        ) : !courseId ? (
          <EmptyState icon={ListChecks} title="Choose a course">
            Pick a term and a course to see and edit its grade components.
          </EmptyState>
        ) : list.isPending ? (
          <TableSkeleton label="Loading grade components" />
        ) : list.isError ? (
          <QueryError error={list.error} onRetry={() => list.refetch()} />
        ) : (
          <>
            <div className="space-y-2 border-b border-line p-4 sm:px-5">
              <WeightBar total={total} />
              {locked && (
                <p className="flex items-center gap-1.5 text-sm text-muted">
                  <Lock className="size-4" aria-hidden /> Grades for this course were finalized this term, so its components are locked.
                </p>
              )}
            </div>
            {items.length === 0 ? (
              <EmptyState
                icon={ListChecks}
                title="No components yet"
                action={
                  !locked && (
                    <Button onClick={() => setDialog({ open: true })}>
                      <Plus className="size-4" aria-hidden /> New component
                    </Button>
                  )
                }
              >
                Add the parts {course?.code} is graded on this term, e.g. Midterm 30%, Project 20%, Final 50%.
              </EmptyState>
            ) : (
              <TableScroll>
                <table className="w-full min-w-[560px] text-left text-sm">
                  <thead className="border-b border-line bg-canvas/60 text-xs uppercase tracking-wide text-muted">
                    <tr>
                      <th scope="col" className="px-4 py-3 font-medium">Component</th>
                      <th scope="col" className="px-4 py-3 text-right font-medium">Max grade</th>
                      <th scope="col" className="px-4 py-3 text-right font-medium">Weight</th>
                      <th scope="col" className="px-4 py-3 font-medium">Scores</th>
                      <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-line">
                    {items.map((c) => (
                      <tr key={c.id} className="transition-colors hover:bg-canvas/50">
                        <th scope="row" className="px-4 py-3 font-medium text-ink">{c.name}</th>
                        <td className="tabular px-4 py-3 text-right">{num(c.maxGrade)}</td>
                        <td className="tabular px-4 py-3 text-right">{num(c.weight)}%</td>
                        <td className="px-4 py-3">
                          {c.scoreCount > 0 ? <Badge tone="info">{c.scoreCount} entered</Badge> : <span className="text-muted">None yet</span>}
                        </td>
                        <td className="whitespace-nowrap px-4 py-3 text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            disabled={locked}
                            onClick={() => setDialog({ open: true, component: c })}
                            aria-label={`Edit ${c.name}`}
                          >
                            <Pencil className="size-4" aria-hidden /> Edit
                          </Button>
                          {/* A component with scores can't be deleted (the API says 409); the button says so up front. */}
                          <Button
                            variant="danger-ghost"
                            size="sm"
                            disabled={locked || c.scoreCount > 0}
                            onClick={() => {
                              remove.reset()
                              setDeleting(c)
                            }}
                            aria-label={c.scoreCount > 0 ? `Delete ${c.name}: not possible, it has scores` : `Delete ${c.name}`}
                          >
                            <Trash2 className="size-4" aria-hidden /> {c.scoreCount > 0 ? 'Has scores' : 'Delete'}
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </TableScroll>
            )}
          </>
        )}
      </Card>

      {course && termId && (
        <GradeComponentFormDialog
          open={dialog.open}
          courseId={course.id}
          termId={Number(termId)}
          courseCode={course.code}
          weightLeft={weightLeft(dialog.open ? dialog.component : undefined)}
          component={dialog.open ? dialog.component : undefined}
          onClose={() => setDialog({ open: false })}
          onSaved={(c) => {
            saved.show(`${c.name} saved.`)
            setDialog({ open: false })
          }}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title={`Delete ${deleting?.name ?? ''}?`}
        confirmLabel="Delete"
        loading={remove.isPending}
        error={remove.isError ? errorMessage(remove.error) : null}
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      >
        The component is removed from {course?.code}. This only works while no scores have been entered for it.
      </ConfirmDialog>
    </div>
  )
}
