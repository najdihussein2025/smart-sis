import { z } from 'zod'
import { todayIso } from '@/shared/lib/format'
import type { CreateUserRequest, UpdateUserRequest, UserDetail } from '../types'

/** Mirrors CreateUserRequestValidator / UpdateUserRequestValidator (UX only; the API checks again, including duplicates). */
export function userSchema(creating: boolean) {
  return z
    .object({
      role: z.enum(['Student', 'Instructor']),
      universityId: z.string().trim(),
      fullName: z.string().trim().min(1, 'Enter the full name.').max(150, 'Use at most 150 characters.'),
      email: z
        .string()
        .trim()
        .min(1, 'Enter an email address.')
        .max(256, 'Use at most 256 characters.')
        .regex(/^[^@\s]+@[^@\s]+\.[^@\s]+$/, 'Enter a valid email address.'),
      phone: z.string().trim().max(32, 'Use at most 32 characters.'),
      dateOfBirth: z.string().refine((d) => !d || d < todayIso(), 'Date of birth must be in the past.'),
      initialPassword: z.string(),
      student: z.object({ majorId: z.string(), campusId: z.string(), enrollmentDate: z.string(), maxCreditsOverride: z.string().trim() }),
      instructor: z.object({ departmentId: z.string() }),
    })
    .superRefine((v, ctx) => {
      const issue = (path: (string | number)[], message: string) => ctx.addIssue({ code: 'custom', path, message })
      if (creating) {
        if (!/^\d{4,9}$/.test(v.universityId) || Number(v.universityId) < 1000)
          issue(['universityId'], 'University ID must have 4 to 9 digits.')
        const p = v.initialPassword
        if (!p) issue(['initialPassword'], 'Enter an initial password.')
        else if (p.length < 8) issue(['initialPassword'], 'Use at least 8 characters.')
        else if (!/[A-Za-z]/.test(p)) issue(['initialPassword'], 'Include at least one letter.')
        else if (!/[0-9]/.test(p)) issue(['initialPassword'], 'Include at least one number.')
        else if (p.length > 128) issue(['initialPassword'], 'Use at most 128 characters.')
      }
      if (v.role === 'Student') {
        if (!v.student.majorId) issue(['student', 'majorId'], 'Choose a major.')
        if (!v.student.campusId) issue(['student', 'campusId'], 'Choose a campus.')
        if (!v.student.enrollmentDate) issue(['student', 'enrollmentDate'], 'Enter the enrollment date.')
        const cap = v.student.maxCreditsOverride
        if (cap && (!/^\d+$/.test(cap) || Number(cap) < 1 || Number(cap) > 30))
          issue(['student', 'maxCreditsOverride'], 'Credit cap override must be between 1 and 30.')
      } else if (!v.instructor.departmentId) {
        issue(['instructor', 'departmentId'], 'Choose a department.')
      }
    })
}

export type UserFormValues = z.infer<ReturnType<typeof userSchema>>

/** API error keys that land on a form field ("student.majorId" etc. match the form's nested fields). */
export const userApiFields = [
  'universityId', 'fullName', 'email', 'phone', 'dateOfBirth', 'initialPassword', 'role',
  'student.majorId', 'student.campusId', 'student.enrollmentDate', 'student.maxCreditsOverride', 'instructor.departmentId',
] as const

export const emptyUser = (role: 'Student' | 'Instructor' = 'Student'): UserFormValues => ({
  role,
  universityId: '',
  fullName: '',
  email: '',
  phone: '',
  dateOfBirth: '',
  initialPassword: '',
  student: { majorId: '', campusId: '', enrollmentDate: todayIso(), maxCreditsOverride: '' },
  instructor: { departmentId: '' },
})

export function toFormValues(u: UserDetail): UserFormValues {
  return {
    role: u.role === 'Instructor' ? 'Instructor' : 'Student',
    universityId: String(u.universityId),
    fullName: u.fullName,
    email: u.email,
    phone: u.phone ?? '',
    dateOfBirth: u.dateOfBirth ?? '',
    initialPassword: '',
    student: u.student
      ? {
          majorId: String(u.student.majorId),
          campusId: String(u.student.campusId),
          enrollmentDate: u.student.enrollmentDate,
          maxCreditsOverride: u.student.maxCreditsOverride === null ? '' : String(u.student.maxCreditsOverride),
        }
      : emptyUser().student,
    instructor: { departmentId: u.instructor ? String(u.instructor.departmentId) : '' },
  }
}

export function toUpdateRequest(v: UserFormValues): UpdateUserRequest {
  return {
    email: v.email.trim(),
    fullName: v.fullName.trim(),
    phone: v.phone.trim() || null,
    dateOfBirth: v.dateOfBirth || null,
    student:
      v.role === 'Student'
        ? {
            majorId: Number(v.student.majorId),
            campusId: Number(v.student.campusId),
            enrollmentDate: v.student.enrollmentDate,
            maxCreditsOverride: v.student.maxCreditsOverride ? Number(v.student.maxCreditsOverride) : null,
          }
        : null,
    instructor: v.role === 'Instructor' ? { departmentId: Number(v.instructor.departmentId) } : null,
  }
}

export function toCreateRequest(v: UserFormValues): CreateUserRequest {
  return { ...toUpdateRequest(v), universityId: Number(v.universityId), role: v.role, initialPassword: v.initialPassword }
}
