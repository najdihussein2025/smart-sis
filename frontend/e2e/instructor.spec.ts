import { createCourse, createSection, createStudent, dmy, enroll, INSTRUCTOR_ID, lastWeekday, ok, slot } from './support/api'
import { expect, login, openNav, selectContaining, test } from './support/fixtures'

test.describe('Instructor journeys (§3 Instructor)', () => {
  test('attendance: 3 steps, submit, and a second submit for the same session is blocked', async ({ page, admin, run }, testInfo) => {
    const s = slot(testInfo, 2)
    const course = await createCourse(admin, run, 'E2E Networks')
    const section = await createSection(admin, run, course, s, { roomIndex: 0, instructor: true })
    const student = await createStudent(admin, run)
    await enroll(student, section.id)
    const date = dmy(lastWeekday(s.day))

    const choose = async () => {
      await page.getByLabel('Course and section').selectOption({ label: `${course.code} A · ${course.name}` })
      const session = page.getByLabel('Session', { exact: true })
      await session.selectOption({ index: 1 })
      const dateField = page.getByRole('textbox', { name: 'Date' })
      await dateField.fill(date)
      await dateField.press('Enter')
    }

    await login(page, INSTRUCTOR_ID)
    await openNav(page, 'Instructor', 'Attendance')
    await choose()

    const students = page.getByRole('list', { name: `Students of ${course.code} A` })
    await expect(students).toContainText(student.fullName)
    await page.getByRole('button', { name: 'Mark all absent' }).click()
    await expect(page.getByText('0 present, 1 absent')).toBeVisible()
    await page.getByRole('button', { name: 'Submit attendance' }).click()
    await expect(page.getByText(`Attendance saved for ${course.code} A on ${date}: 0 present, 1 absent.`)).toBeVisible()

    // Same session and date again: shown as already taken, nothing to submit (rule A4 / BG_06).
    await page.reload()
    await choose()
    await expect(page.getByText('Attendance for this session was already submitted.', { exact: false })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Submit attendance' })).toHaveCount(0)
  })

  test('grading: pick section and component, enter a score, it stays after reload', async ({ page, admin, run }, testInfo) => {
    const course = await createCourse(admin, run, 'E2E Compilers')
    const section = await createSection(admin, run, course, slot(testInfo, 3), { roomIndex: 1, instructor: true })
    const student = await createStudent(admin, run)
    await enroll(student, section.id)
    await ok(
      admin.post('/api/admin/grade-components', { data: { courseId: course.id, termId: run.termId, name: 'Midterm', maxGrade: 50, weight: 40 } }),
      'grade component',
    )

    await login(page, INSTRUCTOR_ID)
    await openNav(page, 'Instructor', 'Grading')
    await selectContaining(page.getByLabel('Section'), course.code)
    await page.getByRole('tab', { name: 'Midterm · 40%' }).click()
    await expect(page.getByText(/components add up to 40%/)).toBeVisible()

    const score = page.getByLabel(`Score for ${student.fullName}`)
    await score.fill('42.5')
    await page.getByRole('button', { name: 'Save scores' }).click()
    await expect(page.getByText('Scores saved for Midterm.')).toBeVisible()

    await page.reload()
    await selectContaining(page.getByLabel('Section'), course.code)
    await page.getByRole('tab', { name: 'Midterm · 40%' }).click()
    await expect(page.getByLabel(`Score for ${student.fullName}`)).toHaveValue('42.5')
  })

  test('instructor posts an announcement to their own section', async ({ page, admin, run }, testInfo) => {
    const course = await createCourse(admin, run, 'E2E Robotics')
    await createSection(admin, run, course, slot(testInfo, 4), { roomIndex: 2, instructor: true })
    const title = `E2E quiz moved ${Date.now()}`

    await login(page, INSTRUCTOR_ID)
    await openNav(page, 'Instructor', 'Announcements')
    await page.getByRole('button', { name: 'New announcement' }).click()
    const dialog = page.getByRole('dialog', { name: 'New announcement' })
    await selectContaining(dialog.getByLabel('Section'), course.code)
    await dialog.getByLabel('Title').fill(title)
    await dialog.getByLabel('Message').fill('Simulated announcement for one section.')
    await dialog.getByRole('button', { name: 'Publish' }).click()
    await expect(page.getByRole('heading', { name: title })).toBeVisible()
  })
})
