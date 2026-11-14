import { existsSync } from 'node:fs'
import { defineConfig, devices } from '@playwright/test'

/**
 * T79 / PB-23: cross-browser end-to-end tests. `npm run e2e` starts (or reuses) the API on :5190 against a LocalDB
 * database and the production build on :5199 (vite preview, `/api` proxied to the API), then runs every spec in
 * Chromium, Firefox, WebKit (Safari's engine) and Microsoft Edge.
 *
 * Every test makes its own simulated data through the admin API (fresh students, courses and sections; SR-23), and
 * global-setup makes a new current term per run, so runs never collide with each other or with older runs.
 */
const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5190'
const WEB_PORT = Number(process.env.E2E_WEB_PORT ?? 5199)
const DB = process.env.E2E_DB ?? 'Server=(localdb)\\MSSQLLocalDB;Database=SmartSisE2E;Trusted_Connection=True;TrustServerCertificate=True'

// Edge runs only where it is installed (Windows has it; Linux CI usually doesn't). E2E_EDGE=0 skips it, =1 forces it.
const edgePaths = [
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  '/opt/microsoft/msedge/msedge',
  '/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge',
]
const withEdge = process.env.E2E_EDGE ? process.env.E2E_EDGE === '1' : edgePaths.some((p) => existsSync(p))

export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.ts',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  // Four browsers share one API and one LocalDB; more workers just make the API the bottleneck.
  workers: process.env.E2E_WORKERS ? Number(process.env.E2E_WORKERS) : 4,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: `http://localhost:${WEB_PORT}`,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    // Same language and time zone in every browser, so dates read the same everywhere.
    locale: 'en-GB',
    timezoneId: 'Asia/Beirut',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
    ...(withEdge ? [{ name: 'msedge', use: { ...devices['Desktop Edge'], channel: 'msedge' } }] : []),
  ],
  webServer: [
    {
      command: 'dotnet run --no-launch-profile --project ../backend/src/SmartSis.Api',
      url: `${API_URL}/health`,
      reuseExistingServer: true,
      timeout: 180_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: API_URL,
        ConnectionStrings__Sis: DB,
        // No AI service in e2e: the assistant must show its friendly "not available" message.
        AiService__BaseUrl: 'http://localhost:5998',
      },
    },
    {
      command: `npx vite build && npx vite preview --port ${WEB_PORT} --strictPort`,
      url: `http://localhost:${WEB_PORT}`,
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
      env: { VITE_API_PROXY: API_URL },
    },
  ],
})
