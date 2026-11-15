import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { CourseGrades, MyGrades, Transcript } from '../types'

// PB-09 / T37: the student's grades this term and transcript. Simulated data (SR-23).

const csc210: CourseGrades = {
  sectionId: 21, courseCode: 'CSC 210', courseName: 'Data Structures', sectionCode: 'A', credits: 3, status: 'Enrolled',
  gradedWeight: 50, earned: 42, runningPct: 84, finalPct: null, letterGrade: null, gradePoints: null,
  components: [
    { name: 'Lab work', weight: 20, maxGrade: 100, score: 90, pct: 90 },
    { name: 'Midterm', weight: 30, maxGrade: 100, score: 80, pct: 80 },
    { name: 'Final', weight: 50, maxGrade: 100, score: null, pct: null },
  ],
}
const mat201: CourseGrades = {
  ...csc210, sectionId: 41, courseCode: 'MAT 201', courseName: 'Discrete Mathematics', status: 'Completed',
  gradedWeight: 100, earned: 88.5, runningPct: 88.5, finalPct: 89, letterGrade: 'A-', gradePoints: 3.7,
  components: [{ name: 'Final', weight: 100, maxGrade: 100, score: 88.5, pct: 88.5 }],
}
const myGrades = (extra: Partial<MyGrades> = {}): MyGrades => ({
  termId: 1, termName: 'Fall 2026', termGpa: 3.7, cumulativeGpa: 3.35, creditsEarned: 6, courses: [csc210, mat201], ...extra,
})

const transcript: Transcript = {
  cumulativeGpa: 3.35, creditsAttempted: 6, creditsEarned: 6,
  terms: [
    {
      termId: 2, termName: 'Spring 2026', isCurrent: false, termGpa: 3.0, cumulativeGpa: 3.0, creditsAttempted: 3, creditsEarned: 3,
      courses: [{ courseCode: 'CSC 101', courseName: 'Introduction to Programming', sectionCode: 'A', credits: 3, status: 'Completed', finalPct: 82, letterGrade: 'B', gradePoints: 3.0, countsInGpa: true }],
    },
    {
      termId: 1, termName: 'Fall 2026', isCurrent: true, termGpa: 3.7, cumulativeGpa: 3.35, creditsAttempted: 3, creditsEarned: 3,
      courses: [
        { courseCode: 'CSC 210', courseName: 'Data Structures', sectionCode: 'A', credits: 3, status: 'Enrolled', finalPct: null, letterGrade: null, gradePoints: null, countsInGpa: true },
        { courseCode: 'MAT 201', courseName: 'Discrete Mathematics', sectionCode: 'A', credits: 3, status: 'Completed', finalPct: 89, letterGrade: 'A-', gradePoints: 3.7, countsInGpa: true },
      ],
    },
  ],
}

function mockGrades(my: MyGrades = myGrades(), tr: Transcript = transcript) {
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/grades/my', () => HttpResponse.json(my)),
    http.get('/api/grades/transcript', () => HttpResponse.json(tr)),
  )
}

