import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, within } from '@testing-library/react'
import { server } from '@/test/server'
import { lookups, profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'

// Read-only view of the admin reference lists. Simulated data only (SR-23).

function mockLookups(body: typeof lookups = lookups) {
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
    http.get('/api/admin/lookups', () => HttpResponse.json(body)),
  )
}

describe('LookupsPage', () => {
  it('shows each list in a tab with its count, and majors with department code and credit cap', async () => {
    mockLookups()
    const { user } = renderAt('/admin/lookups', { as: 'Admin' })

    expect(await screen.findByRole('heading', { level: 1, name: 'Lookups' })).toBeInTheDocument()
    expect(screen.getByText(/Editing these lists isn’t available in the app yet/)).toBeInTheDocument()
    expect(screen.queryByText(/build phase/i)).not.toBeInTheDocument()

    expect(await screen.findByRole('tab', { name: 'Departments, 2' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Rooms, 3' })).toBeInTheDocument()
    const departments = screen.getByRole('table', { name: 'Departments' })
    expect(within(departments).getByRole('rowheader', { name: 'Mathematics' })).toBeInTheDocument()
    expect(within(departments).getByText('MAT')).toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Majors, 2' }))
    const row = screen.getByRole('rowheader', { name: 'Computer Science' }).closest('tr')!
    expect(within(row).getByText('CSC')).toBeInTheDocument()
    expect(within(row).getByText('18 cr')).toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Rooms, 3' }))
    const lab = screen.getByRole('rowheader', { name: 'Lab-3' }).closest('tr')!
    expect(within(lab).getByText('25 seats')).toBeInTheDocument()
  })

  it('filters the open list with the search box and clears it', async () => {
    mockLookups()
    const { user } = renderAt('/admin/lookups', { as: 'Admin' })
    await user.click(await screen.findByRole('tab', { name: 'Rooms, 3' }))

    await user.type(screen.getByLabelText('Search rooms'), 'b-2')
    expect(screen.getByRole('rowheader', { name: 'B-204' })).toBeInTheDocument()
    expect(screen.queryByRole('rowheader', { name: 'B-101' })).not.toBeInTheDocument()
    expect(screen.getByText('1 of 3 rooms')).toBeInTheDocument()

    await user.clear(screen.getByLabelText('Search rooms'))
    await user.type(screen.getByLabelText('Search rooms'), 'zzz')
    expect(screen.getByText('No rooms match')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Clear search' }))
    expect(screen.getByRole('rowheader', { name: 'B-101' })).toBeInTheDocument()
  })

  it('shows an empty state for an empty list', async () => {
    mockLookups({ ...lookups, instructors: [] })
    const { user } = renderAt('/admin/lookups', { as: 'Admin' })

    await user.click(await screen.findByRole('tab', { name: 'Instructors, 0' }))
    expect(screen.getByText('No instructors yet')).toBeInTheDocument()
  })

  it('shows an error with a retry button', async () => {
    let calls = 0
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Admin'))),
      http.get('/api/admin/lookups', () => {
        calls++
        return calls === 1 ? HttpResponse.json({ title: 'Something went wrong.' }, { status: 500 }) : HttpResponse.json(lookups)
      }),
    )
    const { user } = renderAt('/admin/lookups', { as: 'Admin' })

    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('table', { name: 'Departments' })).toBeInTheDocument()
  })
})
