import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { server } from '@/test/server'
import { lookups, paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { CreateUserRequest, UpdateUserRequest, UserDetail, UserListItem } from '../types'

// PB-01 / T11: admin searches, creates, edits and deactivates accounts. Simulated users only (SR-23).

const admin: UserListItem = { id: 1, universityId: 1001, fullName: 'Demo Admin', email: 'admin@smartsis.local', role: 'Admin', isActive: true, majorName: null, departmentName: null }
const instructor: UserListItem = { id: 2, universityId: 2001, fullName: 'Demo Instructor', email: 'instructor@smartsis.local', role: 'Instructor', isActive: true, majorName: null, departmentName: 'Computer Science' }
const student: UserListItem = { id: 3, universityId: 3001, fullName: 'Demo Student', email: 'student@smartsis.local', role: 'Student', isActive: true, majorName: 'Computer Science', departmentName: null }
const former: UserListItem = { id: 4, universityId: 3009, fullName: 'Simulated Former Student', email: 'former@smartsis.local', role: 'Student', isActive: false, majorName: 'Computer Science', departmentName: null }

const studentDetail: UserDetail = {
  id: 3, universityId: 3001, fullName: 'Demo Student', email: 'student@smartsis.local', role: 'Student',
  phone: null, dateOfBirth: '2005-03-14', photoUrl: null, isActive: true, deletedAt: null,
  student: { majorId: 1, majorName: 'Computer Science', campusId: 1, campusName: 'Main Campus', enrollmentDate: '2024-09-01', maxCreditsOverride: null, effectiveMaxCredits: 18 },
  instructor: null,
}

type Sent = { method: string; url: string; body?: CreateUserRequest | UpdateUserRequest }

function mockUsers(all: UserListItem[] = [admin, instructor, student, former]) {
  const sent: Sent[] = []
  const queries: URLSearchParams[] = []
  let users = [...all]
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/lookups', () => HttpResponse.json(lookups)),
    http.get('/api/admin/users', ({ request }) => {
      const q = new URL(request.url).searchParams
      queries.push(q)
      const s = (q.get('search') ?? '').toLowerCase()
      const status = q.get('status') ?? 'Active'
      const hits = users.filter(
        (u) =>
          (!s || `${u.fullName} ${u.email} ${u.universityId}`.toLowerCase().includes(s)) &&
          (!q.get('role') || u.role === q.get('role')) &&
          (status === 'All' || u.isActive === (status === 'Active')),
      )
      return HttpResponse.json(paged(hits))
    }),
    http.get('/api/admin/users/3', () => HttpResponse.json(studentDetail)),
    http.post('/api/admin/users', async ({ request }) => {
      const body = (await request.json()) as CreateUserRequest
      sent.push({ method: 'POST', url: '/api/admin/users', body })
      return HttpResponse.json({ ...studentDetail, id: 9, fullName: body.fullName }, { status: 201 })
    }),
    http.put('/api/admin/users/:id', async ({ request, params }) => {
      const body = (await request.json()) as UpdateUserRequest
      sent.push({ method: 'PUT', url: `/api/admin/users/${params.id}`, body })
      return HttpResponse.json({ ...studentDetail, fullName: body.fullName })
    }),
    http.post('/api/admin/users/:id/deactivate', ({ params }) => {
      sent.push({ method: 'POST', url: `/api/admin/users/${params.id}/deactivate` })
      users = users.map((u) => (u.id === Number(params.id) ? { ...u, isActive: false } : u))
      return new HttpResponse(null, { status: 204 })
    }),
  )
  return { sent, queries }
}

async function openNew(user: UserEvent) {
  await user.click((await screen.findAllByRole('button', { name: /New user/ }))[0]!)
  const dialog = screen.getByRole('dialog', { name: 'New student' })
  await waitFor(() => expect(within(dialog).getByLabelText('Major')).toBeEnabled())
  return dialog
}

