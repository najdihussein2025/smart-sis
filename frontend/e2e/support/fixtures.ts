import { test as base, expect, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import { ADMIN_ID, apiAs, PASSWORD, runInfo, type RunInfo } from './api'

/** Signs in through the real login form and waits for the role's dashboard. */
export async function login(page: Page, universityId: number, password = PASSWORD) {
  await page.goto('/login')
  await page.getByLabel('University ID').fill(String(universityId))
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).not.toHaveURL(/\/login$/)
}

/** Opens a page from the sidebar (desktop layout), like a user would. */
export async function openNav(page: Page, area: 'Student' | 'Instructor' | 'Admin', label: string) {
  await page.getByRole('navigation', { name: `${area} navigation` }).getByRole('link', { name: label, exact: true }).click()
}

export const test = base.extend<{ run: RunInfo; admin: APIRequestContext }>({
  // eslint-disable-next-line no-empty-pattern
  run: async ({}, use) => use(runInfo()),
  // eslint-disable-next-line no-empty-pattern
  admin: async ({}, use) => {
    const api = await apiAs(ADMIN_ID)
    await use(api)
    await api.dispose()
  },
})

export { expect }

/** Picks the first option of a native select whose text contains `text` (option labels carry extra details). */
export async function selectContaining(select: Locator, text: string) {
  const value = await select.locator('option', { hasText: text }).first().getAttribute('value')
  if (value === null) throw new Error(`No option containing "${text}"`)
  await select.selectOption(value)
}

/** Types a DD/MM/YYYY date into a DateField and commits it. */
export async function typeDate(field: Locator, ddmmyyyy: string) {
  await field.fill(ddmmyyyy)
  await field.press('Enter')
}
