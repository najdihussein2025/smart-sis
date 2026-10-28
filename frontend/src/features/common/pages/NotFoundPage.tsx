import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="grid min-h-dvh place-items-center px-4 text-center">
      <div>
        <p className="font-display text-6xl font-semibold text-brand">404</p>
        <p className="mt-3 text-muted">This page doesn't exist.</p>
        <Link to="/" className="mt-6 inline-block text-sm font-medium text-brand hover:text-accent">
          Go home
        </Link>
      </div>
    </div>
  )
}
