import { writeFileSync } from 'node:fs'
import { ADMIN_ID, apiAs, ok, writeRunFile, type RunInfo } from './support/api'

interface Lookups {
  campuses: { id: number }[]
  departments: { id: number; code: string }[]
  majors: { id: number; name: string }[]
  rooms: { id: number }[]
  instructors: { id: number; universityId: number }[]
}

/**
 * Runs once before all browsers. Makes a fresh current term whose enrollment window is open now, so every run starts
 * with an empty timetable (no room or instructor clashes with older runs) and enrollment can be tried right away.
 */
export default async function globalSetup() {
  const admin = await apiAs(ADMIN_ID)
  const lookups = await ok<Lookups>(admin.get('/api/admin/lookups'), 'lookups')
  const finance = await ok<{ paymentTypes: { id: number }[] }>(admin.get('/api/admin/finance/options'), 'finance options')

  const now = Date.now()
  const day = 86_400_000
  const isoDay = (ms: number) => new Date(ms).toISOString().slice(0, 10)
  const utc = (ms: number) => new Date(ms).toISOString()
  const name = `E2E ${new Date(now).toISOString().slice(0, 16).replace('T', ' ')}`
  const term = await ok<{ id: number; name: string }>(
    admin.post('/api/admin/terms', {
      data: {
        name,
        startDate: isoDay(now - 40 * day),
        endDate: isoDay(now + 90 * day),
        enrollStart: utc(now - day),
        enrollEnd: utc(now + 7 * day),
        addDropStart: utc(now + 7 * day),
        addDropEnd: utc(now + 21 * day),
        isCurrent: true,
      },
    }),
    'create e2e term',
  )

  const cs = lookups.majors.find((m) => m.name === 'Computer Science') ?? lookups.majors[0]!
  const dept = lookups.departments.find((d) => d.code === 'CSC') ?? lookups.departments[0]!
  const instructor = lookups.instructors.find((i) => i.universityId === 2001)!
  const run: RunInfo = {
    termId: term.id,
    termName: term.name,
    majorId: cs.id,
    campusId: lookups.campuses[0]!.id,
    departmentId: dept.id,
    rooms: lookups.rooms.map((r) => r.id),
    instructorUserId: instructor.id,
    paymentTypeId: finance.paymentTypes[0]!.id,
  }
  writeFileSync(writeRunFile, JSON.stringify(run, null, 2))
  await admin.dispose()
}
