import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { ComponentRoster, GradingSection, SaveScoresRequest, TeachingSection } from '../types'

// PB-08 / T34: instructor grading — section → component → scores, then finalize. Simulated data (SR-23).

const teaching: TeachingSection[] = [
  {
    sectionId: 21, courseCode: 'CSC 210', courseName: 'Data Structures', sectionCode: 'A', termName: 'Fall 2026',
    studentCount: 2, componentCount: 2, totalWeight: 100, notGraded: 3, isFinalized: false,
  },
  {
    sectionId: 41, courseCode: 'MAT 201', courseName: 'Discrete Mathematics', sectionCode: 'A', termName: 'Fall 2026',
    studentCount: 1, componentCount: 0, totalWeight: 0, notGraded: 0, isFinalized: false,
  },
]

const section21 = (isFinalized = false): GradingSection => ({
  sectionId: 21, courseCode: 'CSC 210', courseName: 'Data Structures', sectionCode: 'A', termName: 'Fall 2026',
  studentCount: 2, totalWeight: 100, notGraded: 3, isFinalized,
  components: [
    { id: 1, name: 'Midterm', maxGrade: 50, weight: 40, graded: 1, notGraded: 1 },
    { id: 2, name: 'Final', maxGrade: 100, weight: 60, graded: 0, notGraded: 2 },
  ],
})

const roster = (componentId: number, scores: (number | null)[], isFinalized = false): ComponentRoster => {
  const c = section21().components.find((x) => x.id === componentId)!
  return {
    sectionId: 21, courseCode: 'CSC 210', sectionCode: 'A', isFinalized, component: c,
    students: [
      { enrollmentId: 7, universityId: 3001, fullName: 'Demo Student', status: isFinalized ? 'Completed' : 'Enrolled', score: scores[0] ?? null },
      { enrollmentId: 8, universityId: 3002, fullName: 'Demo Student 2', status: isFinalized ? 'Completed' : 'Enrolled', score: scores[1] ?? null },
    ],
  }
}

function mockGrading({ sections = teaching, finalized = false } = {}) {
  const sent: { url: string; body?: SaveScoresRequest }[] = []
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))),
    http.get('/api/instructor/grading/sections', () => HttpResponse.json(sections)),
    http.get('/api/instructor/grading/sections/21', () => HttpResponse.json(section21(finalized))),
    http.get('/api/instructor/grading/sections/41', () =>
      HttpResponse.json({ ...section21(), sectionId: 41, courseCode: 'MAT 201', totalWeight: 0, components: [] }),
    ),
    http.get('/api/instructor/grading/sections/21/components/1', () => HttpResponse.json(roster(1, [42.5, null], finalized))),
    http.get('/api/instructor/grading/sections/21/components/2', () => HttpResponse.json(roster(2, [], finalized))),
    http.put('/api/instructor/grading/sections/21/components/:componentId', async ({ request, params }) => {
      const body = (await request.json()) as SaveScoresRequest
      sent.push({ url: `components/${String(params.componentId)}`, body })
      return HttpResponse.json(roster(Number(params.componentId), body.scores.map((s) => s.score)))
    }),
    http.post('/api/instructor/grading/sections/21/finalize', () => {
      sent.push({ url: 'finalize' })
      return HttpResponse.json({ sectionId: 21, students: [] })
    }),
  )
  return { sent }
}

/** The roster needs three requests in a row (sections → section → roster), slow when the whole suite runs in parallel. */
const scoreInput = (name: string) => screen.findByLabelText(`Score for ${name}`, {}, { timeout: 15000 })

