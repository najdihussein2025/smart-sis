import { apiAs, createCourse, createSection, createStudent, enroll, ok, slot } from './support/api'
import { expect, login, openNav, test } from './support/fixtures'

const days = ['', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

test.describe('Student journeys (§3 Student)', () => {
  test('catalog → course → cart → enroll with a rule failure → schedule', async ({ page, admin, run }, testInfo) => {
    const s = slot(testInfo, 0)
    const course = await createCourse(admin, run, 'E2E Algorithms')
    const clash = await createCourse(admin, run, 'E2E Clashing')
    await createSection(admin, run, course, s, { roomIndex: 0 })
    await createSection(admin, run, clash, s, { roomIndex: 1 })
    const student = await createStudent(admin, run)

    await login(page, student.universityId)
    await openNav(page, 'Student', 'Course catalog')
    await expect(page.getByRole('heading', { name: 'Course catalog' })).toBeVisible()

    // Catalog → course details → add to cart, for both courses.
    for (const c of [course, clash]) {
      await page.getByRole('searchbox', { name: 'Search' }).fill(c.code)
      await page.getByRole('link', { name: `${c.code} ${c.name}` }).click()
      await expect(page.getByRole('heading', { name: c.name })).toBeVisible()
      await expect(page.getByText('None.')).toBeVisible() // no prerequisites
      await page.getByRole('button', { name: `Add ${c.code} A to cart` }).click()
      await expect(page.getByRole('button', { name: `${c.code} A: In Cart` })).toBeVisible()
      await page.getByRole('link', { name: 'Course catalog' }).first().click()
    }

    // Cart: enroll in both. The second one clashes with the first (rule E5) and is refused with its reason.
    await openNav(page, 'Student', 'Cart')
    const cart = page.getByRole('list', { name: 'Sections in your cart' })
    await expect(cart.getByRole('checkbox', { name: /^Select / })).toHaveCount(2)
    await page.getByText('Select all').click()
    await page.getByRole('button', { name: 'Enroll in 2 sections' }).click()

    const results = page.getByRole('region', { name: 'Enrollment results' })
    await expect(results).toContainText('Enrolled in 1 section.')
    await expect(results).toContainText('1 could not be enrolled.')
    await expect(results).toContainText(/conflict/i)

    // Schedule shows the enrolled class on its day.
    await openNav(page, 'Student', 'Schedule')
    await page.getByRole('tab', { name: days[s.day] }).click()
    const classes = page.getByRole('list', { name: `Classes on ${days[s.day]}` })
    await expect(classes).toContainText(`${s.start}–${s.end}`)
    const enrolledCode = (await classes.textContent()) ?? ''
    expect(enrolledCode.includes(course.code) || enrolledCode.includes(clash.code)).toBe(true)
    expect(enrolledCode.includes(course.code) && enrolledCode.includes(clash.code)).toBe(false)
  })

  test('grades, attendance and finance tabs show the student’s own data', async ({ page, admin, run }, testInfo) => {
    const course = await createCourse(admin, run, 'E2E Statistics')
    const section = await createSection(admin, run, course, slot(testInfo, 1), { roomIndex: 2 })
    const student = await createStudent(admin, run)
    await enroll(student, section.id)
    const title = `E2E lab fee ${Date.now()}`
    await ok(
      admin.post('/api/admin/finance/charges', {
        data: { universityId: student.universityId, termId: run.termId, paymentTypeId: run.paymentTypeId, title, amount: 150, dueDate: '2030-01-15' },
      }),
      'one-off charge',
    )

    await login(page, student.universityId)

    await openNav(page, 'Student', 'Grades')
    await expect(page.getByRole('heading', { name: 'Grades' })).toBeVisible()
    await expect(page.getByText(course.code).first()).toBeVisible()
    await expect(page.getByText('Cumulative GPA')).toBeVisible()

    await openNav(page, 'Student', 'Attendance')
    const perCourse = page.getByRole('list', { name: 'Attendance per course' })
    await expect(perCourse).toContainText(course.code)
    await expect(perCourse).toContainText('Perfect')

    await openNav(page, 'Student', 'Financial account')
    await expect(page.getByText('Outstanding balance')).toBeVisible()
    const pending = page.getByRole('list', { name: 'Pending charges' })
    await expect(pending.getByRole('heading', { name: title })).toBeVisible()
    await expect(pending).toContainText('$150.00')
    await expect(pending).toContainText('15 Jan 2030')
    await page.getByRole('tab', { name: 'Completed' }).click()
    await expect(page.getByText('No completed charges yet')).toBeVisible()
  })

  test('support request: submit, admin replies, student sees the reply and a notification', async ({ page, admin, run }) => {
    const student = await createStudent(admin, run)
    const subject = `E2E cannot see my section ${Date.now()}`

    await login(page, student.universityId)
    await openNav(page, 'Student', 'Support')
    // The empty list repeats the button, so there can be two once it has loaded.
    await page.getByRole('button', { name: 'New request' }).first().click()
    const dialog = page.getByRole('dialog', { name: 'New support request' })
    await dialog.getByLabel('Category').selectOption({ label: 'Registration' })
    await dialog.getByLabel('Subject').fill(subject)
    await dialog.getByLabel('Description').fill('Simulated request written by the e2e tests.')
    await dialog.getByRole('button', { name: 'Submit request' }).click()
    await expect(page.getByRole('heading', { name: subject })).toBeVisible()
    await expect(page.getByText('Request submitted.', { exact: false })).toBeVisible()

    // The admin answers through the API (the admin inbox UI is covered in admin.spec).
    const mine = await apiAs(student.universityId)
    const list = await ok<{ items: { id: number; subject: string }[] }>(mine.get('/api/support/requests'), 'my requests')
    const id = list.items.find((r) => r.subject === subject)!.id
    await ok(admin.post(`/api/admin/support/${id}/messages`, { data: { body: 'E2E admin answer: fixed.' } }), 'admin reply')
    await mine.dispose()

    await page.reload()
    await expect(page.getByText('E2E admin answer: fixed.')).toBeVisible()
    await page.getByLabel('Your reply').fill('Thanks, it works now.')
    await page.getByRole('button', { name: 'Send reply' }).click()
    await expect(page.getByText('Thanks, it works now.')).toBeVisible()

    // Bell: the admin reply made a notification.
    const bell = page.getByRole('button', { name: /^Notifications, \d+ unread$/ })
    await expect(bell).toBeVisible()
    await bell.click()
    await expect(page.getByRole('region', { name: 'Notifications' })).toBeVisible()
    await expect(page.getByRole('list', { name: 'Latest notifications' })).toContainText(subject.slice(0, 20))
  })

  test('announcements feed shows a post for students', async ({ page, admin, run }) => {
    const student = await createStudent(admin, run)
    const title = `E2E library hours ${Date.now()}`
    await ok(
      admin.post('/api/admin/announcements', {
        data: { title, body: 'Simulated announcement.', scope: 'Students', majorId: null, sectionId: null, publishAt: null, expiresAt: null },
      }),
      'post announcement',
    )

    await login(page, student.universityId)
    await openNav(page, 'Student', 'Announcements')
    await expect(page.getByRole('list', { name: 'Announcements' })).toContainText(title)

    await page.getByRole('button', { name: /^Notifications, \d+ unread$/ }).click()
    await expect(page.getByRole('list', { name: 'Latest notifications' })).toContainText(title)
  })

  test('assistant page renders and explains that the AI service is not available', async ({ page, admin, run }) => {
    const student = await createStudent(admin, run)
    await login(page, student.universityId)
    await openNav(page, 'Student', 'Assistant')
    await expect(page.getByRole('heading', { name: 'Assistant', level: 1 })).toBeVisible()
    await page.getByRole('textbox').last().fill('What is my GPA?')
    await page.getByRole('button', { name: 'Send' }).click()
    await expect(page.getByRole('alert').filter({ hasText: /assistant/i })).toContainText(/isn.t (set up yet|available right now)/)
  })

  test('career assessment: answer every question and get the top careers', async ({ page, admin, run }) => {
    const bank = await ok<{ activeCount: number; minQuestions: number }>(admin.get('/api/admin/career/questions'), 'question bank')
    test.skip(bank.activeCount < bank.minQuestions, 'Question bank not seeded')
    // 20–30 questions, one click each: WebKit on a busy machine needs more than the default minute.
    test.slow()
    const student = await createStudent(admin, run)

    await login(page, student.universityId)
    await openNav(page, 'Student', 'Career')
    await page.getByRole('button', { name: /take the career assessment|start/i }).click()
    const progress = page.getByRole('progressbar', { name: 'Assessment progress' })
    await expect(progress).toBeVisible()
    const total = Number(await progress.getAttribute('aria-valuemax'))
    const groups = page.locator('fieldset[id^="question-"]')
    await expect(groups).toHaveCount(total)
    for (let i = 0; i < total; i++) await groups.nth(i).getByRole('radio').first().check()
    await expect(progress).toHaveAttribute('aria-valuenow', String(total))
    await page.getByRole('button', { name: 'Submit assessment' }).click()

    await expect(page.getByText('Assessment submitted. Here are your results.')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Your top careers' })).toBeVisible()
    await expect(page.getByRole('list', { name: 'Recommended careers' }).getByRole('listitem')).toHaveCount(3)
  })
})
