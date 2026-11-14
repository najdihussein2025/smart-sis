import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import { server } from '@/test/server'
import { profileFor } from '@/test/fixtures'
import { renderAt } from '@/test/render'
import type { EnrolledSection, EnrollResult, MyEnrollments } from '@/features/enrollment/types'
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

const enrolledCsc210A: EnrolledSection = {
  enrollmentId: 7, sectionId: 21, courseId: 2, courseCode: 'CSC 210', courseName: 'Data Structures', credits: 3, sectionCode: 'A',
  instructorName: 'Demo Instructor', meetings: [], enrolledAt: '2026-11-01T10:00:00Z',
}

const mine = (items: EnrolledSection[], window: MyEnrollments['window'] = 'Enrollment'): MyEnrollments => ({
  termId: 1, termName: 'Fall 2026', window, enrolledCredits: items.length * 3, maxCredits: 18, items,
})

function mockCart(initial: CartItem[], { enrolled = [] as EnrolledSection[], window = 'Enrollment' as Cart['window'] } = {}) {
  let items = initial
  server.use(
    http.get('/api/profile', () => HttpResponse.json(profileFor('Student'))),
    http.get('/api/cart', () => HttpResponse.json(cartOf(items, { window }))),
    http.get('/api/enrollment/my', () => HttpResponse.json(mine(enrolled, window))),
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
      http.get('/api/enrollment/my', () => HttpResponse.json(mine([]))),
      http.get('/api/cart', () => HttpResponse.json({ title: 'Could not load your cart.' }, { status: 500 })),
    )
    renderAt('/student/cart', { as: 'Student' })
    expect(await screen.findByText('Could not load your cart.')).toBeInTheDocument()
  })

  it('enrolls the chosen sections and shows each rule failure with its reason', async () => {
    mockCart([cartItem(1, 'CSC 101'), cartItem(3, 'CSC 310'), cartItem(4, 'MAT 201')])
    let sent: number[] = []
    const result: EnrollResult = {
      enrolledCount: 1,
      results: [
        { sectionId: 1, label: 'CSC 101 A', success: true, message: null, code: null },
        { sectionId: 3, label: 'CSC 310 A', success: false, message: 'You need to pass CSC 210 (D or better) before CSC 310.', code: 'prerequisite_missing' },
      ],
    }
    server.use(
      http.post('/api/enrollment/enroll', async ({ request }) => {
        sent = ((await request.json()) as { sectionIds: number[] }).sectionIds
        return HttpResponse.json(result)
      }),
    )
    const { user } = renderAt('/student/cart', { as: 'Student' })

    const enrollButton = await screen.findByRole('button', { name: /Enroll in/ })
    expect(enrollButton).toBeDisabled()
    await user.click(screen.getByRole('checkbox', { name: 'Select CSC 101 A' }))
    await user.click(screen.getByRole('checkbox', { name: 'Select CSC 310 A' }))
    await user.click(screen.getByRole('button', { name: 'Enroll in 2 sections' }))

    const results = await screen.findByRole('region', { name: 'Enrollment results' })
    expect(sent).toEqual([1, 3])
    expect(within(results).getByText('Enrolled in 1 section. 1 could not be enrolled.')).toBeInTheDocument()
    expect(within(results).getByText(/Prerequisite:/)).toBeInTheDocument()
    expect(within(results).getByText('You need to pass CSC 210 (D or better) before CSC 310.')).toBeInTheDocument()
  })

  it('select all picks every section', async () => {
    mockCart([cartItem(1, 'CSC 101'), cartItem(4, 'MAT 201')])
    const { user } = renderAt('/student/cart', { as: 'Student' })
    await user.click(await screen.findByRole('checkbox', { name: 'Select all' }))
    expect(screen.getByRole('button', { name: 'Enroll in 2 sections' })).toBeEnabled()
  })

  it('cannot enroll while registration is closed', async () => {
    mockCart([cartItem(1, 'CSC 101')], { window: 'Closed' })
    const { user } = renderAt('/student/cart', { as: 'Student' })
    await user.click(await screen.findByRole('checkbox', { name: 'Select CSC 101 A' }))
    expect(screen.getByRole('button', { name: /Enroll in/ })).toBeDisabled()
    expect(screen.getByText(/Registration is closed/)).toBeInTheDocument()
  })

  it('swaps an enrolled section for a cart section, and shows a refused swap', async () => {
    mockCart([cartItem(22, 'CSC 210', { courseId: 2, sectionCode: 'B' })], { enrolled: [enrolledCsc210A] })
    let swaps = 0
    server.use(
      http.post('/api/enrollment/swap', async ({ request }) => {
        swaps++
        const body = (await request.json()) as { fromSectionId: number; toSectionId: number }
        expect(body).toEqual({ fromSectionId: 21, toSectionId: 22 })
        return swaps === 1
          ? HttpResponse.json({ title: 'Section CSC 210 B is full.', code: 'section_full' }, { status: 409 })
          : HttpResponse.json(mine([]))
      }),
    )
    const { user } = renderAt('/student/cart', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Swap into CSC 210 B' }))
    const dialog = screen.getByRole('dialog', { name: 'Swap into CSC 210 B' })
    // The other section of the same course is chosen already.
    expect(within(dialog).getByRole('radio')).toBeChecked()
    await user.click(within(dialog).getByRole('button', { name: 'Swap' }))
    expect(await within(dialog).findByText('Section CSC 210 B is full.')).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Swap' }))
    expect(await screen.findByText('Swapped CSC 210 A for CSC 210 B.')).toBeInTheDocument()
  })

  it('drops an enrolled course after confirming, during add/drop', async () => {
    let enrolled = [enrolledCsc210A]
    mockCart([], { window: 'AddDrop' })
    server.use(
      http.get('/api/enrollment/my', () => HttpResponse.json(mine(enrolled, 'AddDrop'))),
      http.post('/api/enrollment/drop', async ({ request }) => {
        expect(await request.json()).toEqual({ sectionId: 21 })
        enrolled = []
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { user } = renderAt('/student/cart', { as: 'Student' })

    await user.click(await screen.findByRole('button', { name: 'Drop CSC 210 A' }))
    const dialog = screen.getByRole('dialog', { name: 'Drop CSC 210 A?' })
    await user.click(within(dialog).getByRole('button', { name: 'Drop course' }))

    expect(await screen.findByText(/Dropped CSC 210 A/)).toBeInTheDocument()
    expect(await screen.findByText('No courses yet')).toBeInTheDocument()
  })

  it('cancelling the confirmation keeps the course', async () => {
    mockCart([], { window: 'AddDrop', enrolled: [enrolledCsc210A] })
    const { user } = renderAt('/student/cart', { as: 'Student' })
    await user.click(await screen.findByRole('button', { name: 'Drop CSC 210 A' }))
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByText('CSC 210 A')).toBeInTheDocument()
  })

  it('drop is disabled outside add/drop', async () => {
    mockCart([], { window: 'Enrollment', enrolled: [enrolledCsc210A] })
    renderAt('/student/cart', { as: 'Student' })
    expect(await screen.findByRole('button', { name: 'Drop CSC 210 A' })).toBeDisabled()
    expect(screen.getByText('You can drop a course only during the add/drop period.')).toBeInTheDocument()
  })

  it('shows the API reason when a drop is refused', async () => {
    mockCart([], { window: 'AddDrop', enrolled: [enrolledCsc210A] })
    server.use(
      http.post('/api/enrollment/drop', () =>
        HttpResponse.json({ title: 'Courses can be dropped only during the add/drop period.' }, { status: 409 })),
    )
    const { user } = renderAt('/student/cart', { as: 'Student' })
    await user.click(await screen.findByRole('button', { name: 'Drop CSC 210 A' }))
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Drop course' }))
    expect(await within(screen.getByRole('dialog')).findByText('Courses can be dropped only during the add/drop period.')).toBeInTheDocument()
  })
})
