import { describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { act, screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { paged, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import { NOTIFICATIONS_REFRESH_MS } from '../hooks'
import type { AppNotification } from '../types'

// PB-12 / T48: the bell in the top bar — unread badge, latest notifications, mark read, open the link. Simulated data (SR-23).

const lab: AppNotification = {
  id: 5, type: 'Announcement', title: 'CSC 210 A: lab moved this week', body: 'Starts 15 minutes late.',
  link: '/student/announcements?announcement=7', createdAt: '2026-11-02T08:00:00Z', readAt: null,
}
const welcome: AppNotification = {
  id: 4, type: 'Announcement', title: 'Welcome to the new Smart SIS', body: 'The system now runs on the web.',
  link: '/student/announcements?announcement=1', createdAt: '2026-10-28T08:00:00Z', readAt: '2026-10-29T08:00:00Z',
}

function mockBell(items: AppNotification[] = [lab, welcome]) {
  const calls = { read: [] as string[], readAll: 0, counts: 0 }
  let unread = items.filter((n) => !n.readAt).length
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/announcements', () => HttpResponse.json(paged([]))),
    http.get('/api/notifications/unread-count', () => {
      calls.counts++
      return HttpResponse.json({ count: unread })
    }),
    http.get('/api/notifications', () => HttpResponse.json(paged(items, 1, 10))),
    http.post('/api/notifications/:id/read', ({ params }) => {
      calls.read.push(String(params.id))
      unread = Math.max(0, unread - 1)
      return new HttpResponse(null, { status: 204 })
    }),
    http.post('/api/notifications/read-all', () => {
      calls.readAll++
      unread = 0
      return new HttpResponse(null, { status: 204 })
    }),
  )
  return calls
}

describe('NotificationBell', () => {
  it('shows the unread count and the latest notifications, unread ones marked', async () => {
    mockBell()
    const { user } = renderAt('/student', { as: 'Student' })

    const bell = await screen.findByRole('button', { name: 'Notifications, 1 unread' })
    expect(bell).toHaveAttribute('aria-expanded', 'false')
    await user.click(bell)

    const panel = screen.getByRole('region', { name: 'Notifications' })
    const items = within(await within(panel).findByRole('list', { name: 'Latest notifications' })).getAllByRole('button')
    expect(items.map((b) => b.textContent)).toEqual([
      expect.stringContaining('Unread: CSC 210 A: lab moved this week'),
      expect.not.stringContaining('Unread:'),
    ])
    expect(bell).toHaveAttribute('aria-expanded', 'true')
  })

  it('opening a notification marks it read and goes to its link', async () => {
    const calls = mockBell()
    const { user, router } = renderAt('/student', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Notifications, 1 unread' }))
    await user.click(await screen.findByRole('button', { name: /CSC 210 A: lab moved this week/ }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/student/announcements'))
    expect(router.state.location.search).toBe('?announcement=7')
    expect(calls.read).toEqual(['5'])
    expect(screen.queryByRole('region', { name: 'Notifications' })).not.toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Notifications' })).toBeInTheDocument()
  })

  it('opening a read notification does not mark it again', async () => {
    const calls = mockBell()
    const { user, router } = renderAt('/student', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Notifications, 1 unread' }))
    await user.click(await screen.findByRole('button', { name: /Welcome to the new Smart SIS/ }))

    await waitFor(() => expect(router.state.location.search).toBe('?announcement=1'))
    expect(calls.read).toEqual([])
  })

  it('marks all as read', async () => {
    const calls = mockBell([lab, { ...welcome, readAt: null }])
    const { user } = renderAt('/student', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Notifications, 2 unread' }))
    await user.click(screen.getByRole('button', { name: 'Mark all as read' }))

    expect(await screen.findByRole('button', { name: 'Notifications' })).toBeInTheDocument()
    expect(calls.readAll).toBe(1)
    expect(screen.getByRole('button', { name: 'Mark all as read' })).toBeDisabled()
  })

  it('says so when there is nothing, and closes with Escape', async () => {
    mockBell([])
    const { user } = renderAt('/student', { as: 'Student' })

    const bell = await screen.findByRole('button', { name: 'Notifications' })
    await user.click(bell)
    expect(await screen.findByText(/You're all caught up/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Mark all as read' })).toBeDisabled()

    await user.keyboard('{Escape}')
    expect(screen.queryByRole('region', { name: 'Notifications' })).not.toBeInTheDocument()
    expect(bell).toHaveFocus()
  })

  it('shows an error with a retry button when the list fails', async () => {
    mockBell()
    let calls = 0
    server.use(
      http.get('/api/notifications', () => {
        calls++
        return calls === 1
          ? HttpResponse.json({ status: 500, title: 'Notifications are unavailable.' }, { status: 500 })
          : HttpResponse.json(paged([lab]))
      }),
    )
    const { user } = renderAt('/student', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Notifications, 1 unread' }))
    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('button', { name: /CSC 210 A: lab moved this week/ })).toBeInTheDocument()
  })

  it('asks for the unread count again every minute', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    const calls = mockBell()
    renderAt('/student', { as: 'Student' })
    await screen.findByRole('button', { name: 'Notifications, 1 unread' })
    const before = calls.counts

    await act(async () => {
      await vi.advanceTimersByTimeAsync(NOTIFICATIONS_REFRESH_MS)
    })

    await waitFor(() => expect(calls.counts).toBeGreaterThan(before))
  })
})
