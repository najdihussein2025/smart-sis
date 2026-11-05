import { errorMessage } from '@/shared/api/problem'
import { Alert } from './Alert'
import { Button } from './Button'

/** Error state for a list or detail screen: the message plus a retry button. */
export function QueryError({ error, onRetry }: { error: unknown; onRetry: () => void }) {
  return (
    <div className="space-y-3 p-6">
      <Alert>{errorMessage(error, 'Could not load this page.')}</Alert>
      <Button variant="secondary" onClick={onRetry}>
        Try again
      </Button>
    </div>
  )
}
