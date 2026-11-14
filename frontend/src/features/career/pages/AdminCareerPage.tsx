import { useState } from 'react'
import { Compass, ListChecks, Pencil, Plus, Tags, Trash2 } from 'lucide-react'
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
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { CareerFormDialog } from '../components/CareerFormDialog'
import { QuestionFormDialog } from '../components/QuestionFormDialog'
import { CourseTagsDialog, TraitFormDialog } from '../components/SmallDialogs'
import { useAdminCareers, useCourseTags, useDeleteQuestion, useQuestionBank, useTraits } from '../hooks'
import type { CareerPath, CourseTags, Question, Trait } from '../types'

type Tab = 'questions' | 'traits' | 'careers' | 'tags'

/** PB-16 / T29, PB-20 / T72: the question bank, traits, careers with roadmaps and course-trait tags. */
export function AdminCareerPage() {
  const [tab, setTab] = useState<Tab>('questions')
  const saved = useSavedMessage()

  return (
    <>
      <PageHeader eyebrow="Administration" title="Career">
        The career assessment, the careers students are matched to and their roadmaps.
      </PageHeader>
      {saved.message && (
        <div className="mb-4">
          <Alert tone="success">{saved.message}</Alert>
        </div>
      )}
      <div className="mb-4">
        <Tabs<Tab>
          label="Career setup"
          value={tab}
          onChange={setTab}
          items={[
            { value: 'questions', label: 'Question bank' },
            { value: 'traits', label: 'Traits' },
            { value: 'careers', label: 'Careers & roadmaps' },
            { value: 'tags', label: 'Course tags' },
          ]}
        />
      </div>
      {tab === 'questions' && <QuestionsTab onSaved={saved.show} />}
      {tab === 'traits' && <TraitsTab onSaved={saved.show} />}
      {tab === 'careers' && <CareersTab onSaved={saved.show} />}
      {tab === 'tags' && <CourseTagsTab onSaved={saved.show} />}
    </>
  )
}

type Editing<T> = { open: false } | { open: true; item?: T }

