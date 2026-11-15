import { useState } from 'react'
import { ArrowLeft, Send } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { useQuiz, useSubmitAssessment } from '../hooks'

interface Props {
  onCancel: () => void
  onSubmitted: () => void
}

/** PB-16 / T29: the quiz, one radio group per question, with a progress bar and a single submit. */
export function QuizView({ onCancel, onSubmitted }: Props) {
  const quiz = useQuiz(true)
  const submit = useSubmitAssessment()
  const [answers, setAnswers] = useState<Record<number, number>>({})
  const [missing, setMissing] = useState<string | null>(null)

  const back = (
    <div className="mb-4">
      <Button variant="ghost" size="sm" onClick={onCancel}>
        <ArrowLeft className="size-4" aria-hidden /> Back
      </Button>
    </div>
  )

  if (quiz.isPending)
    return (
      <Card>
        <TableSkeleton label="Loading the assessment" rows={5} />
      </Card>
    )
  if (quiz.isError)
    return (
      <>
        {back}
        <Card>
          <QueryError error={quiz.error} onRetry={() => quiz.refetch()} />
        </Card>
      </>
    )

  const questions = quiz.data.questions
  const answered = questions.filter((q) => answers[q.id] !== undefined).length
  const pct = questions.length === 0 ? 0 : Math.round((answered / questions.length) * 100)

  const choose = (questionId: number, optionId: number) => {
    setAnswers((a) => ({ ...a, [questionId]: optionId }))
    setMissing(null)
    submit.reset()
  }

  const onSubmit = () => {
    const firstMissing = questions.find((q) => answers[q.id] === undefined)
    if (firstMissing) {
      const left = questions.length - answered
      setMissing(left === 1 ? 'Answer every question: 1 is still unanswered.' : `Answer every question: ${left} are still unanswered.`)
      document.getElementById(`question-${firstMissing.id}`)?.focus()
      return
    }
    submit.mutate(
      { answers: questions.map((q) => ({ questionId: q.id, optionId: answers[q.id]! })) },
      { onSuccess: onSubmitted },
    )
  }

  return (
    <>
      {back}
      <Card className="sticky top-0 z-10 mb-6 p-4">
        <div className="tabular flex items-center justify-between gap-3 text-sm">
          <span className="font-medium text-ink">
            {answered} of {questions.length} answered
          </span>
          <span className="text-muted">{quiz.data.termName}</span>
        </div>
        <div
          className="mt-2 h-2 overflow-hidden rounded-full bg-canvas"
          role="progressbar"
          aria-label="Assessment progress"
          aria-valuemin={0}
          aria-valuemax={questions.length}
          aria-valuenow={answered}
        >
          <div className="h-full rounded-full bg-brand transition-[width] motion-reduce:transition-none" style={{ width: `${pct}%` }} />
        </div>
      </Card>

      <ol className="space-y-4">
        {questions.map((q, i) => (
          <li key={q.id}>
            <Card className="p-4 sm:p-5">
              <fieldset id={`question-${q.id}`} tabIndex={-1} className="focus:outline-none">
                <legend className="font-semibold text-ink">
                  <span className="tabular text-muted">{i + 1}.</span> {q.text}
                </legend>
                <div className="mt-3 grid gap-2 sm:grid-cols-2">
                  {q.options.map((o) => {
                    const checked = answers[q.id] === o.id
                    return (
                      <label
                        key={o.id}
                        className={`flex min-h-11 cursor-pointer items-start gap-2.5 rounded-lg border px-3.5 py-2.5 text-sm transition-colors ${
                          checked ? 'border-brand bg-brand-50 text-ink' : 'border-line text-ink hover:bg-canvas'
                        }`}
                      >
                        <input
                          type="radio"
                          name={`question-${q.id}`}
                          value={o.id}
                          checked={checked}
                          onChange={() => choose(q.id, o.id)}
                          className="mt-0.5 accent-brand"
                        />
                        {o.text}
                      </label>
                    )
                  })}
                </div>
              </fieldset>
            </Card>
          </li>
        ))}
      </ol>

      <div className="mt-6 space-y-3">
        {missing && <Alert>{missing}</Alert>}
        {submit.isError && <Alert>{errorMessage(submit.error)}</Alert>}
        <Button onClick={onSubmit} loading={submit.isPending} className="max-sm:w-full">
          <Send className="size-4" aria-hidden /> Submit assessment
        </Button>
        <p className="text-sm text-muted">You can take the assessment once per term.</p>
      </div>
    </>
  )
}
