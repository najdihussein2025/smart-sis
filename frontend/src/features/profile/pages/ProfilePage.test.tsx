import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'

// The signed-in user's own profile, for every role. Simulated data (SR-23).

describe('ProfilePage', () => {
  it('shows a student their contact and study details', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json({ ...profileFor('Student'), dateOfBirth: '2005-03-14' })))
    renderAt('/student/profile', { as: 'Student' })

    expect(await screen.findByText('Studies')).toBeInTheDocument()
    expect(screen.getByText('student@smartsis.local')).toBeInTheDocument()
    expect(screen.getByText('14 Mar 2005')).toBeInTheDocument()
    expect(screen.getByText('Computer Science')).toBeInTheDocument()
    expect(screen.getByText('18 credits')).toBeInTheDocument()
    expect(screen.getByText('Not on file')).toBeInTheDocument()
  })

  it('shows an instructor their department and no study details', async () => {
    server.use(http.get('/api/profile', () => HttpResponse.json(profileFor('Instructor'))))
    renderAt('/instructor/profile', { as: 'Instructor' })

    expect(await screen.findByText('Teaching')).toBeInTheDocument()
    expect(screen.queryByText('Studies')).not.toBeInTheDocument()
  })

})
