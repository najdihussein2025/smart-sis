import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { renderAt } from '@/test/render'
import type { CareerPath, CourseTags, Question, QuestionBank, Trait } from '../types'

// PB-16 / T29, PB-20 / T72: the admin career setup page. Simulated data (SR-23).

const traits: Trait[] = [
  { id: 1, name: 'Analytical thinking', description: 'Reasoning with data.' },
  { id: 2, name: 'Creativity', description: null },
]

const question = (id: number, extra: Partial<Question> = {}): Question => ({
  id,
  text: `Question number ${id}?`,
  isActive: true,
  answerCount: 0,
  options: [
    { id: id * 10 + 1, text: 'Puzzles', points: [{ traitId: 1, traitName: 'Analytical thinking', points: 3 }] },
    { id: id * 10 + 2, text: 'Drawing', points: [{ traitId: 2, traitName: 'Creativity', points: 2 }] },
  ],
  ...extra,
})

const bank = (questions: Question[], activeCount = questions.filter((q) => q.isActive).length): QuestionBank => ({
  activeCount, minQuestions: 20, maxQuestions: 30, questions,
})

const careers: CareerPath[] = [
  {
    id: 1, name: 'Software Engineer', description: 'Builds software.', isActive: true,
    traits: [{ traitId: 1, traitName: 'Analytical thinking', weight: 8 }],
    steps: [{ id: 5, stepOrder: 1, title: 'Learn to program', description: null, courseId: 1, courseCode: 'CSC 101', courseName: 'Introduction to Programming' }],
  },
]

const courseTags: CourseTags[] = [
  { courseId: 1, code: 'CSC 101', name: 'Introduction to Programming', traitIds: [1] },
  { courseId: 3, code: 'CSC 310', name: 'Database Systems', traitIds: [] },
]

function mockAdmin(questions: Question[] = [question(1), question(2, { answerCount: 4 })], activeCount?: number) {
  server.use(
    http.get('/api/admin/career/traits', () => HttpResponse.json(traits)),
    http.get('/api/admin/career/questions', () => HttpResponse.json(bank(questions, activeCount))),
    http.get('/api/admin/career/careers', () => HttpResponse.json(careers)),
    http.get('/api/admin/career/course-tags', () => HttpResponse.json(courseTags)),
  )
}