describe('StudentGradesPage', () => {
  it('shows each course with components, not graded parts, progress and GPA', async () => {
    mockGrades()
    renderAt('/student/grades', { as: 'Student' })

    const course = await screen.findByRole('article', { name: 'CSC 210 grades' })
    expect(within(course).getByText('In progress')).toBeInTheDocument()
    const midterm = within(course).getByRole('rowheader', { name: 'Midterm' }).closest('tr')!
    expect(within(midterm).getByText('30%')).toBeInTheDocument()
    expect(within(midterm).getByText('80 / 100')).toBeInTheDocument()
    expect(within(midterm).getByText('80%')).toBeInTheDocument()
    const final = within(course).getByRole('rowheader', { name: 'Final' }).closest('tr')!
    expect(within(final).getByText('Not graded')).toBeInTheDocument()
    expect(within(course).getByRole('progressbar', { name: 'Graded so far in CSC 210 A' })).toHaveAttribute('aria-valuenow', '50')
    expect(within(course).getByText('84%')).toBeInTheDocument()

    const done = screen.getByRole('article', { name: 'MAT 201 grades' })
    expect(within(done).getByText('Final: A- (89%)')).toBeInTheDocument()

    expect(screen.getByText('3.70')).toBeInTheDocument()
    expect(screen.getByText('3.35')).toBeInTheDocument()
  })

  it('a course with no grade components shows only the setup notice, no progress', async () => {
    const bare: CourseGrades = { ...csc210, gradedWeight: 0, earned: 0, runningPct: null, components: [] }
    mockGrades(myGrades({ courses: [bare] }))
    renderAt('/student/grades', { as: 'Student' })

    const course = await screen.findByRole('article', { name: 'CSC 210 grades' })
    expect(within(course).getByText("The grade components for this course aren't set up yet.")).toBeInTheDocument()
    expect(within(course).queryByRole('progressbar')).not.toBeInTheDocument()
    expect(within(course).queryByText(/Graded so far/)).not.toBeInTheDocument()
    expect(within(course).queryByRole('table')).not.toBeInTheDocument()
  })

  it('the progress bar says graded and earned in words', async () => {
    mockGrades()
    renderAt('/student/grades', { as: 'Student' })
    const bar = await screen.findByRole('progressbar', { name: 'Graded so far in CSC 210 A' })
    expect(bar).toHaveAttribute('aria-valuetext', '50% graded, 42 of 100 points earned')
    expect(screen.getByRole('heading', { level: 1, name: 'Grades' })).toBeInTheDocument()
    expect(screen.getAllByText('Fall 2026').length).toBeGreaterThan(0)
  })

  it('shows the transcript per term with term and cumulative GPA', async () => {
    mockGrades()
    const { user } = renderAt('/student/grades', { as: 'Student' })
    await screen.findByRole('article', { name: 'CSC 210 grades' })

    await user.click(screen.getByRole('tab', { name: 'Transcript' }))

    const spring = await screen.findByRole('region', { name: 'Spring 2026' })
    expect(within(spring).getByText('B')).toBeInTheDocument()
    expect(within(spring).getByText(/Term GPA/)).toHaveTextContent('Term GPA 3.00 · Cumulative 3.00 · Credits 3 / 3')
    const fall = screen.getByRole('region', { name: 'Fall 2026' })
    expect(within(fall).getByText('Current')).toBeInTheDocument()
    expect(within(fall).getByText('In progress')).toBeInTheDocument()
    expect(screen.getByText(/6 of 6 credits earned/)).toBeInTheDocument()
  })

  it('marks a retaken attempt and an absence drop on the transcript', async () => {
    const retake: Transcript = {
      ...transcript,
      terms: [
        {
          ...transcript.terms[0]!,
          courses: [{ ...transcript.terms[0]!.courses[0]!, letterGrade: 'F', gradePoints: 0, countsInGpa: false }],
        },
        {
          ...transcript.terms[1]!,
          courses: [{ ...transcript.terms[1]!.courses[0]!, status: 'DroppedAbsence', letterGrade: 'F', gradePoints: 0 }],
        },
      ],
    }
    mockGrades(myGrades(), retake)
    const { user } = renderAt('/student/grades', { as: 'Student' })
    await user.click(await screen.findByRole('tab', { name: 'Transcript' }))

    expect(await screen.findByText('Retaken')).toBeInTheDocument()
    expect(screen.getByText('Absences')).toBeInTheDocument()
  })

  it('shows an empty state when not enrolled this term', async () => {
    mockGrades(myGrades({ courses: [], termGpa: null, cumulativeGpa: null, creditsEarned: 0 }))
    renderAt('/student/grades', { as: 'Student' })

    expect(await screen.findByText('No courses this term')).toBeInTheDocument()
    expect(screen.getAllByText('—')).toHaveLength(2)
  })

  it('shows an empty transcript', async () => {
    mockGrades(myGrades({ courses: [] }), { cumulativeGpa: null, creditsAttempted: 0, creditsEarned: 0, terms: [] })
    const { user } = renderAt('/student/grades', { as: 'Student' })
    await user.click(await screen.findByRole('tab', { name: 'Transcript' }))

    expect(await screen.findByText('Nothing on your transcript yet')).toBeInTheDocument()
  })

  it('shows an error with retry', async () => {
    mockGrades()
    server.use(http.get('/api/grades/my', () => HttpResponse.json({ title: 'Server error' }, { status: 500 })))
    renderAt('/student/grades', { as: 'Student' })

    expect(await screen.findByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
