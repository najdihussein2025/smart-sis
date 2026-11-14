import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { lookups, paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { Announcement, AnnouncementSection, SaveAnnouncementRequest, SaveSectionAnnouncementRequest } from '../types'

// PB-12 / T47: announcements feed for every role, posting and managing for admins and instructors. Simulated data (SR-23).

const welcome: Announcement = {
  id: 1, title: 'Welcome to the new Smart SIS', body: 'The system now runs on the web.\nReport anything odd.',
  scope: 'All', majorId: null, majorName: null, sectionId: null, sectionLabel: null,
  authorName: 'Demo Admin', authorRole: 'Admin', publishAt: '2026-10-28T08:00:00Z', expiresAt: null, status: 'Live', canManage: false,
}
const advising: Announcement = {
  ...welcome, id: 2, title: 'Computer Science advising hours', body: 'Monday to Thursday, 10:00–12:00.',
  scope: 'Major', majorId: 1, majorName: 'Computer Science', publishAt: '2026-11-01T08:00:00Z', expiresAt: '2026-12-01T08:00:00Z',
}
const labMoved: Announcement = {
  ...welcome, id: 3, title: 'CSC 210 A: lab moved this week', body: 'Starts 15 minutes late.',
  scope: 'Section', sectionId: 12, sectionLabel: 'CSC 210 A', authorName: 'Demo Instructor', authorRole: 'Instructor',
}
const midterms: Announcement = {
  ...welcome, id: 4, title: 'Midterm exam schedule', body: 'Exams run over two weeks.', publishAt: '2099-01-05T08:00:00Z',
  status: 'Scheduled', canManage: true,
}
const library: Announcement = {
  ...welcome, id: 5, title: 'Library closed for inventory', body: 'Closed on Saturday.', expiresAt: '2026-10-10T08:00:00Z',
  status: 'Expired', canManage: true,
}

const sections: AnnouncementSection[] = [
  { id: 11, label: 'CSC 101 A', courseName: 'Introduction to Programming', termName: 'Fall 2026', enrolledCount: 3 },
  { id: 12, label: 'CSC 210 A', courseName: 'Data Structures', termName: 'Fall 2026', enrolledCount: 3 },
]

function mockFeed(items: Announcement[]) {
  server.use(http.get('/api/announcements', () => HttpResponse.json(paged(items, 1, 10))))
}

describe('AnnouncementsPage', () => {
  it('shows a student their feed, newest first, with audience and expiry, and no post button', async () => {
    mockFeed([advising, welcome])
    renderAt('/student/announcements', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Announcements' })
    const items = within(list).getAllByRole('article')
    expect(items.map((a) => within(a).getByRole('heading').textContent)).toEqual([
      'Computer Science advising hours', 'Welcome to the new Smart SIS',
    ])
    expect(within(items[0]!).getByText('Computer Science students')).toBeInTheDocument()
    expect(within(items[0]!).getByText(/^Until/)).toBeInTheDocument()
    expect(within(items[1]!).getByText('Everyone')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /New announcement/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('tablist')).not.toBeInTheDocument()
  })

  it('is in the student navigation', async () => {
    mockFeed([welcome])
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))))
    const { user, router } = renderAt('/student', { as: 'Student' })

    await user.click(within(screen.getAllByRole('navigation', { name: 'Student navigation' })[0]!).getByRole('link', { name: 'Announcements' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/student/announcements'))
    expect(await screen.findByRole('article', { name: 'Welcome to the new Smart SIS' })).toBeInTheDocument()
  })

  it('highlights the announcement a notification links to', async () => {
    mockFeed([advising, welcome])
    renderAt('/student/announcements?announcement=1', { as: 'Student' })

    const target = await screen.findByRole('article', { name: 'Welcome to the new Smart SIS' })
    await waitFor(() => expect(target).toHaveFocus())
    expect(target.className).toContain('ring-2')
    expect(screen.getByRole('article', { name: 'Computer Science advising hours' }).className).not.toContain('ring-2')
  })

  it('shows an empty state when nothing is published', async () => {
    mockFeed([])
    renderAt('/student/announcements', { as: 'Student' })
    expect(await screen.findByText('No announcements yet')).toBeInTheDocument()
  })

  it('shows an error with a retry button when the feed fails', async () => {
    let calls = 0
    server.use(
      http.get('/api/announcements', () => {
        calls++
        return calls === 1
          ? HttpResponse.json({ status: 500, title: 'The feed is unavailable.' }, { status: 500 })
          : HttpResponse.json(paged([welcome]))
      }),
    )
    const { user } = renderAt('/student/announcements', { as: 'Student' })

    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('article', { name: 'Welcome to the new Smart SIS' })).toBeInTheDocument()
  })

  it('lets an admin post to everyone now', async () => {
    mockFeed([welcome])
    const sent: SaveAnnouncementRequest[] = []
    server.use(
      http.post('/api/admin/announcements', async ({ request }) => {
        const body = (await request.json()) as SaveAnnouncementRequest
        sent.push(body)
        return HttpResponse.json({ ...welcome, id: 9, title: body.title, canManage: true }, { status: 201 })
      }),
    )
    const { user } = renderAt('/admin/announcements', { as: 'Admin' })
    await screen.findByRole('article', { name: 'Welcome to the new Smart SIS' })

    await user.click(screen.getByRole('button', { name: /New announcement/ }))
    const dialog = screen.getByRole('dialog', { name: 'New announcement' })
    await user.type(within(dialog).getByLabelText('Title'), 'Campus closed on Monday')
    await user.type(within(dialog).getByLabelText('Message'), 'Public holiday.')
    await user.click(within(dialog).getByRole('button', { name: 'Publish' }))

    expect(await screen.findByText('"Campus closed on Monday" is published.')).toBeInTheDocument()
    expect(sent).toEqual([
      { title: 'Campus closed on Monday', body: 'Public holiday.', scope: 'All', majorId: null, sectionId: null, publishAt: null, expiresAt: null },
    ])
  })

  it('checks the admin form before sending: title, message and the major', async () => {
    mockFeed([])
    const sent: SaveAnnouncementRequest[] = []
    server.use(
      http.get('/api/admin/lookups', () => HttpResponse.json(lookups)),
      http.post('/api/admin/announcements', async ({ request }) => {
        sent.push((await request.json()) as SaveAnnouncementRequest)
        return HttpResponse.json({ ...advising, canManage: true }, { status: 201 })
      }),
    )
    const { user } = renderAt('/admin/announcements', { as: 'Admin' })
    await user.click((await screen.findAllByRole('button', { name: /New announcement/ }))[0]!)
    const dialog = screen.getByRole('dialog', { name: 'New announcement' })

    await user.click(within(dialog).getByRole('button', { name: 'Publish' }))
    expect(await within(dialog).findByText('Enter a title.')).toBeInTheDocument()
    expect(within(dialog).getByText('Write the announcement.')).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText('Title'), 'Advising hours')
    await user.type(within(dialog).getByLabelText('Message'), 'Room B-204.')
    await user.selectOptions(within(dialog).getByLabelText('Audience'), 'Students of one major')
    await user.click(within(dialog).getByRole('button', { name: 'Publish' }))
    expect(await within(dialog).findByText('Choose a major.')).toBeInTheDocument()
    expect(sent).toHaveLength(0)

    await waitFor(() => expect(within(dialog).getByLabelText('Major')).toBeEnabled())
    await user.selectOptions(within(dialog).getByLabelText('Major'), 'Computer Science')
    await user.click(within(dialog).getByRole('button', { name: 'Publish' }))
    await waitFor(() => expect(sent).toHaveLength(1))
    expect(sent[0]).toMatchObject({ scope: 'Major', majorId: 1, sectionId: null })
  })

  it('shows the API field error under the field', async () => {
    mockFeed([])
    server.use(
      http.post('/api/admin/announcements', () =>
        HttpResponse.json(
          { status: 400, title: 'One or more validation errors occurred.', errors: { title: ['Use at most 200 characters.'] } },
          { status: 400 },
        ),
      ),
    )
    const { user } = renderAt('/admin/announcements', { as: 'Admin' })
    await user.click((await screen.findAllByRole('button', { name: /New announcement/ }))[0]!)
    const dialog = screen.getByRole('dialog', { name: 'New announcement' })
    await user.type(within(dialog).getByLabelText('Title'), 'Too long, says the server')
    await user.type(within(dialog).getByLabelText('Message'), 'Body.')
    await user.click(within(dialog).getByRole('button', { name: 'Publish' }))

    expect(await within(dialog).findByText('Use at most 200 characters.')).toBeInTheDocument()
    expect(within(dialog).getByLabelText('Title')).toHaveAttribute('aria-invalid', 'true')
  })

  it('lets an instructor post to one of their sections, and shows a 403 from the API', async () => {
    mockFeed([labMoved])
    const sent: SaveSectionAnnouncementRequest[] = []
    server.use(
      http.get('/api/instructor/announcements/sections', () => HttpResponse.json(sections)),
      http.post('/api/instructor/announcements', async ({ request }) => {
        const body = (await request.json()) as SaveSectionAnnouncementRequest
        sent.push(body)
        return sent.length === 1
          ? HttpResponse.json({ status: 403, title: "You don't teach CSC 101 A, so you can't post to it." }, { status: 403 })
          : HttpResponse.json({ ...labMoved, id: 10, title: body.title, canManage: true }, { status: 201 })
      }),
    )
    const { user } = renderAt('/instructor/announcements', { as: 'Instructor' })
    await user.click((await screen.findAllByRole('button', { name: /New announcement/ }))[0]!)
    const dialog = screen.getByRole('dialog', { name: 'New announcement' })
    expect(within(dialog).queryByLabelText('Audience')).not.toBeInTheDocument()

    await waitFor(() => expect(within(dialog).getByLabelText('Section')).toBeEnabled())
    await user.selectOptions(within(dialog).getByLabelText('Section'), 'CSC 101 A · Introduction to Programming (3 enrolled)')
    await user.type(within(dialog).getByLabelText('Title'), 'Quiz on Thursday')
    await user.type(within(dialog).getByLabelText('Message'), 'Chapters 3 and 4.')
    await user.click(within(dialog).getByRole('button', { name: 'Publish' }))

    expect(await within(dialog).findByText("You don't teach CSC 101 A, so you can't post to it.")).toBeInTheDocument()
    expect(sent[0]).toEqual({ title: 'Quiz on Thursday', body: 'Chapters 3 and 4.', sectionId: 11, publishAt: null, expiresAt: null })

    await user.selectOptions(within(dialog).getByLabelText('Section'), 'CSC 210 A · Data Structures (3 enrolled)')
    expect(within(dialog).queryByText("You don't teach CSC 101 A, so you can't post to it.")).not.toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Publish' }))
    expect(await screen.findByText('"Quiz on Thursday" is published.')).toBeInTheDocument()
    expect(sent[1]?.sectionId).toBe(12)
  })

  it('asks for a future publish time when scheduling', async () => {
    mockFeed([])
    server.use(http.get('/api/instructor/announcements/sections', () => HttpResponse.json(sections)))
    const { user } = renderAt('/instructor/announcements', { as: 'Instructor' })
    await user.click((await screen.findAllByRole('button', { name: /New announcement/ }))[0]!)
    const dialog = screen.getByRole('dialog', { name: 'New announcement' })

    await user.click(within(dialog).getByLabelText('Publish later'))
    expect(within(dialog).getByRole('group', { name: 'Publish at' })).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Schedule' }))
    expect(await within(dialog).findByText('Choose when to publish it.')).toBeInTheDocument()
  })

  it('lists all posts for an admin with a status filter, and deletes after confirmation', async () => {
    mockFeed([welcome])
    const queries: URLSearchParams[] = []
    const deleted: string[] = []
    let items = [midterms, library]
    server.use(
      http.get('/api/admin/announcements', ({ request }) => {
        const q = new URL(request.url).searchParams
        queries.push(q)
        return HttpResponse.json(paged(items.filter((a) => !q.get('status') || a.status === q.get('status'))))
      }),
      http.delete('/api/admin/announcements/:id', ({ params }) => {
        deleted.push(String(params.id))
        items = items.filter((a) => String(a.id) !== params.id)
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { user } = renderAt('/admin/announcements', { as: 'Admin' })
    await screen.findByRole('article', { name: 'Welcome to the new Smart SIS' })

    await user.click(screen.getByRole('tab', { name: 'All posts' }))
    const scheduled = await screen.findByRole('article', { name: 'Midterm exam schedule' })
    expect(within(scheduled).getByText('Scheduled')).toBeInTheDocument()
    expect(within(screen.getByRole('article', { name: 'Library closed for inventory' })).getByText('Expired')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Status'), 'Expired')
    await waitFor(() => expect(screen.queryByRole('article', { name: 'Midterm exam schedule' })).not.toBeInTheDocument())
    expect(queries.at(-1)?.get('status')).toBe('Expired')

    await user.click(screen.getByRole('button', { name: 'Delete "Library closed for inventory"' }))
    const confirm = screen.getByRole('dialog', { name: 'Delete announcement?' })
    await user.click(within(confirm).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('"Library closed for inventory" was deleted.')).toBeInTheDocument()
    expect(deleted).toEqual(['5'])
    expect(await screen.findByText('Nothing posted here')).toBeInTheDocument()
  })
})
