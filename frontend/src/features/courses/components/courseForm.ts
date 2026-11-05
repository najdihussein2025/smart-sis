import { z } from 'zod'
import type { CourseDetail, CourseType, SaveCourseRequest } from '../types'

/** Same pattern as the API's CourseCode rule: 2–5 letters, optional space, 3 digits, optional letter. */
const codePattern = /^[A-Za-z]{2,5}\s?\d{3}[A-Za-z]?$/

/** Mirrors the API's SaveCourseRequestValidator so mistakes show before the request (UX only; the API checks again). */
export const courseSchema = z.object({
  code: z.string().trim().regex(codePattern, 'Use a code like "CSC 101": 2–5 letters, 3 digits, optional letter.'),
  name: z.string().trim().min(1, 'Enter a course name.').max(150, 'Use at most 150 characters.'),
  credits: z
    .string()
    .trim()
    .refine((c) => /^\d{1,2}$/.test(c) && Number(c) <= 12, 'Credits must be between 0 and 12.'),
  description: z.string().max(4000, 'Use at most 4000 characters.'),
  departmentId: z.string().min(1, 'Choose a department.'),
  prerequisites: z.array(z.object({ id: z.number(), code: z.string(), name: z.string() })),
  majors: z.array(z.object({ majorId: z.number(), type: z.enum(['Major', 'Elective']) })),
})

export type CourseFormValues = z.infer<typeof courseSchema>

/**
 * API field names that can carry an error ("prerequisiteIds[0]" and "majors[0].majorId" land on their list).
 * `prerequisiteIds` is shown on the form's `prerequisites` picker.
 */
export const courseApiFields = ['code', 'name', 'credits', 'description', 'departmentId', 'prerequisiteIds', 'majors'] as const
export type CourseApiField = (typeof courseApiFields)[number]
export const toFormField = (f: CourseApiField): keyof CourseFormValues => (f === 'prerequisiteIds' ? 'prerequisites' : f)

export const emptyCourse: CourseFormValues = {
  code: '', name: '', credits: '3', description: '', departmentId: '', prerequisites: [], majors: [],
}

export function toFormValues(c: CourseDetail): CourseFormValues {
  return {
    code: c.code,
    name: c.name,
    credits: String(c.credits),
    description: c.description ?? '',
    departmentId: String(c.departmentId),
    prerequisites: c.prerequisites,
    majors: c.majors.map((m) => ({ majorId: m.majorId, type: m.type })),
  }
}

export function toRequest(v: CourseFormValues): SaveCourseRequest {
  return {
    code: v.code.trim(),
    name: v.name.trim(),
    credits: Number(v.credits),
    description: v.description.trim() || null,
    departmentId: Number(v.departmentId),
    prerequisiteIds: v.prerequisites.map((p) => p.id),
    majors: v.majors,
  }
}

/** Sets how the course counts for one major ('' removes it from that major). */
export function withMajorType(majors: CourseFormValues['majors'], majorId: number, type: CourseType | '') {
  const others = majors.filter((m) => m.majorId !== majorId)
  return type ? [...others, { majorId, type }] : others
}
