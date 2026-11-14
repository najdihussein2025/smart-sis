import { ADMIN_ID, apiAs, createStudent, ok, randomCourseCode, randomUniversityId, slot, uid } from './support/api'
import { expect, login, openNav, selectContaining, test, typeDate } from './support/fixtures'

test.describe('Admin journeys (§3 Admin)', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, ADMIN_ID)
  })

  test('create a term; its window times come back exactly as typed', async ({ page }) => {
    const name = `E2E Term ${uid()}`
    await openNav(page, 'Admin', 'Terms')
    await page.getByRole('button', { name: 'New term' }).click()
    const dialog = page.getByRole('dialog', { name: 'New term' })
    await dialog.getByLabel('Name').fill(name)
    await typeDate(dialog.getByRole('textbox', { name: 'First day' }), '10/01/2031')
    await typeDate(dialog.getByRole('textbox', { name: 'Last day' }), '30/05/2031')
    const window = [
      ['Enrollment opens', '01/12/2030', '08:00'],
      ['Enrollment closes', '10/12/2030', '17:30'],
      ['Add/drop opens', '10/12/2030', '17:30'],
      ['Add/drop closes', '20/01/2031', '12:00'],
    ] as const
    for (const [label, day, time] of window) {
      await typeDate(dialog.getByRole('textbox', { name: `${label} date` }), day)
      await dialog.getByLabel(`${label} time`).selectOption(time)
    }
    await dialog.getByRole('button', { name: 'Create term' }).click()
    await expect(page.getByText(`${name} saved.`)).toBeVisible()

    const row = page.getByRole('row', { name: new RegExp(name) })
    await expect(row).toContainText('10 Jan 2031')
    await expect(row).toContainText('01 Dec, 08:00')
    await expect(row).toContainText('20 Jan, 12:00')

    // Reopen: the same local times, not shifted by the time zone.
    await page.getByRole('button', { name: `Edit ${name}` }).click()
    const edit = page.getByRole('dialog', { name: `Edit ${name}` })
    for (const [label, day, time] of window) {
      await expect(edit.getByRole('textbox', { name: `${label} date` })).toHaveValue(day)
      await expect(edit.getByLabel(`${label} time`)).toHaveValue(time)
    }
  })

  test('create a course, then a section for it with a weekly meeting', async ({ page, run }, testInfo) => {
    const code = randomCourseCode()
    const name = `E2E Operating Systems ${uid()}`
    await openNav(page, 'Admin', 'Courses')
    await page.getByRole('button', { name: 'New course' }).click()
    const dialog = page.getByRole('dialog', { name: 'New course' })
    await dialog.getByLabel('Code').fill(code.toLowerCase())
    await dialog.getByLabel('Name').fill(name)
    await dialog.getByLabel('Credits').fill('3')
    await selectContaining(dialog.getByLabel('Department'), 'Computer Science')
    await dialog.getByLabel('Computer Science', { exact: true }).selectOption('Major')
    await dialog.getByRole('button', { name: 'Create course' }).click()
    await expect(dialog).toBeHidden()
    await page.getByRole('searchbox', { name: 'Search' }).fill(code)
    await expect(page.getByRole('rowheader', { name: code, exact: true })).toBeVisible() // stored upper case

    const s = slot(testInfo, 5)
    await openNav(page, 'Admin', 'Sections')
    await page.getByRole('button', { name: 'New section' }).click()
    const sec = page.getByRole('dialog', { name: 'New section' })
    await selectContaining(sec.getByLabel('Course'), code)
    await selectContaining(sec.getByLabel('Term'), run.termName)
    await sec.getByLabel('Section code').fill('b')
    await sec.getByLabel('Capacity').fill('25')
    const meeting = sec.getByRole('group', { name: 'Meeting 1' })
    await meeting.getByLabel('Day').selectOption(String(s.day))
    await meeting.getByLabel('Starts').selectOption(s.start)
    await meeting.getByLabel('Ends').selectOption(s.end)
    await meeting.getByLabel('Room').selectOption({ index: 1 })
    await sec.getByRole('button', { name: 'Create section' }).click()
    await expect(sec).toBeHidden()
    await expect(page.getByRole('button', { name: `Edit ${code} B` })).toBeVisible()
  })

  test('users list, search and create a student', async ({ page }) => {
    await openNav(page, 'Admin', 'Users')
    await page.getByLabel('Search').fill('Demo Instructor')
    await expect(page.getByText('Demo Instructor').first()).toBeVisible()

    const id = randomUniversityId()
    const name = `E2E New Student ${uid()}`
    await page.getByRole('button', { name: 'New user' }).click()
    const dialog = page.getByRole('dialog')
    await dialog.getByText('Student', { exact: true }).click()
    await dialog.getByLabel('University ID').fill(String(id))
    await dialog.getByLabel('Full name').fill(name)
    await dialog.getByLabel('Email').fill(`e2e.${id}@smartsis.local`)
    await dialog.getByLabel('Initial password').fill('Start1234')
    await selectContaining(dialog.getByLabel('Major'), 'Computer Science')
    await selectContaining(dialog.getByLabel('Campus'), 'Main Campus')
    await typeDate(dialog.getByRole('textbox', { name: 'Enrollment date' }), '01/09/2026')
    await dialog.getByRole('button', { name: 'Create student' }).click()
    await expect(dialog).toBeHidden()

    await page.getByLabel('Search').fill(String(id))
    await expect(page.getByText(name)).toBeVisible()
  })

  test('charge a major, then record a partial payment', async ({ page, admin, run }) => {
    const student = await createStudent(admin, run)
    const title = `E2E tuition ${uid()}`
    await openNav(page, 'Admin', 'Payments')
    await page.getByRole('button', { name: 'Charge a major' }).click()
    const dialog = page.getByRole('dialog', { name: 'Charge a major' })
    await selectContaining(dialog.getByLabel('Major'), 'Computer Science')
    await selectContaining(dialog.getByLabel('Term'), run.termName)
    await selectContaining(dialog.getByLabel('Payment type'), 'Tuition')
    await dialog.getByLabel('Title').fill(title)
    await dialog.getByLabel('Amount per student').fill('1200')
    await typeDate(dialog.getByRole('textbox', { name: 'Due date' }), '15/02/2031')
    await dialog.getByRole('button', { name: 'Create charges' }).click()
    await expect(dialog).toBeHidden()

    await page.getByRole('tab', { name: 'Charges' }).click()
    await page.getByRole('textbox', { name: 'Student' }).fill(String(student.universityId))
    await page.getByRole('button', { name: `Record payment for ${student.fullName}, ${title}` }).click()
    const pay = page.getByRole('dialog', { name: 'Record payment' })
    await pay.getByLabel('Payment amount').fill('500')
    await pay.getByRole('button', { name: 'Record payment' }).click()
    await expect(pay).toBeHidden()
    const row = page.getByRole('row', { name: new RegExp(title) })
    await expect(row).toContainText('$700.00')
    await expect(row).toContainText(/Partial/)

    // Rule F3: can't pay more than the balance.
    await page.getByRole('button', { name: `Record payment for ${student.fullName}, ${title}` }).click()
    await pay.getByLabel('Payment amount').fill('800')
    await pay.getByRole('button', { name: 'Record payment' }).click()
    await expect(pay.getByText(/balance/i).last()).toBeVisible()
    await expect(pay).toBeVisible()
  })

  test('support inbox: open a student request and reply', async ({ page, admin, run }) => {
    const student = await createStudent(admin, run)
    const subject = `E2E grade question ${uid()}`
    const mine = await apiAs(student.universityId)
    const categories = await ok<{ id: number; name: string }[]>(mine.get('/api/support/categories'), 'categories')
    await ok(
      mine.post('/api/support/requests', {
        data: { categoryId: categories.find((c) => c.name === 'Grade dispute')!.id, subject, description: 'Simulated question.' },
      }),
      'create request',
    )
    await mine.dispose()

    await openNav(page, 'Admin', 'Support inbox')
    await page.getByLabel('Search').fill(subject)
    await page.getByRole('link', { name: subject }).click()
    await expect(page.getByRole('heading', { name: subject })).toBeVisible()
    await page.getByLabel('Reply to the student').fill('E2E: we are checking with the instructor.')
    await page.getByRole('button', { name: 'Send reply' }).click()
    await expect(page.getByText('E2E: we are checking with the instructor.')).toBeVisible()
    await expect(page.getByText('In progress').first()).toBeVisible()
  })

  test('post an announcement to all students', async ({ page }) => {
    const title = `E2E exam period ${uid()}`
    await openNav(page, 'Admin', 'Announcements')
    await page.getByRole('button', { name: 'New announcement' }).click()
    const dialog = page.getByRole('dialog', { name: 'New announcement' })
    await dialog.getByLabel('Audience').selectOption('Students')
    await dialog.getByLabel('Title').fill(title)
    await dialog.getByLabel('Message').fill('Simulated announcement body.')
    await dialog.getByRole('button', { name: 'Publish' }).click()
    await expect(dialog).toBeHidden()
    await expect(page.getByRole('heading', { name: title })).toBeVisible()
  })
})
