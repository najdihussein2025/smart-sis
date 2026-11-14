import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { renderAt } from '@/test/render'
import type { Assessment, MyAssessments, Quiz, Recommendations, Roadmap } from '../types'

// PB-16 / T29, PB-17 / T67, PB-20 / T73: the student career page. Simulated data (SR-23).

const quiz: Quiz = {
  termName: 'Fall 2026',
  questions: [
    { id: 1, text: 'In a group project, which task do you pick first?', options: [{ id: 11, text: 'Planning' }, { id: 12, text: 'Coding' }] },
    { id: 2, text: 'Which school subject did you enjoy most?', options: [{ id: 21, text: 'Mathematics' }, { id: 22, text: 'Art' }] },
  ],
}

const attempt: Assessment = {
  id: 7, termId: 1, termName: 'Fall 2026', submittedAt: '2026-11-10T09:00:00Z',
  traits: [
    { traitId: 1, traitName: 'Analytical thinking', points: 30, maxPoints: 40, score: 75 },
    { traitId: 2, traitName: 'Creativity', points: 10, maxPoints: 40, score: 25 },
  ],
}

const mine = (extra: Partial<MyAssessments> = {}): MyAssessments => ({
  currentTermId: 1, currentTermName: 'Fall 2026', canTake: true, reason: null, attempts: [], ...extra,
})

const taken = mine({ canTake: false, reason: 'You already took the assessment in Fall 2026. You can retake it next term.', attempts: [attempt] })

const recommendations = (sectionStatus: 'Available' | 'InCart' = 'Available'): Recommendations => ({
  assessmentId: 7, termName: 'Fall 2026', submittedAt: attempt.submittedAt, usesGrades: true,
  careers: [
    { careerId: 2, name: 'Data Scientist', description: 'Turns data into insight.', matchPct: 81, quizPct: 78, gradePct: 88, topTraits: ['Analytical thinking'] },
    { careerId: 1, name: 'Software Engineer', description: null, matchPct: 77, quizPct: 75, gradePct: null, topTraits: ['Technical aptitude'] },
    { careerId: 4, name: 'UX Designer', description: null, matchPct: 52, quizPct: 52, gradePct: null, topTraits: [] },
  ],
  explanation: 'You love finding patterns in data.',
  explanationSource: 'Ai',
  suggestedCourses: [
    { courseId: 3, code: 'CSC 310', name: 'Database Systems', credits: 3, careers: ['Data Scientist'], sections: [{ sectionId: 31, sectionCode: 'A', seatsLeft: 20, status: sectionStatus }] },
    { courseId: 9, code: 'CSC 450', name: 'Machine Learning', credits: 3, careers: ['Data Scientist'], sections: [] },
  ],
})

const roadmap = (careerId: number, name: string): Roadmap => ({
  careerId, name, description: null, doneCount: 1, courseStepCount: 3,
  steps: [
    { stepOrder: 1, title: 'Learn to program', description: null, status: 'Done', course: { id: 1, code: 'CSC 101', name: 'Introduction to Programming', credits: 3, sections: [] } },
    { stepOrder: 2, title: 'Data structures', description: null, status: 'InProgress', course: { id: 2, code: 'CSC 210', name: 'Data Structures', credits: 3, sections: [] } },
    { stepOrder: 3, title: 'Databases', description: 'SQL every day.', status: 'Next', course: { id: 3, code: 'CSC 310', name: 'Database Systems', credits: 3, sections: [{ sectionId: 31, sectionCode: 'A', seatsLeft: 0, status: 'Available' }] } },
    { stepOrder: 4, title: 'Analyse a real dataset', description: null, status: 'Milestone', course: null },
  ],
})

