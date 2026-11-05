import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="grid min-h-dvh place-items-center px-4 text-center">
      <div>
        <p className="font-display text-6xl font-semibold text-brand">404</p>
        <p className="mt-3 text-muted">This page doesn't exist.</p>
        <Link to="/" className="mt-6 inline-flex h-10 items-center rounded-lg bg-brand px-4 text-sm font-medium text-white hover:bg-brand-600">
          Go home
        </Link>
      </div>
    </div>
  )
}
