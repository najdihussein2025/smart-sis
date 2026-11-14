import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { request, type APIRequestContext, type TestInfo } from '@playwright/test'

/**
 * Talks to the API directly (not through the browser) to make the simulated data a test needs (SR-23).
 * Every name, code and ID is random per test, so the four browser projects can run side by side on one database.
 */
export const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5190'
export const PASSWORD = 'Pass@1234'
export const ADMIN_ID = 1001
export const INSTRUCTOR_ID = 2001

export interface RunInfo {
  termId: number
  termName: string
  /** Seeded lookups. */
  majorId: number
  campusId: number
  departmentId: number
  rooms: number[]
  instructorUserId: number
  paymentTypeId: number
}

const runFile = fileURLToPath(new URL('../.run.json', import.meta.url))
export const runInfo = (): RunInfo => JSON.parse(readFileSync(runFile, 'utf8')) as RunInfo
export const writeRunFile = runFile

/** A signed-in API client. */
export async function apiAs(universityId: number, password = PASSWORD): Promise<APIRequestContext> {
  const anon = await request.newContext({ baseURL: API_URL })
  const res = await anon.post('/api/auth/login', { data: { universityId, password } })
  if (!res.ok()) throw new Error(`Login ${universityId} failed: ${res.status()} ${await res.text()}`)
  const { accessToken } = (await res.json()) as { accessToken: string }
  await anon.dispose()
  return request.newContext({ baseURL: API_URL, extraHTTPHeaders: { Authorization: `Bearer ${accessToken}` } })
}

export async function ok<T>(res: Promise<import('@playwright/test').APIResponse>, what: string): Promise<T> {
  const r = await res
  if (!r.ok()) throw new Error(`${what} failed: ${r.status()} ${await r.text()}`)
  const text = await r.text()
  return (text ? JSON.parse(text) : undefined) as T
}

// ---- random simulated values ----
const letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
const pick = (n: number, from: string) => Array.from({ length: n }, () => from[Math.floor(Math.random() * from.length)]).join('')
/** "QZXK 482": 4 letters + 3 digits, valid per §4 and practically unique. */
export const randomCourseCode = () => `${pick(4, letters)} ${pick(3, '0123456789')}`
/** 9-digit University ID starting with 7 (seeded ones are 4 digits). */
export const randomUniversityId = () => Number(`7${pick(8, '0123456789')}`)
export const uid = () => pick(6, letters.toLowerCase())

// ---- weekly slots ----
/**
 * A section meeting slot no other test in this run uses. The seeded instructor teaches many test sections, and a room
 * or instructor clash is refused (§4 Sections), so each browser project gets its own hour and each test its own day.
 *  slot 0..5 → Mon..Sat; project index → 08:00, 10:00, 12:00, 14:00, 16:00.
 */
export function slot(testInfo: TestInfo, n: number): { day: number; start: string; end: string } {
  const projects = ['chromium', 'firefox', 'webkit', 'msedge']
  const p = Math.max(0, projects.indexOf(testInfo.project.name))
  const hour = 8 + p * 2
  const hh = String(hour).padStart(2, '0')
  return { day: (n % 6) + 1, start: `${hh}:00`, end: `${hh}:45` }
}

// ---- builders ----
export interface Student {
  id: number
  universityId: number
  fullName: string
}

export async function createStudent(admin: APIRequestContext, run: RunInfo, name = 'E2E Student'): Promise<Student> {
  const universityId = randomUniversityId()
  const fullName = `${name} ${uid()}`
  const user = await ok<{ id: number }>(
    admin.post('/api/admin/users', {
      data: {
        universityId,
        email: `e2e.${universityId}@smartsis.local`,
        fullName,
        phone: null,
        dateOfBirth: null,
        role: 'Student',
        initialPassword: PASSWORD,
        student: { majorId: run.majorId, campusId: run.campusId, enrollmentDate: '2025-09-01', maxCreditsOverride: null },
        instructor: null,
      },
    }),
    'create student',
  )
  return { id: user.id, universityId, fullName }
}

export interface Course {
  id: number
  code: string
  name: string
}

export async function createCourse(admin: APIRequestContext, run: RunInfo, name = 'E2E Course', credits = 3): Promise<Course> {
  const code = randomCourseCode()
  const fullName = `${name} ${uid()}`
  const c = await ok<{ id: number }>(
    admin.post('/api/admin/courses', {
      data: {
        code,
        name: fullName,
        credits,
        description: 'Simulated course made by the e2e tests.',
        departmentId: run.departmentId,
        prerequisiteIds: [],
        majors: [{ majorId: run.majorId, type: 'Major' }],
      },
    }),
    'create course',
  )
  return { id: c.id, code, name: fullName }
}

export async function createSection(
  admin: APIRequestContext,
  run: RunInfo,
  course: Course,
  s: { day: number; start: string; end: string },
  opts: { code?: string; roomIndex?: number; instructor?: boolean; capacity?: number } = {},
): Promise<{ id: number; code: string }> {
  const code = opts.code ?? 'A'
  const sec = await ok<{ id: number }>(
    admin.post('/api/admin/sections', {
      data: {
        courseId: course.id,
        termId: run.termId,
        sectionCode: code,
        instructorId: opts.instructor ? run.instructorUserId : null,
        capacity: opts.capacity ?? 30,
        meetings: [
          { id: null, dayOfWeek: s.day, startTime: `${s.start}:00`, endTime: `${s.end}:00`, roomId: run.rooms[opts.roomIndex ?? 0], type: 'Lecture' },
        ],
      },
    }),
    'create section',
  )
  return { id: sec.id, code }
}

/** Student adds the section to the cart and enrolls through the API. */
export async function enroll(student: Student, sectionId: number): Promise<void> {
  const api = await apiAs(student.universityId)
  await ok(api.post('/api/cart', { data: { sectionId } }), 'add to cart')
  const result = await ok<{ enrolledCount: number; results: { message: string | null }[] }>(
    api.post('/api/enrollment/enroll', { data: { sectionIds: [sectionId] } }),
    'enroll',
  )
  if (result.enrolledCount !== 1) throw new Error(`enroll failed: ${result.results[0]?.message}`)
  await api.dispose()
}

/** The most recent past date (yyyy-mm-dd, never today) that falls on the given weekday (1 = Monday). */
export function lastWeekday(day: number, now = new Date()): string {
  const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()))
  do d.setUTCDate(d.getUTCDate() - 1)
  while (((d.getUTCDay() + 6) % 7) + 1 !== day)
  return d.toISOString().slice(0, 10)
}

/** yyyy-mm-dd → dd/mm/yyyy, the way DateField is typed. */
export const dmy = (iso: string) => iso.split('-').reverse().join('/')
