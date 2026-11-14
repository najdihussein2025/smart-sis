import { ADMIN_ID, INSTRUCTOR_ID } from './support/api'
import { expect, login, test } from './support/fixtures'

test.describe('Login and roles (§3 shared)', () => {
  test('wrong password shows one message that clears as soon as the user types', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('University ID').fill('3001')
    await page.getByLabel('Password').fill('wrong-password-1')
    await page.getByRole('button', { name: 'Sign in' }).click()

    const error = page.getByText('Invalid University ID or password.')
    await expect(error).toBeVisible()
    await expect(page).toHaveURL(/\/login$/)

    await page.getByLabel('Password').press('x')
    await expect(error).toBeHidden()
  })

  test('empty form shows field errors', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByText('Enter your University ID.')).toBeVisible()
    await expect(page.getByText('Enter your password.')).toBeVisible()
  })

  for (const [id, home, area] of [
    [3001, '/student', 'Student'],
    [INSTRUCTOR_ID, '/instructor', 'Instructor'],
    [ADMIN_ID, '/admin', 'Admin'],
  ] as const) {
    test(`${area} lands on their dashboard and can't open another role's area`, async ({ page }) => {
      await login(page, id)
      await expect(page).toHaveURL(new RegExp(`${home}$`))
      await expect(page.getByRole('navigation', { name: `${area} navigation` })).toBeVisible()

      const other = home === '/admin' ? '/student/grades' : '/admin/users'
      await page.goto(other)
      await expect(page).toHaveURL(new RegExp(`${home}$`))
    })
  }

  test('sign out goes back to login and protects the pages', async ({ page }) => {
    await login(page, 3001)
    await page.getByRole('button', { name: 'Sign out' }).click()
    await expect(page).toHaveURL(/\/login$/)
    await page.goto('/student/schedule')
    await expect(page).toHaveURL(/\/login$/)
  })

  test('forgot password always confirms, whatever the ID', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('link', { name: 'Forgot password?' }).click()
    await expect(page).toHaveURL(/\/forgot-password$/)
    await page.getByLabel('University ID').fill('999999')
    await page.getByRole('button', { name: /send|reset/i }).click()
    await expect(page.getByText(/if .*account|check your email|sent/i).first()).toBeVisible()
  })
})
