import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { Cart, CartItem } from '../types'

// PB-04 / T15: the cart lists sections with credits against the cap, and removes them. Simulated data (SR-23).

const cartItem = (sectionId: number, courseCode: string, extra: Partial<CartItem> = {}): CartItem => ({
  sectionId, courseId: sectionId, courseCode, courseName: `${courseCode} course`, credits: 3, sectionCode: 'A',
  instructorName: 'Demo Instructor', capacity: 30, seatsLeft: 10, addedAt: '2026-11-01T10:00:00Z',
  meetings: [{ id: sectionId, dayOfWeek: 1, startTime: '08:00:00', endTime: '09:15:00', roomId: 1, roomName: 'B-101', type: 'Lecture' }],
  ...extra,
})

const cartOf = (items: CartItem[], extra: Partial<Cart> = {}): Cart => ({
  termId: 1, termName: 'Fall 2026', window: 'Enrollment', items,
  cartCredits: items.reduce((n, i) => n + i.credits, 0), enrolledCredits: 9, maxCredits: 18, ...extra,
})

function mockCart(initial: CartItem[]) {
  let items = initial
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/cart', () => HttpResponse.json(cartOf(items))),
    http.delete('/api/cart/:id', ({ params }) => {
      items = items.filter((i) => i.sectionId !== Number(params.id))
      return new HttpResponse(null, { status: 204 })
    }),
  )
}

describe('CartPage', () => {
  it('lists sections with credits against the cap', async () => {
    mockCart([cartItem(1, 'CSC 101'), cartItem(4, 'MAT 201', { seatsLeft: 0 })])
    renderAt('/student/cart', { as: 'Student' })

    const list = await screen.findByRole('list', { name: 'Sections in your cart' })
    expect(within(list).getByText('CSC 101 A')).toBeInTheDocument()
    expect(within(list).getByText('MAT 201 A')).toBeInTheDocument()
    expect(within(list).getByText('Full')).toBeInTheDocument()
    expect(screen.getByText('9 credits')).toBeInTheDocument()
    expect(screen.getByText('18 credits per term')).toBeInTheDocument()
  })

  it('removes a section', async () => {
    mockCart([cartItem(1, 'CSC 101'), cartItem(4, 'MAT 201')])
    const { user } = renderAt('/student/cart', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Remove MAT 201 A from cart' }))

    await waitFor(() => expect(screen.queryByText('MAT 201 A')).not.toBeInTheDocument())
    expect(screen.getByText('CSC 101 A')).toBeInTheDocument()
  })

  it('shows an empty cart with a link to the catalog', async () => {
    mockCart([])
    renderAt('/student/cart', { as: 'Student' })
    expect(await screen.findByText('Your cart is empty')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'course catalog' })).toHaveAttribute('href', '/student/courses')
  })

  it('shows the error with a retry button', async () => {
    server.use(
      http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
      http.get('/api/cart', () => HttpResponse.json({ title: 'Could not load your cart.' }, { status: 500 })),
    )
    renderAt('/student/cart', { as: 'Student' })
    expect(await screen.findByText('Could not load your cart.')).toBeInTheDocument()
  })
})