async function fillStudent(user: UserEvent, dialog: HTMLElement, id = '3002', email = 'student3002@smartsis.local') {
  await user.type(within(dialog).getByLabelText('University ID'), id)
  await user.type(within(dialog).getByLabelText('Full name'), 'Simulated Student Two')
  await user.type(within(dialog).getByLabelText('Email'), email)
  await user.type(within(dialog).getByLabelText('Initial password'), 'Welcome2027')
  await user.selectOptions(within(dialog).getByLabelText('Major'), 'Computer Science')
  await user.selectOptions(within(dialog).getByLabelText('Campus'), 'Main Campus')
  const enrolled = within(dialog).getByLabelText('Enrollment date')
  await user.clear(enrolled)
  await user.type(enrolled, '01092026')
  await user.tab()
}

describe('UsersPage', () => {
  it('lists active accounts with ID, role, major or department and status', async () => {
    const { queries } = mockUsers()
    renderAt('/admin/users', { as: 'Admin' })

    const row = (await screen.findByRole('rowheader', { name: /Demo Student/ })).closest('tr')!
    expect(within(row).getByText('3001')).toBeInTheDocument()
    expect(within(row).getByText('Student')).toBeInTheDocument()
    expect(within(row).getByText('Computer Science')).toBeInTheDocument()
    expect(within(row).getAllByText('Active').length).toBeGreaterThan(0)
    expect(screen.queryByRole('rowheader', { name: /Simulated Former Student/ })).not.toBeInTheDocument()
    expect(queries[0]?.get('status')).toBe('Active')

    // Admin accounts (and your own) can't be edited or deactivated here.
    expect(screen.queryByRole('button', { name: 'Edit Demo Admin' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Deactivate Demo Admin' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Deactivate Demo Instructor' })).toBeInTheDocument()
  })

  it('searches by name or ID and filters by role and status', async () => {
    const { queries } = mockUsers()
    const { user } = renderAt('/admin/users', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: /Demo Student/ })

    await user.type(screen.getByLabelText('Search'), '2001')
    await waitFor(() => expect(screen.queryByRole('rowheader', { name: /Demo Student/ })).not.toBeInTheDocument())
    expect(screen.getByRole('rowheader', { name: /Demo Instructor/ })).toBeInTheDocument()
    expect(queries.at(-1)?.get('search')).toBe('2001')

    await user.clear(screen.getByLabelText('Search'))
    await user.selectOptions(screen.getByLabelText('Role'), 'Students')
    await user.selectOptions(screen.getByLabelText('Status'), 'Inactive')
    const row = (await screen.findByRole('rowheader', { name: /Simulated Former Student/ })).closest('tr')!
    expect(within(row).getAllByText('Inactive').length).toBeGreaterThan(0)
    // A deactivated account can't be edited or deactivated again.
    expect(within(row).queryByRole('button')).not.toBeInTheDocument()
    expect(queries.at(-1)?.get('role')).toBe('Student')
    expect(queries.at(-1)?.get('status')).toBe('Inactive')
  })

  it('shows an empty state when nothing matches', async () => {
    mockUsers()
    const { user } = renderAt('/admin/users', { as: 'Admin' })
    await screen.findByRole('rowheader', { name: /Demo Student/ })

    await user.type(screen.getByLabelText('Search'), 'zzz')

    expect(await screen.findByText('No users match')).toBeInTheDocument()
  })

  it('shows an error with a retry button when the list fails', async () => {
    mockUsers()
    let calls = 0
    server.use(
      http.get('/api/admin/users', () => {
        calls++
        return calls === 1 ? HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 }) : HttpResponse.json(paged([student]))
      }),
    )
    const { user } = renderAt('/admin/users', { as: 'Admin' })

    expect(await screen.findByText('Something went wrong.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('rowheader', { name: /Demo Student/ })).toBeInTheDocument()
  })

  it('creates a student', async () => {
    const { sent } = mockUsers()
    const { user } = renderAt('/admin/users', { as: 'Admin' })
    const dialog = await openNew(user)

    await fillStudent(user, dialog)
    await user.type(within(dialog).getByLabelText('Credit cap per term (optional)'), '21')
    await user.click(within(dialog).getByRole('button', { name: 'Create student' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(sent).toEqual([
      {
        method: 'POST',
        url: '/api/admin/users',
        body: {
          universityId: 3002, role: 'Student', initialPassword: 'Welcome2027',
          email: 'student3002@smartsis.local', fullName: 'Simulated Student Two', phone: null, dateOfBirth: null,
          student: { majorId: 1, campusId: 1, enrollmentDate: '2026-09-01', maxCreditsOverride: 21 },
          instructor: null,
        },
      },
    ])
    expect(screen.getByText('Simulated Student Two saved.')).toBeInTheDocument()
  })

  it('creates an instructor with a department', async () => {
    const { sent } = mockUsers()
    const { user } = renderAt('/admin/users', { as: 'Admin' })
    await openNew(user)
    await user.click(screen.getByRole('radio', { name: 'Instructor' }))
    const dialog = screen.getByRole('dialog', { name: 'New instructor' })

    await user.type(within(dialog).getByLabelText('University ID'), '2002')
    await user.type(within(dialog).getByLabelText('Full name'), 'Simulated Instructor Two')
    await user.type(within(dialog).getByLabelText('Email'), 'instructor2002@smartsis.local')
    await user.type(within(dialog).getByLabelText('Initial password'), 'Welcome2027')
    expect(within(dialog).queryByLabelText('Major')).not.toBeInTheDocument()
    await user.selectOptions(within(dialog).getByLabelText('Department'), 'MAT · Mathematics')
    await user.click(within(dialog).getByRole('button', { name: 'Create instructor' }))

    await waitFor(() => expect(sent).toHaveLength(1))
    expect(sent[0]?.body).toMatchObject({ universityId: 2002, role: 'Instructor', student: null, instructor: { departmentId: 2 } })
  })

  it('checks the fields before sending', async () => {
    const { sent } = mockUsers()
    const { user } = renderAt('/admin/users', { as: 'Admin' })
    const dialog = await openNew(user)

    await user.type(within(dialog).getByLabelText('University ID'), '12')
    await user.type(within(dialog).getByLabelText('Email'), 'not-an-email')
    await user.type(within(dialog).getByLabelText('Initial password'), 'password')
    await user.type(within(dialog).getByLabelText('Credit cap per term (optional)'), '31')
    await user.click(within(dialog).getByRole('button', { name: 'Create student' }))

    expect(await within(dialog).findByText('University ID must have 4 to 9 digits.')).toBeInTheDocument()
    expect(within(dialog).getByText('Enter the full name.')).toBeInTheDocument()
    expect(within(dialog).getByText('Enter a valid email address.')).toBeInTheDocument()
    expect(within(dialog).getByText('Include at least one number.')).toBeInTheDocument()
    expect(within(dialog).getByText('Choose a major.')).toBeInTheDocument()
    expect(within(dialog).getByText('Credit cap override must be between 1 and 30.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)
  })

  it('shows duplicate University ID and email (409) under their fields', async () => {
    mockUsers()
    let reply = { status: 409, title: 'University ID 3001 is already in use.', code: 'duplicate_university_id' }
    server.use(http.post('/api/admin/users', () => HttpResponse.json(reply, { status: 409 })))
    const { user } = renderAt('/admin/users', { as: 'Admin' })
    const dialog = await openNew(user)
    await fillStudent(user, dialog, '3001', 'student@smartsis.local')

    await user.click(within(dialog).getByRole('button', { name: 'Create student' }))
    const id = within(dialog).getByLabelText('University ID')
    await waitFor(() => expect(id).toHaveAttribute('aria-invalid', 'true'))
    expect(within(dialog).getByText(reply.title)).toBeInTheDocument()

    reply = { status: 409, title: 'Email student@smartsis.local is already in use.', code: 'duplicate_email' }
    await user.clear(id)
    await user.type(id, '3002')
    await user.click(within(dialog).getByRole('button', { name: 'Create student' }))
    const email = within(dialog).getByLabelText('Email')
    await waitFor(() => expect(email).toHaveAttribute('aria-invalid', 'true'))
    expect(within(dialog).getByText(reply.title)).toBeInTheDocument()
  })

  it('shows the API\'s 400 field errors under nested fields', async () => {
    mockUsers()
    server.use(
      http.post('/api/admin/users', () =>
        HttpResponse.json(
          { status: 400, title: 'Some fields are not valid.', errors: { 'student.campusId': ["Campus 1 doesn't exist."] } },
          { status: 400 },
        ),
      ),
    )
    const { user } = renderAt('/admin/users', { as: 'Admin' })
    const dialog = await openNew(user)
    await fillStudent(user, dialog)

    await user.click(within(dialog).getByRole('button', { name: 'Create student' }))

    expect(await within(dialog).findByText("Campus 1 doesn't exist.")).toBeInTheDocument()
  })

  it('edits a student with the saved values, without University ID, role or password', async () => {
    const { sent } = mockUsers()
    const { user } = renderAt('/admin/users', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Edit Demo Student' }))
    const dialog = await screen.findByRole('dialog', { name: 'Edit Demo Student' })
    expect(await within(dialog).findByLabelText('Full name')).toHaveValue('Demo Student')
    expect(within(dialog).getByLabelText('Date of birth (optional)')).toHaveValue('14/03/2005')
    expect(within(dialog).getByLabelText('Enrollment date')).toHaveValue('01/09/2024')
    expect(within(dialog).queryByLabelText('University ID')).not.toBeInTheDocument()
    expect(within(dialog).queryByLabelText('Initial password')).not.toBeInTheDocument()
    await waitFor(() => expect(within(dialog).getByLabelText('Major')).toHaveValue('1'))

    await user.type(within(dialog).getByLabelText('Phone (optional)'), '+961 1 000000')
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(sent).toHaveLength(1))
    expect(sent[0]).toEqual({
      method: 'PUT',
      url: '/api/admin/users/3',
      body: {
        email: 'student@smartsis.local', fullName: 'Demo Student', phone: '+961 1 000000', dateOfBirth: '2005-03-14',
        student: { majorId: 1, campusId: 1, enrollmentDate: '2024-09-01', maxCreditsOverride: null },
        instructor: null,
      },
    })
  })

  it('deactivates through a confirm dialog, and Cancel does nothing', async () => {
    const { sent } = mockUsers()
    const { user } = renderAt('/admin/users', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Deactivate Demo Student' }))
    let confirm = screen.getByRole('dialog', { name: 'Deactivate Demo Student?' })
    await user.click(within(confirm).getByRole('button', { name: 'Cancel' }))
    expect(sent).toHaveLength(0)

    await user.click(screen.getByRole('button', { name: 'Deactivate Demo Student' }))
    confirm = screen.getByRole('dialog', { name: 'Deactivate Demo Student?' })
    await user.click(within(confirm).getByRole('button', { name: 'Deactivate' }))

    expect(await screen.findByText('Demo Student deactivated.')).toBeInTheDocument()
    expect(sent).toEqual([{ method: 'POST', url: '/api/admin/users/3/deactivate' }])
    await waitFor(() => expect(screen.queryByRole('rowheader', { name: /Demo Student/ })).not.toBeInTheDocument())
  })

  it('shows a 409 from deactivation inside the confirm dialog', async () => {
    mockUsers()
    server.use(
      http.post('/api/admin/users/:id/deactivate', () =>
        HttpResponse.json({ status: 409, title: "You can't deactivate your own account.", code: 'cannot_deactivate_self' }, { status: 409 }),
      ),
    )
    const { user } = renderAt('/admin/users', { as: 'Admin' })

    await user.click(await screen.findByRole('button', { name: 'Deactivate Demo Instructor' }))
    const confirm = screen.getByRole('dialog', { name: 'Deactivate Demo Instructor?' })
    await user.click(within(confirm).getByRole('button', { name: 'Deactivate' }))

    expect(await within(confirm).findByText("You can't deactivate your own account.")).toBeInTheDocument()
  })
})