describe('InstructorGradingPage', () => {
  it('opens the first section and component with existing scores filled in', async () => {
    mockGrading()
    renderAt('/instructor/grading', { as: 'Instructor' })

    expect(await scoreInput('Demo Student')).toHaveValue('42.5')
    expect(screen.getByLabelText('Score for Demo Student 2')).toHaveValue('')
    expect(screen.getByText('1 not graded')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Midterm · 40%, 1 not graded' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Final · 60%, 2 not graded' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'CSC 210 · A — Data Structures (3 not graded)' })).toBeInTheDocument()
  })

  it('switches component and saves the scores', async () => {
    const { sent } = mockGrading()
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    await scoreInput('Demo Student')

    await user.click(screen.getByRole('tab', { name: /^Final · 60%/ }))
    const first = await scoreInput('Demo Student')
    expect(screen.getByText('2 not graded')).toBeInTheDocument()
    await user.type(first, '88')
    expect(screen.getByText('1 not graded')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Save scores' }))

    expect(await screen.findByText('Scores saved for Final.')).toBeInTheDocument()
    expect(sent).toEqual([{ url: 'components/2', body: { scores: [{ enrollmentId: 7, score: 88 }, { enrollmentId: 8, score: null }] } }])
  })

  it('refuses a score above the max without calling the API', async () => {
    const { sent } = mockGrading()
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    const input = await scoreInput('Demo Student 2')

    await user.type(input, '51')
    // Checked as soon as the field is left, before any save.
    await user.tab()
    expect(await screen.findByText('Enter a score between 0 and 50 (2 decimals).')).toBeInTheDocument()
    expect(input).toHaveAttribute('aria-invalid', 'true')

    await user.click(screen.getByRole('button', { name: 'Save scores' }))
    const summary = await screen.findByText('1 score is above the maximum (50). Fix it to save.')
    await waitFor(() => expect(document.activeElement).toContainElement(summary))
    expect(sent).toEqual([])

    // Correcting the score clears the error while typing.
    await user.clear(input)
    await user.type(input, '50')
    expect(screen.queryByText('Enter a score between 0 and 50 (2 decimals).')).not.toBeInTheDocument()
    expect(screen.queryByText('1 score is above the maximum (50). Fix it to save.')).not.toBeInTheDocument()
  })

  it('shows a 400 from the API under the student\'s score', async () => {
    mockGrading()
    server.use(
      http.put('/api/instructor/grading/sections/21/components/1', () =>
        HttpResponse.json(
          { status: 400, title: 'Some fields are not valid.', errors: { 'scores[1].enrollmentId': ['Demo Student 2\'s grade is already final.'], 'scores[1].score': ['Enter a score between 0 and 50.'] } },
          { status: 400 },
        ),
      ),
    )
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    await user.type(await scoreInput('Demo Student 2'), '30')
    await user.click(screen.getByRole('button', { name: 'Save scores' }))

    expect(await screen.findByText('Enter a score between 0 and 50.')).toBeInTheDocument()
  })

  it('finalizes after confirming, showing a 409 first', async () => {
    mockGrading()
    let calls = 0
    server.use(
      http.post('/api/instructor/grading/sections/21/finalize', () => {
        calls++
        if (calls === 1)
          return HttpResponse.json(
            { status: 409, title: '3 scores are missing (Demo Student 2: Midterm and 2 more). Enter every score, 0 included, before finalizing.', code: 'scores_missing' },
            { status: 409 },
          )
        return HttpResponse.json({ sectionId: 21, students: [{ enrollmentId: 7 }, { enrollmentId: 8 }] })
      }),
    )
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    await scoreInput('Demo Student')

    await user.click(screen.getByRole('button', { name: /Finalize grades/ }))
    const dialog = screen.getByRole('dialog', { name: 'Finalize grades for CSC 210 A?' })
    await user.click(within(dialog).getByRole('button', { name: 'Finalize' }))
    expect(await within(dialog).findByText(/3 scores are missing/)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Finalize' }))
    expect(await screen.findByText('Grades finalized for 2 students in CSC 210 A.')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('shows a finalized section read-only', async () => {
    mockGrading({ finalized: true })
    renderAt('/instructor/grading', { as: 'Instructor' })

    expect(await scoreInput('Demo Student')).toBeDisabled()
    expect(screen.getAllByText('Finalized').length).toBeGreaterThan(0)
    expect(screen.queryByRole('button', { name: 'Save scores' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Finalize grades/ })).not.toBeInTheDocument()
  })

  it('explains when the course has no components yet', async () => {
    mockGrading()
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    await scoreInput('Demo Student')

    await user.selectOptions(screen.getByLabelText('Section'), 'MAT 201 · A — Discrete Mathematics')

    expect(await screen.findByText('No grade components yet')).toBeInTheDocument()
  })

  it('asks before switching component with unsaved scores, and keeps them on cancel', async () => {
    const { sent } = mockGrading()
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    await user.type(await scoreInput('Demo Student 2'), '30')

    await user.click(screen.getByRole('tab', { name: /^Final · 60%/ }))
    const dialog = screen.getByRole('dialog', { name: 'Discard unsaved scores?' })
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }))
    expect(screen.getByRole('tab', { name: /^Midterm/ })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByLabelText('Score for Demo Student 2')).toHaveValue('30')

    await user.click(screen.getByRole('tab', { name: /^Final · 60%/ }))
    await user.click(within(screen.getByRole('dialog', { name: 'Discard unsaved scores?' })).getByRole('button', { name: 'Discard scores' }))
    expect(screen.getByRole('tab', { name: /^Final/ })).toHaveAttribute('aria-selected', 'true')
    expect(await scoreInput('Demo Student 2')).toHaveValue('')
    expect(sent).toEqual([])
  })

  it('asks before switching section with unsaved scores', async () => {
    mockGrading()
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    await user.type(await scoreInput('Demo Student 2'), '30')

    await user.selectOptions(screen.getByLabelText('Section'), 'MAT 201 · A — Discrete Mathematics')
    const dialog = screen.getByRole('dialog', { name: 'Discard unsaved scores?' })
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }))
    expect(screen.getByLabelText('Score for Demo Student 2')).toHaveValue('30')

    await user.selectOptions(screen.getByLabelText('Section'), 'MAT 201 · A — Discrete Mathematics')
    await user.click(within(screen.getByRole('dialog', { name: 'Discard unsaved scores?' })).getByRole('button', { name: 'Discard scores' }))
    expect(await screen.findByText('No grade components yet')).toBeInTheDocument()
  })

  it('moves to the next student on Enter instead of saving', async () => {
    const { sent } = mockGrading()
    const { user } = renderAt('/instructor/grading', { as: 'Instructor' })
    const first = await scoreInput('Demo Student')
    await user.clear(first)
    await user.type(first, '40{Enter}')

    expect(screen.getByLabelText('Score for Demo Student 2')).toHaveFocus()
    expect(sent).toEqual([])
  })

  it('keeps finalize off while the weights are not 100%, and says why', async () => {
    mockGrading()
    server.use(http.get('/api/instructor/grading/sections/21', () => HttpResponse.json({ ...section21(), totalWeight: 80 })))
    renderAt('/instructor/grading', { as: 'Instructor' })
    await scoreInput('Demo Student')

    expect(screen.getByRole('button', { name: /Finalize grades/ })).toBeDisabled()
    expect(screen.getByText(/the components add up to 80%, not 100%/)).toBeInTheDocument()
  })

  it('opens the section given in the URL', async () => {
    mockGrading()
    renderAt('/instructor/grading?section=41', { as: 'Instructor' })
    expect(await screen.findByText('No grade components yet')).toBeInTheDocument()
    expect(screen.getByLabelText('Section')).toHaveValue('41')
  })

  it('shows an empty state without sections', async () => {
    mockGrading({ sections: [] })
    renderAt('/instructor/grading', { as: 'Instructor' })
    expect(await screen.findByText('No sections to grade')).toBeInTheDocument()
  })

  it('shows an error with retry when the sections fail to load', async () => {
    mockGrading()
    server.use(http.get('/api/instructor/grading/sections', () => HttpResponse.json({ title: 'Server error' }, { status: 500 })))
    renderAt('/instructor/grading', { as: 'Instructor' })
    expect(await screen.findByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
