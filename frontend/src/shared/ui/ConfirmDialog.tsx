import type { ReactNode } from 'react'
import { Alert } from './Alert'
import { Button } from './Button'
import { Dialog } from './Dialog'

interface Props {
  open: boolean
  title: string
  children: ReactNode
  /** Text of the confirm button, e.g. "Deactivate". */
  confirmLabel: string
  onConfirm: () => void
  onClose: () => void
  loading?: boolean
  /** Error from the last attempt, shown above the buttons. */
  error?: string | null
}

/** Asks before a destructive action (drop, deactivate). Cancel is the safe default and closes with Esc. */
export function ConfirmDialog({ open, title, children, confirmLabel, onConfirm, onClose, loading, error }: Props) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={title}
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button variant="danger" type="button" onClick={onConfirm} loading={loading}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <div className="space-y-4 text-sm text-ink">
        <div>{children}</div>
        {error && <Alert>{error}</Alert>}
      </div>
    </Dialog>
  )
}
