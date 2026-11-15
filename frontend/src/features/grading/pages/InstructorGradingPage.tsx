import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { Check, CheckCheck, ListChecks, Users } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { useUnsavedChanges } from '@/shared/lib/useUnsavedChanges'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { num } from '../components/gradingForms'
import { ScoreSheet } from '../components/ScoreSheet'
import { useFinalizeSection, useGradingSection, useRoster, useTeachingSections } from '../hooks'

/**
 * PB-08 / T34: section → component → score per student, existing scores filled in; then finalize the section's grades.
 * `?section=<id>` opens a section directly (the teaching schedule links here).
 */
export function InstructorGradingPage() {
  const sections = useTeachingSections()
  const [params] = useSearchParams()
  const [sectionId, setSectionId] = useState<number | undefined>(() => Number(params.get('section')) || undefined)
  const [componentId, setComponentId] = useState<number | undefined>()
  const [confirming, setConfirming] = useState(false)
  const [dirty, setDirty] = useState(false)
  const unsaved = useUnsavedChanges(dirty)
  const saved = useSavedMessage()
  const finalize = useFinalizeSection()

  // Start on the linked (or first) section, and on the first component of the chosen section.
  useEffect(() => {
    const list = sections.data
    if (list?.length && !list.some((s) => s.sectionId === sectionId)) setSectionId(list[0]!.sectionId)
  }, [sectionId, sections.data])
  const section = useGradingSection(sectionId)
  useEffect(() => {
    const components = section.data?.components ?? []
    if (section.data && !components.some((c) => c.id === componentId)) setComponentId(components[0]?.id)
  }, [section.data, componentId])
  const roster = useRoster(sectionId, componentId)

  const current = sections.data?.find((s) => s.sectionId === sectionId)
  const label = current ? `${current.courseCode} ${current.sectionCode}` : ''
  const weightsOff = !!section.data && section.data.totalWeight !== 100
  const missing = section.data?.notGraded ?? 0

  const onFinalize = () => {
    if (!sectionId) return
    finalize.mutate(sectionId, {
      onSuccess: (r) => {
        setConfirming(false)
        saved.show(`Grades finalized for ${r.students.length} student${r.students.length === 1 ? '' : 's'} in ${label}.`)
      },
    })
  }

  const finalizeAction = section.data ? (
    section.data.isFinalized ? (
      <Badge tone="success" dot>
        Finalized
      </Badge>
    ) : (
      <Button
        variant="secondary"
        size="sm"
        disabled={section.data.studentCount === 0 || section.data.components.length === 0 || weightsOff || dirty}
        aria-describedby={weightsOff || dirty ? 'finalize-reason' : undefined}
        onClick={() => {
          finalize.reset()
          setConfirming(true)
        }}
      >
        <CheckCheck className="size-4" aria-hidden /> Finalize grades
      </Button>
    )
  ) : undefined

  return (
    <>
      <PageHeader eyebrow={current?.termName ?? 'Teaching'} title="Grading" icon={ListChecks}>
        Choose a section and a component, then enter each student&apos;s score. Finalize when every score is in.
      </PageHeader>

      {saved.message && (
        <div className="mb-6">
          <Alert tone="success">{saved.message}</Alert>
        </div>
      )}

      {sections.isPending ? (
        <Card>
          <TableSkeleton label="Loading your sections" />
        </Card>
      ) : sections.isError ? (
        <Card>
          <QueryError error={sections.error} onRetry={() => sections.refetch()} />
        </Card>
      ) : sections.data.length === 0 ? (
        <Card>
          <EmptyState icon={Users} title="No sections to grade">
            You don&apos;t teach any section this term. Sections appear here once the admin assigns them to you.
          </EmptyState>
        </Card>
      ) : (
        <Card>
          <CardHeader
            title={current ? `${label} · ${current.courseName}` : 'Section'}
            description={
              section.data ? (
                <span className="tabular">
                  {section.data.studentCount} student{section.data.studentCount === 1 ? '' : 's'} · weights total{' '}
                  {num(section.data.totalWeight)}%
                </span>
              ) : undefined
            }
            actions={finalizeAction}
          />

          <div className="space-y-4 border-b border-line p-4 sm:px-5">
            <div className="max-w-xl">
              <SelectField
                label="Section"
                value={sectionId ?? ''}
                onChange={(e) => {
                  const id = Number(e.target.value)
                  unsaved.guard(() => {
                    setSectionId(id)
                    setComponentId(undefined)
                  })
                }}
              >
                {sections.data.map((s) => (
                  <option key={s.sectionId} value={s.sectionId}>
                    {s.courseCode} · {s.sectionCode} — {s.courseName}
                    {s.isFinalized ? ' (finalized)' : s.notGraded > 0 ? ` (${s.notGraded} not graded)` : ''}
                  </option>
                ))}
              </SelectField>
            </div>
            {section.data && !section.data.isFinalized && (weightsOff || dirty) && (
              <p id="finalize-reason" className="text-sm text-warning">
                {weightsOff
                  ? `Finalizing is off: the components add up to ${num(section.data.totalWeight)}%, not 100%. Ask the admin to complete them.`
                  : 'Save the scores you typed before finalizing.'}
              </p>
            )}
          </div>

          {section.isPending ? (
            <TableSkeleton label="Loading components" />
          ) : section.isError ? (
            <QueryError error={section.error} onRetry={() => section.refetch()} />
          ) : section.data.components.length === 0 ? (
            <EmptyState icon={ListChecks} title="No grade components yet">
              The admin hasn&apos;t set up the grade components for {section.data.courseCode} this term. Scores can be entered once
              they exist.
            </EmptyState>
          ) : (
            <>
              <div className="border-b border-line p-4 sm:px-5">
                <Tabs
                  label="Grade components"
                  value={String(componentId ?? '')}
                  onChange={(v) => unsaved.guard(() => setComponentId(Number(v)))}
                  items={section.data.components.map((c) => {
                    const name = `${c.name} · ${num(c.weight)}%`
                    const done = section.data.studentCount > 0 && c.notGraded === 0
                    return {
                      value: String(c.id),
                      ariaLabel: `${name}, ${done ? 'everyone graded' : `${c.notGraded} not graded`}`,
                      label: (
                        <>
                          {name}
                          {done && <Check className="size-3.5 text-success" aria-hidden />}
                        </>
                      ),
                      count: done ? undefined : c.notGraded,
                    }
                  })}
                />
              </div>
              {section.data.studentCount === 0 ? (
                <EmptyState icon={Users} title="No students yet">
                  Nobody is enrolled in {label}. Scores can be entered once students enroll.
                </EmptyState>
              ) : roster.isPending ? (
                <TableSkeleton label="Loading students" />
              ) : roster.isError ? (
                <QueryError error={roster.error} onRetry={() => roster.refetch()} />
              ) : (
                <ScoreSheet key={roster.data.component.id} roster={roster.data} onDirtyChange={setDirty} />
              )}
            </>
          )}
        </Card>
      )}

      <ConfirmDialog
        open={confirming}
        title={`Finalize grades for ${label}?`}
        confirmLabel="Finalize"
        loading={finalize.isPending}
        error={finalize.isError ? errorMessage(finalize.error) : null}
        onConfirm={onFinalize}
        onClose={() => setConfirming(false)}
      >
        <p>
          Each student gets their final percentage and letter grade, and their GPA is updated. Scores can&apos;t be changed afterwards.
        </p>
        {missing > 0 && (
          <p className="mt-3 font-medium text-warning">
            {missing} score{missing === 1 ? ' is' : 's are'} still missing. Enter every score, 0 included, before finalizing.
          </p>
        )}
      </ConfirmDialog>

      <ConfirmDialog
        open={unsaved.pending}
        title="Discard unsaved scores?"
        confirmLabel="Discard scores"
        onConfirm={unsaved.confirm}
        onClose={unsaved.cancel}
      >
        The scores you typed for this component haven&apos;t been saved. Switching now loses them.
      </ConfirmDialog>
    </>
  )
}