function QuestionsTab({ onSaved }: { onSaved: (m: string) => void }) {
  const bank = useQuestionBank()
  const traits = useTraits()
  const remove = useDeleteQuestion()
  const [editing, setEditing] = useState<Editing<Question>>({ open: false })
  const [deleting, setDeleting] = useState<Question | null>(null)

  return (
    <Card>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-5 py-4">
        <p className="text-sm text-muted">
          {bank.data
            ? `${bank.data.activeCount} active questions. The quiz asks ${bank.data.minQuestions}–${bank.data.maxQuestions} (the first ${bank.data.maxQuestions} by number).`
            : 'Questions students answer in the career assessment.'}
        </p>
        <Button onClick={() => setEditing({ open: true })} disabled={!traits.data}>
          <Plus className="size-4" aria-hidden /> New question
        </Button>
      </div>
      {bank.data && bank.data.activeCount < bank.data.minQuestions && (
        <div className="px-5 pt-4">
          <Alert>
            Students can't take the assessment until at least {bank.data.minQuestions} questions are active ({bank.data.activeCount} now).
          </Alert>
        </div>
      )}
      {bank.isPending ? (
        <TableSkeleton label="Loading questions" />
      ) : bank.isError ? (
        <QueryError error={bank.error} onRetry={() => bank.refetch()} />
      ) : bank.data.questions.length === 0 ? (
        <EmptyState icon={ListChecks} title="No questions yet">
          Add at least {bank.data.minQuestions} questions so students can take the assessment.
        </EmptyState>
      ) : (
        <TableScroll>
          <table className="w-full text-left text-sm">
            <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
              <tr>
                <th scope="col" className="px-5 py-3 font-medium">#</th>
                <th scope="col" className="px-5 py-3 font-medium">Question</th>
                <th scope="col" className="px-5 py-3 font-medium max-sm:hidden">Answered</th>
                <th scope="col" className="px-5 py-3 font-medium">Status</th>
                <th scope="col" className="px-5 py-3"><span className="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {bank.data.questions.map((q) => (
                <tr key={q.id}>
                  <td className="px-5 py-3.5 tabular-nums text-muted">{q.id}</td>
                  <th scope="row" className="px-5 py-3.5 font-normal text-ink">
                    {q.text}
                    <span className="mt-0.5 block text-xs text-muted">{q.options.length} options</span>
                  </th>
                  <td className="px-5 py-3.5 tabular-nums text-muted max-sm:hidden">{q.answerCount}</td>
                  <td className="px-5 py-3.5">
                    {q.isActive ? <Badge tone="success">Active</Badge> : <Badge>Inactive</Badge>}
                  </td>
                  <td className="whitespace-nowrap px-5 py-3.5 text-right">
                    <Button variant="ghost" className="h-8 px-2.5" onClick={() => setEditing({ open: true, item: q })} aria-label={`Edit question ${q.id}`}>
                      <Pencil className="size-4" aria-hidden /> Edit
                    </Button>
                    {q.answerCount === 0 && (
                      <Button variant="ghost" className="h-8 px-2.5" onClick={() => { remove.reset(); setDeleting(q) }} aria-label={`Delete question ${q.id}`}>
                        <Trash2 className="size-4" aria-hidden />
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </TableScroll>
      )}

      <QuestionFormDialog
        open={editing.open}
        question={editing.open ? editing.item : undefined}
        traits={traits.data ?? []}
        onClose={() => setEditing({ open: false })}
        onSaved={() => {
          onSaved('Question saved.')
          setEditing({ open: false })
        }}
      />
      <ConfirmDialog
        open={deleting !== null}
        title="Delete question?"
        confirmLabel="Delete"
        loading={remove.isPending}
        error={remove.isError ? errorMessage(remove.error) : null}
        onClose={() => setDeleting(null)}
        onConfirm={() =>
          deleting &&
          remove.mutate(deleting.id, {
            onSuccess: () => {
              onSaved('Question deleted.')
              setDeleting(null)
            },
          })
        }
      >
        “{deleting?.text}” has never been answered, so it can be deleted for good.
      </ConfirmDialog>
    </Card>
  )
}

function TraitsTab({ onSaved }: { onSaved: (m: string) => void }) {
  const traits = useTraits()
  const [editing, setEditing] = useState<Editing<Trait>>({ open: false })

  return (
    <Card>
      <div className="flex items-center justify-between gap-3 border-b border-line px-5 py-4">
        <p className="text-sm text-muted">Traits the quiz measures and careers weigh.</p>
        <Button onClick={() => setEditing({ open: true })}>
          <Plus className="size-4" aria-hidden /> New trait
        </Button>
      </div>
      {traits.isPending ? (
        <TableSkeleton label="Loading traits" />
      ) : traits.isError ? (
        <QueryError error={traits.error} onRetry={() => traits.refetch()} />
      ) : traits.data.length === 0 ? (
        <EmptyState icon={Tags} title="No traits yet">
          Add traits first: questions, careers and course tags all point at them.
        </EmptyState>
      ) : (
        <ul className="divide-y divide-line" aria-label="Traits">
          {traits.data.map((t) => (
            <li key={t.id} className="flex items-center justify-between gap-3 px-5 py-3.5">
              <div className="min-w-0">
                <p className="font-medium text-ink">{t.name}</p>
                {t.description && <p className="text-sm text-muted">{t.description}</p>}
              </div>
              <Button variant="ghost" className="h-8 px-2.5" onClick={() => setEditing({ open: true, item: t })} aria-label={`Edit ${t.name}`}>
                <Pencil className="size-4" aria-hidden /> Edit
              </Button>
            </li>
          ))}
        </ul>
      )}
      <TraitFormDialog
        open={editing.open}
        trait={editing.open ? editing.item : undefined}
        onClose={() => setEditing({ open: false })}
        onSaved={(t) => {
          onSaved(`${t.name} saved.`)
          setEditing({ open: false })
        }}
      />
    </Card>
  )
}

function CareersTab({ onSaved }: { onSaved: (m: string) => void }) {
  const careers = useAdminCareers()
  const traits = useTraits()
  const courses = useCourseTags()
  const [editing, setEditing] = useState<Editing<CareerPath>>({ open: false })

  return (
    <Card>
      <div className="flex items-center justify-between gap-3 border-b border-line px-5 py-4">
        <p className="text-sm text-muted">Careers with the traits they need and the steps to get there.</p>
        <Button onClick={() => setEditing({ open: true })} disabled={!traits.data || !courses.data}>
          <Plus className="size-4" aria-hidden /> New career
        </Button>
      </div>
      {careers.isPending ? (
        <TableSkeleton label="Loading careers" />
      ) : careers.isError ? (
        <QueryError error={careers.error} onRetry={() => careers.refetch()} />
      ) : careers.data.length === 0 ? (
        <EmptyState icon={Compass} title="No careers yet">
          Add careers so students can be matched to them.
        </EmptyState>
      ) : (
        <ul className="divide-y divide-line" aria-label="Careers">
          {careers.data.map((c) => (
            <li key={c.id} className="flex flex-wrap items-start justify-between gap-3 px-5 py-4">
              <div className="min-w-0 space-y-1">
                <p className="flex flex-wrap items-center gap-2 font-medium text-ink">
                  {c.name} {!c.isActive && <Badge>Inactive</Badge>}
                </p>
                <p className="text-sm text-muted">{c.traits.map((t) => `${t.traitName} ${t.weight}`).join(' · ')}</p>
                <p className="text-xs text-muted">
                  {c.steps.length} roadmap {c.steps.length === 1 ? 'step' : 'steps'}
                  {c.steps.some((s) => s.courseCode) ? `: ${c.steps.filter((s) => s.courseCode).map((s) => s.courseCode).join(', ')}` : ''}
                </p>
              </div>
              <Button
                variant="ghost"
                className="h-8 px-2.5"
                onClick={() => setEditing({ open: true, item: c })}
                disabled={!traits.data || !courses.data}
                aria-label={`Edit ${c.name}`}
              >
                <Pencil className="size-4" aria-hidden /> Edit
              </Button>
            </li>
          ))}
        </ul>
      )}
      <CareerFormDialog
        open={editing.open}
        career={editing.open ? editing.item : undefined}
        traits={traits.data ?? []}
        courses={courses.data ?? []}
        onClose={() => setEditing({ open: false })}
        onSaved={(c) => {
          onSaved(`${c.name} saved.`)
          setEditing({ open: false })
        }}
      />
    </Card>
  )
}

function CourseTagsTab({ onSaved }: { onSaved: (m: string) => void }) {
  const courses = useCourseTags()
  const traits = useTraits()
  const [editing, setEditing] = useState<CourseTags | null>(null)
  const names = new Map((traits.data ?? []).map((t) => [t.id, t.name]))

  return (
    <Card>
      <p className="border-b border-line px-5 py-4 text-sm text-muted">
        Grades in a tagged course count for about 30% of a student's trait score.
      </p>
      {courses.isPending || traits.isPending ? (
        <TableSkeleton label="Loading courses" />
      ) : courses.isError || traits.isError ? (
        <QueryError error={courses.error ?? traits.error} onRetry={() => { void courses.refetch(); void traits.refetch() }} />
      ) : courses.data.length === 0 ? (
        <EmptyState icon={Tags} title="No courses yet">
          Create courses first, then tag them with traits.
        </EmptyState>
      ) : (
        <ul className="divide-y divide-line" aria-label="Course tags">
          {courses.data.map((c) => (
            <li key={c.courseId} className="flex flex-wrap items-center justify-between gap-3 px-5 py-3.5">
              <div className="min-w-0 space-y-1">
                <p className="text-ink">
                  <span className="font-medium">{c.code}</span> {c.name}
                </p>
                <div className="flex flex-wrap gap-1.5">
                  {c.traitIds.length === 0 ? (
                    <span className="text-sm text-muted">No traits</span>
                  ) : (
                    c.traitIds.map((id) => <Badge key={id}>{names.get(id) ?? `Trait ${id}`}</Badge>)
                  )}
                </div>
              </div>
              <Button variant="ghost" className="h-8 px-2.5" onClick={() => setEditing(c)} aria-label={`Edit traits of ${c.code}`}>
                <Pencil className="size-4" aria-hidden /> Edit
              </Button>
            </li>
          ))}
        </ul>
      )}
      <CourseTagsDialog
        course={editing}
        traits={traits.data ?? []}
        onClose={() => setEditing(null)}
        onSaved={(c) => {
          onSaved(`Traits of ${c.code} saved.`)
          setEditing(null)
        }}
      />
    </Card>
  )
}
