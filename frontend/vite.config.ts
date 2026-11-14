/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import { configDefaults } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, 'src') },
  },
  server: {
    port: 5173,
    proxy: {
      // Calls to /api go to the ASP.NET Core API during development (also used by `vite preview`).
      // VITE_API_PROXY points it at another API, e.g. the one the e2e tests start on :5190.
      '/api': process.env.VITE_API_PROXY ?? 'http://localhost:5080',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    // Playwright specs in e2e/ run with `npm run e2e`, never with Vitest.
    exclude: [...configDefaults.exclude, 'e2e/**'],
    restoreMocks: true,
    // The suite renders the real routes; on slower or busy machines a single test can take several seconds.
    testTimeout: 30_000,
    // Fixed time zone so date and time conversions give the same result on every machine.
    env: { TZ: 'UTC' },
  },
})
