import { useEffect, useState } from 'react'
import { useMyEnrollments } from '@/features/enrollment/hooks'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { SelectField } from '@/shared/ui/SelectField'
import { Spinner } from '@/shared/ui/Spinner'

export interface TutorCourse {
  courseId: number
  courseCode: string
  courseName: string
}

interface Props {
  open: boolean
  onClose: () => void
  onStart: (course: TutorCourse) => void
}

/** PB-19 / T70: pick one of the courses the student is enrolled in this term, then ask the tutor about it. */
export function TutorDialog({ open, onClose, onStart }: Props) {
  const mine = useMyEnrollments()
  const [courseId, setCourseId] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (open) {
      setCourseId('')
      setError(null)
    }
  }, [open])

  // One entry per course (two sections of a course can't both be enrolled, but stay safe).
  const courses = [...new Map((mine.data?.items ?? []).map((s) => [s.courseId, s])).values()]

  const start = () => {
    const course = courses.find((c) => String(c.courseId) === courseId)
    if (!course) {
      setError('Choose a course.')
      return
    }
    onStart({ courseId: course.courseId, courseCode: course.courseCode, courseName: course.courseName })
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Tutoring"
      description="Get step-by-step explanations of a course's concepts. The tutor explains and gives hints; it won't do graded work for you."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="button" onClick={start} disabled={courses.length === 0}>
            Start tutoring
          </Button>
        </>
      }
    >
      {mine.isPending ? (
        <Spinner />
      ) : mine.isError ? (
        <Alert>{errorMessage(mine.error, 'Could not load your courses.')}</Alert>
      ) : courses.length === 0 ? (
        <p className="text-sm text-muted">You're not enrolled in any course this term. Tutoring is available for your enrolled courses.</p>
      ) : (
        <SelectField
          label="Course"
          value={courseId}
          error={error ?? undefined}
          onChange={(e) => {
            setCourseId(e.target.value)
            setError(null)
          }}
        >
          <option value="">Choose a course…</option>
          {courses.map((c) => (
            <option key={c.courseId} value={c.courseId}>
              {c.courseCode} · {c.courseName}
            </option>
          ))}
        </SelectField>
      )}
    </Dialog>
  )
}
