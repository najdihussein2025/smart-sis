import type { Role, Session } from '@/shared/auth/session'

/** Simulated users matching the backend DevSeeder (SR-23: never real data). */
export const users = {
  Admin: { id: 1, universityId: 1001, fullName: 'Demo Admin', role: 'Admin' },
  Instructor: { id: 2, universityId: 2001, fullName: 'Demo Instructor', role: 'Instructor' },
  Student: { id: 3, universityId: 3001, fullName: 'Demo Student', role: 'Student' },
} as const satisfies Record<Role, Session['user']>

export function sessionFor(role: Role): Session {
  return {
    accessToken: `test-token-${role.toLowerCase()}`,
    expiresAt: new Date(Date.now() + 60 * 60_000).toISOString(),
    user: users[role],
  }
}

export function profileFor(role: Role) {
  const u = users[role]
  return {
    id: u.id,
    universityId: u.universityId,
    fullName: u.fullName,
    email: `${role.toLowerCase()}@smartsis.local`,
    role,
    phone: null,
    dateOfBirth: null,
    photoUrl: null,
    student:
      role === 'Student'
        ? { majorId: 1, majorName: 'Computer Science', campusName: 'Main Campus', enrollmentDate: '2024-09-01', maxCredits: 18 }
        : null,
    instructor: role === 'Instructor' ? { departmentId: 1, departmentName: 'Computer Science' } : null,
  }
}

/** Admin dropdown lists (GET /api/admin/lookups), matching the DevSeeder ids. Simulated (SR-23). */
export const lookups = {
  campuses: [{ id: 1, name: 'Main Campus' }],
  departments: [
    { id: 1, code: 'CSC', name: 'Computer Science' },
    { id: 2, code: 'MAT', name: 'Mathematics' },
  ],
  majors: [
    { id: 1, name: 'Computer Science', departmentId: 1, defaultMaxCredits: 18 },
    { id: 2, name: 'Mathematics', departmentId: 2, defaultMaxCredits: 18 },
  ],
  rooms: [
    { id: 1, name: 'B-101', campusId: 1, campusName: 'Main Campus', capacity: 40 },
    { id: 2, name: 'B-204', campusId: 1, campusName: 'Main Campus', capacity: 30 },
    { id: 3, name: 'Lab-3', campusId: 1, campusName: 'Main Campus', capacity: 25 },
  ],
  instructors: [{ id: 2, fullName: 'Demo Instructor', universityId: 2001, departmentId: 1 }],
}

/** A page of results in the API's PagedResult shape. */
export function paged<T>(items: T[], page = 1, pageSize = 20, totalCount = items.length) {
  return { items, page, pageSize, totalCount, totalPages: Math.ceil(totalCount / pageSize) }
}
