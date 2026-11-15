import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { dashboards, paged } from './fixtures'

/** Healthy admin setup for the dashboard checks. Simulated (SR-23). */
export const dashboardSources = {
  terms: [
    {
      id: 1, name: 'Fall 2026', startDate: '2026-09-01', endDate: '2099-01-31',
      enrollStart: '2026-08-01T06:00:00Z', enrollEnd: '2026-08-31T21:00:00Z',
      addDropStart: '2026-09-01T06:00:00Z', addDropEnd: '2026-09-14T21:00:00Z',
      isCurrent: true, currentWindow: 'Closed',
    },
  ],
  questionBank: { activeCount: 30, minQuestions: 20, maxQuestions: 30, questions: [] },
  knowledge: [
    {
      id: 1, title: 'Student handbook', status: 'Ready', chunks: 12, canReingest: true, uploadedByName: 'Demo Admin',
      createdAt: '2026-08-01T08:00:00Z', updatedAt: '2026-08-01T08:00:00Z',
    },
  ],
}

/**
 * Requests every signed-in screen makes (the bell in RoleShell) and the dashboard numbers (every sign-in lands there).
 * They stay after `server.resetHandlers()`, so tests of other pages don't need to mock them; a test overrides them with
 * `server.use(...)` when it cares.
 */
export const defaultHandlers = [
  http.get('/api/notifications/unread-count', () => HttpResponse.json({ count: 0 })),
  http.get('/api/notifications', () => HttpResponse.json(paged([]))),
  http.get('/api/dashboard/student', () => HttpResponse.json(dashboards.student)),
  http.get('/api/dashboard/instructor', () => HttpResponse.json(dashboards.instructor)),
  http.get('/api/dashboard/admin', () => HttpResponse.json(dashboards.admin)),
  // The other sources the dashboards read ("Next up", "Needs attention"): empty and healthy, so nothing is flagged.
  http.get('/api/schedule/my', () => HttpResponse.json({ termId: 1, termName: 'Fall 2026', items: [] })),
  http.get('/api/schedule/teaching', () => HttpResponse.json({ termId: 1, termName: 'Fall 2026', items: [] })),
  http.get('/api/cart', () =>
    HttpResponse.json({ termId: 1, termName: 'Fall 2026', window: 'Closed', items: [], cartCredits: 0, enrolledCredits: 9, maxCredits: 18 }),
  ),
  http.get('/api/instructor/grading/sections', () => HttpResponse.json([])),
  http.get('/api/admin/terms', () => HttpResponse.json(dashboardSources.terms)),
  http.get('/api/admin/career/questions', () => HttpResponse.json(dashboardSources.questionBank)),
  http.get('/api/admin/knowledge', () => HttpResponse.json(dashboardSources.knowledge)),
  http.get('/api/admin/support', () => HttpResponse.json(paged([]))),
]

/**
 * Mock API for tests. Each test adds the handlers it needs with `server.use(...)`;
 * any request without a handler fails the test (see setup.ts), so a typo in a URL can't pass silently.
 */
export const server = setupServer(...defaultHandlers)