function mockResults(onAdd: () => Response = () => HttpResponse.json({})) {
  let inCart = false
  const roadmapCalls: number[] = []
  server.use(
    http.get('/api/career/assessments', () => HttpResponse.json(taken)),
    http.get('/api/career/recommendations', () => HttpResponse.json(recommendations(inCart ? 'InCart' : 'Available'))),
    http.get('/api/career/careers', () =>
      HttpResponse.json([{ id: 1, name: 'Software Engineer', description: null }, { id: 2, name: 'Data Scientist', description: null }])),
    http.get('/api/career/careers/:id/roadmap', ({ params }) => {
      const id = Number(params.id)
      roadmapCalls.push(id)
      return HttpResponse.json(roadmap(id, id === 2 ? 'Data Scientist' : 'Software Engineer'))
    }),
    http.post('/api/cart', () => {
      const res = onAdd()
      if (res.ok) inCart = true
      return res
    }),
  )
  return roadmapCalls
}

describe('CareerPage', () => {
  it('takes the assessment: progress, a missing answer, then submit and results', async () => {
    let submitted: unknown = null
    server.use(
      http.get('/api/career/assessments', () => HttpResponse.json(submitted ? taken : mine())),
      http.get('/api/career/quiz', () => HttpResponse.json(quiz)),
      http.post('/api/career/assessments', async ({ request }) => {
        submitted = await request.json()
        return HttpResponse.json(attempt, { status: 201 })
      }),
    )
    mockResultsAfterSubmit()
    const { user } = renderAt('/student/career', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Start assessment' }))
    expect(await screen.findByText('0 of 2 answered')).toBeInTheDocument()

    await user.click(screen.getByRole('radio', { name: 'Coding' }))
    expect(screen.getByText('1 of 2 answered')).toBeInTheDocument()
    expect(screen.getByRole('progressbar', { name: 'Assessment progress' })).toHaveAttribute('aria-valuenow', '1')

    await user.click(screen.getByRole('button', { name: 'Submit assessment' }))
    expect(screen.getByText('Answer every question: 1 is still unanswered.')).toBeInTheDocument()
    expect(submitted).toBeNull()

    await user.click(screen.getByRole('radio', { name: 'Mathematics' }))
    await user.click(screen.getByRole('button', { name: 'Submit assessment' }))

    expect(await screen.findByText('Assessment submitted. Here are your results.')).toBeInTheDocument()
    expect(submitted).toEqual({ answers: [{ questionId: 1, optionId: 12 }, { questionId: 2, optionId: 21 }] })
    expect(await screen.findByRole('list', { name: 'Recommended careers' })).toBeInTheDocument()
  })

  it('shows the API\'s 409 when the submit is refused', async () => {
    server.use(
      http.get('/api/career/assessments', () => HttpResponse.json(mine())),
      http.get('/api/career/quiz', () => HttpResponse.json(quiz)),
      http.post('/api/career/assessments', () =>
        HttpResponse.json({ title: 'You already took the assessment in Fall 2026. You can retake it next term.', code: 'already_taken' }, { status: 409 })),
    )
    const { user } = renderAt('/student/career', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Start assessment' }))
    await user.click(await screen.findByRole('radio', { name: 'Planning' }))
    await user.click(screen.getByRole('radio', { name: 'Art' }))
    await user.click(screen.getByRole('button', { name: 'Submit assessment' }))

    expect(await screen.findByText('You already took the assessment in Fall 2026. You can retake it next term.')).toBeInTheDocument()
  })

  it('shows why the quiz can\'t load, with a retry', async () => {
    server.use(
      http.get('/api/career/assessments', () => HttpResponse.json(mine())),
      http.get('/api/career/quiz', () =>
        HttpResponse.json({ title: "The career assessment isn't ready yet: it needs at least 20 questions. Please try again later." }, { status: 409 })),
    )
    const { user } = renderAt('/student/career', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Start assessment' }))

    expect(await screen.findByText(/needs at least 20 questions/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('shows top 3 careers with % match, the explanation and suggested courses with "Add to cart"', async () => {
    mockResults()
    const { user } = renderAt('/student/career', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Recommended careers' })
    expect(within(list).getAllByRole('listitem')).toHaveLength(3)
    expect(within(list).getByRole('meter', { name: 'Data Scientist match' })).toHaveAttribute('aria-valuenow', '81')
    expect(within(list).getByText('81%')).toBeInTheDocument()
    expect(within(list).getByText('Assessment 78% · Grades 88%')).toBeInTheDocument()
    expect(screen.getByText('You love finding patterns in data.')).toBeInTheDocument()
    expect(screen.getByText('Written by the AI assistant from your scores.')).toBeInTheDocument()
    expect(screen.getByText(/You already took the assessment in Fall 2026/)).toBeInTheDocument()
    expect(screen.getByRole('meter', { name: 'Analytical thinking' })).toHaveAttribute('aria-valuenow', '75')

    const suggested = screen.getByRole('list', { name: 'Suggested courses' })
    expect(within(suggested).getByText('Not offered this term.')).toBeInTheDocument()
    await user.click(within(suggested).getByRole('button', { name: 'Add CSC 310 A to cart' }))
    expect(await within(suggested).findByRole('button', { name: 'CSC 310 A: In Cart' })).toBeDisabled()
  })

  it('shows the cart\'s 409 message when adding fails', async () => {
    mockResults(() => HttpResponse.json({ title: "You're already enrolled in CSC 310 A." }, { status: 409 }))
    const { user } = renderAt('/student/career', { as: 'Student' })

    const suggested = await screen.findByRole('list', { name: 'Suggested courses' })
    await user.click(within(suggested).getByRole('button', { name: 'Add CSC 310 A to cart' }))

    expect(await screen.findByText("You're already enrolled in CSC 310 A.")).toBeInTheDocument()
  })

  it('shows the best match\'s roadmap with progress, and switches career', async () => {
    const calls = mockResults()
    const { user } = renderAt('/student/career', { as: 'Student' })

    const steps = await screen.findByRole('list', { name: 'Data Scientist roadmap' })
    const items = Array.from(steps.children) as HTMLElement[]
    expect(within(items[0]!).getByText('Done')).toBeInTheDocument()
    expect(within(items[1]!).getByText('In progress')).toBeInTheDocument()
    expect(within(items[2]!).getByText('Next')).toBeInTheDocument()
    expect(within(items[2]!).getByRole('button', { name: 'CSC 310 A: Full' })).toBeDisabled()
    expect(within(items[3]!).getByText('Milestone')).toBeInTheDocument()
    expect(screen.getByText('1 of 3 courses done')).toBeInTheDocument()

    await user.selectOptions(screen.getByRole('combobox', { name: 'Career' }), 'Software Engineer')
    expect(await screen.findByRole('list', { name: 'Software Engineer roadmap' })).toBeInTheDocument()
    expect(calls).toEqual([2, 1])
  })

  it('explains when the assessment is closed and there are no results', async () => {
    server.use(
      http.get('/api/career/assessments', () =>
        HttpResponse.json(mine({ canTake: false, currentTermId: null, currentTermName: null, reason: 'There is no current term, so the assessment is closed for now.' }))),
    )
    renderAt('/student/career', { as: 'Student' })

    expect(await screen.findByText('The assessment is closed')).toBeInTheDocument()
    expect(screen.getByText('There is no current term, so the assessment is closed for now.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Start assessment' })).not.toBeInTheDocument()
  })

  it('shows the error state with a retry button', async () => {
    server.use(http.get('/api/career/assessments', () => HttpResponse.json({ title: 'Database unavailable.' }, { status: 500 })))
    renderAt('/student/career', { as: 'Student' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('is in the student navigation', async () => {
    server.use(http.get('/api/career/assessments', () => HttpResponse.json(mine())))
    renderAt('/student/career', { as: 'Student' })
    expect(await screen.findByRole('link', { name: 'Career' })).toHaveAttribute('href', '/student/career')
  })
})

/** Results endpoints for the test that submits first. */
function mockResultsAfterSubmit() {
  server.use(
    http.get('/api/career/recommendations', () => HttpResponse.json(recommendations())),
    http.get('/api/career/careers', () => HttpResponse.json([{ id: 2, name: 'Data Scientist', description: null }])),
    http.get('/api/career/careers/:id/roadmap', () => HttpResponse.json(roadmap(2, 'Data Scientist'))),
  )
}
