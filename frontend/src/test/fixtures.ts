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
