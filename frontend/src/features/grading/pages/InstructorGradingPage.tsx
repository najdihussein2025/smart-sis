import { useEffect, useState } from 'react'
import { CheckCheck, ListChecks, Users } from 'lucide-react'
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
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { num } from '../components/gradingForms'
import { ScoreSheet } from '../components/ScoreSheet'
import { useFinalizeSection, useGradingSection, useRoster, useTeachingSections } from '../hooks'

/** PB-08 / T34: section → component → score per student, existing scores filled in; then finalize the section's grades. */
export function InstructorGradingPage() {
  const sections = useTeachingSections()
  const [sectionId, setSectionId] = useState<number | undefined>()
  const [componentId, setComponentId] = useState<number | undefined>()
  const [confirming, setConfirming] = useState(false)
  const saved = useSavedMessage()
  const finalize = useFinalizeSection()

  // Start on the first section, and on the first component of the chosen section.
  useEffect(() => {
    if (sectionId === undefined && sections.data?.length) setSectionId(sections.data[0]!.sectionId)
  }, [sectionId, sections.data])
  const section = useGradingSection(sectionId)
  useEffect(() => {
    const components = section.data?.components ?? []
    if (section.data && !components.some((c) => c.id === componentId)) setComponentId(components[0]?.id)
  }, [section.data, componentId])
  const roster = useRoster(sectionId, componentId)

  const current = sections.data?.find((s) => s.sectionId === sectionId)
  const label = current ? `${current.courseCode} ${current.sectionCode}` : ''

  const onFinalize = () => {
    if (!sectionId) return
    finalize.mutate(sectionId, {
      onSuccess: (r) => {
        setConfirming(false)
        saved.show(`Grades finalized for ${r.students.length} student${r.students.length === 1 ? '' : 's'} in ${label}.`)
      },
    })
  }

  return (
    <>
      <PageHeader eyebrow="Teaching" title="Grading">
        Choose a section and a component, then enter each student&apos;s score. Finalize when every score is in.
      </PageHeader>

      {saved.message && (
        <div className="mb-4">
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
          <div className="flex flex-wrap items-end gap-4 border-b border-line p-4">
            <div className="min-w-64 flex-1">
              <SelectField
                label="Section"
                value={sectionId ?? ''}
                onChange={(e) => {
                  setSectionId(Number(e.target.value))
                  setComponentId(undefined)
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
            {section.data && (
              <div className="flex flex-wrap items-center gap-3 pb-1">
                <span className="text-sm text-muted">
                  {section.data.studentCount} student{section.data.studentCount === 1 ? '' : 's'}
                </span>
                {section.data.isFinalized ? (
                  <Badge tone="success">Finalized</Badge>
                ) : (
                  <Button
                    variant="secondary"
                    disabled={section.data.studentCount === 0 || section.data.components.length === 0}
                    onClick={() => {
                      finalize.reset()
                      setConfirming(true)
                    }}
                  >
                    <CheckCheck className="size-4" aria-hidden /> Finalize grades
                  </Button>
                )}
              </div>
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
              <div className="space-y-2 border-b border-line p-4">
                <Tabs
                  label="Grade components"
                  value={String(componentId ?? '')}
                  onChange={(v) => setComponentId(Number(v))}
                  items={section.data.components.map((c) => ({ value: String(c.id), label: `${c.name} · ${num(c.weight)}%` }))}
                />
                {!section.data.isFinalized && section.data.totalWeight !== 100 && (
                  <p className="text-sm text-warning">
                    The components add up to {num(section.data.totalWeight)}%, not 100%. Grades can be finalized once the admin completes
                    them.
                  </p>
                )}
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
                <ScoreSheet key={roster.data.component.id} roster={roster.data} onSaved={saved.show} />
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
        Each student gets their final percentage and letter grade, and their GPA is updated. Scores can&apos;t be changed afterwards.
      </ConfirmDialog>
    </>
  )
}
