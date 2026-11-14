import type { Meeting } from '@/shared/lib/schedule'

// TODO: replace with generated types from `npm run gen:api` (CartDto).

export interface CartItem {
  sectionId: number
  courseId: number
  courseCode: string
  courseName: string
  credits: number
  sectionCode: string
  instructorName: string | null
  capacity: number
  seatsLeft: number
  meetings: Meeting[]
  addedAt: string
}

export interface Cart {
  termId: number | null
  termName: string | null
  window: 'Closed' | 'Enrollment' | 'AddDrop'
  items: CartItem[]
  cartCredits: number
  enrolledCredits: number
  maxCredits: number
}