describe('AdminCareerPage', () => {
  it('lists the bank and warns while fewer than 20 questions are active', async () => {
    mockAdmin()
    renderAt('/admin/career', { as: 'Admin' })

    expect(await screen.findByText('Question number 1?')).toBeInTheDocument()
    expect(screen.getByText(/Students can't take the assessment until at least 20 questions are active \(2 now\)/)).toBeInTheDocument()
    // Answered questions can't be deleted, only edited (deactivated).
    expect(screen.getByRole('button', { name: 'Delete question 1' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Delete question 2' })).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Career' })).toHaveAttribute('href', '/admin/career')
  })

  it('creates a question with options and trait points', async () => {
    mockAdmin([question(1)], 25)
    let body: unknown = null
    server.use(
      http.post('/api/admin/career/questions', async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(question(3), { status: 201 })
      }),
    )
    const { user } = renderAt('/admin/career', { as: 'Admin' })
    await screen.findByText('Question number 1?')
    expect(screen.queryByText(/Students can't take the assessment/)).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'New question' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Question'), 'What do you enjoy?')
    await user.type(within(dialog).getByLabelText('Option 1'), 'Numbers')
    await user.type(within(dialog).getByLabelText('Option 2'), 'Colours')

    // No option adds points yet → client-side error.
    await user.click(within(dialog).getByRole('button', { name: 'Create question' }))
    expect(await within(dialog).findByText('At least one option must add points to a trait.')).toBeInTheDocument()

    const option1 = within(dialog).getByRole('group', { name: 'Answer option 1' })
    await user.click(within(option1).getByRole('button', { name: 'Add trait points' }))
    await user.selectOptions(within(option1).getByLabelText('Trait'), 'Analytical thinking')
    await user.selectOptions(within(option1).getByLabelText('Points'), '+4')
    await user.click(within(dialog).getByRole('button', { name: 'Create question' }))

    expect(await screen.findByText('Question saved.')).toBeInTheDocument()
    expect(body).toEqual({
      text: 'What do you enjoy?',
      isActive: true,
      options: [
        { id: null, text: 'Numbers', points: [{ traitId: 1, points: 4 }] },
        { id: null, text: 'Colours', points: [] },
      ],
    })
  })

  it('shows the API\'s 409 when an answered option is removed', async () => {
    mockAdmin()
    server.use(
      http.put('/api/admin/career/questions/2', () =>
        HttpResponse.json({ title: 'Students already chose "Drawing", so it can\'t be removed.', code: 'option_answered' }, { status: 409 })),
    )
    const { user } = renderAt('/admin/career', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Edit question 2' }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByText(/Answered 4 times/)).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Add option' }))
    await user.type(within(dialog).getByLabelText('Option 3'), 'Talking')
    await user.click(within(dialog).getByRole('button', { name: 'Remove option 2' }))
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    expect(await within(dialog).findByText('Students already chose "Drawing", so it can\'t be removed.')).toBeInTheDocument()
  })

  it('deletes an unanswered question after confirming', async () => {
    let deleted = false
    mockAdmin()
    server.use(http.delete('/api/admin/career/questions/1', () => {
      deleted = true
      return new HttpResponse(null, { status: 204 })
    }))
    const { user } = renderAt('/admin/career', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Delete question 1' }))
    await user.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('Question deleted.')).toBeInTheDocument()
    expect(deleted).toBe(true)
  })

  it('creates a trait and shows a duplicate name under the field', async () => {
    mockAdmin()
    server.use(
      http.post('/api/admin/career/traits', () =>
        HttpResponse.json({ title: 'A trait named "Creativity" already exists.', code: 'duplicate_trait' }, { status: 409 })),
    )
    const { user } = renderAt('/admin/career', { as: 'Admin' })

    await user.click(await screen.findByRole('tab', { name: 'Traits' }))
    expect(await screen.findByText('Reasoning with data.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'New trait' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Name'), 'Creativity')
    await user.click(within(dialog).getByRole('button', { name: 'Create trait' }))

    expect(await within(dialog).findByText('A trait named "Creativity" already exists.')).toBeInTheDocument()
  })

  it('edits a career: weights and roadmap steps', async () => {
    mockAdmin()
    let body: unknown = null
    server.use(
      http.put('/api/admin/career/careers/1', async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(careers[0])
      }),
    )
    const { user } = renderAt('/admin/career', { as: 'Admin' })

    await user.click(await screen.findByRole('tab', { name: 'Careers & roadmaps' }))
    expect(await screen.findByText('Analytical thinking 8')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Edit Software Engineer' }))
    const dialog = await screen.findByRole('dialog')

    await user.click(within(dialog).getByRole('button', { name: 'Add step' }))
    const step2 = within(dialog).getByRole('group', { name: 'Step 2' })
    await user.type(within(step2).getByLabelText('Title'), 'Databases')
    await user.selectOptions(within(step2).getByLabelText('Course'), 'CSC 310 · Database Systems')
    await user.click(within(dialog).getByRole('button', { name: 'Move step 2 up' }))
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('Software Engineer saved.')).toBeInTheDocument()
    expect(body).toEqual({
      name: 'Software Engineer',
      description: 'Builds software.',
      isActive: true,
      traits: [{ traitId: 1, weight: 8 }],
      steps: [
        { title: 'Databases', description: null, courseId: 3 },
        { title: 'Learn to program', description: null, courseId: 1 },
      ],
    })
  })

  it('tags a course with traits', async () => {
    mockAdmin()
    let body: unknown = null
    server.use(
      http.put('/api/admin/career/course-tags/3', async ({ request }) => {
        body = await request.json()
        return HttpResponse.json({ ...courseTags[1], traitIds: [2] })
      }),
    )
    const { user } = renderAt('/admin/career', { as: 'Admin' })

    await user.click(await screen.findByRole('tab', { name: 'Course tags' }))
    await user.click(await screen.findByRole('button', { name: 'Edit traits of CSC 310' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByLabelText('Creativity'))
    await user.click(within(dialog).getByRole('button', { name: 'Save tags' }))

    expect(await screen.findByText('Traits of CSC 310 saved.')).toBeInTheDocument()
    expect(body).toEqual({ traitIds: [2] })
  })

  it('shows the error state with a retry button', async () => {
    mockAdmin()
    server.use(http.get('/api/admin/career/questions', () => HttpResponse.json({ title: 'Database unavailable.' }, { status: 500 })))
    renderAt('/admin/career', { as: 'Admin' })
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
