import { useState } from 'react'
import { useSwap } from '@/features/enrollment/hooks'
import type { EnrolledSection } from '@/features/enrollment/types'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import type { CartItem } from '../types'

interface Props {
  item: CartItem | null
  enrolled: EnrolledSection[]
  onClose: () => void
  onSwapped: (message: string) => void
}

/** Swap an enrolled section for this cart section in one step (enrollment or add/drop period). */
export function SwapDialog({ item, enrolled, onClose, onSwapped }: Props) {
  const swap = useSwap()
  // Another section of the same course is the obvious choice; otherwise the student picks.
  const sameCourse = enrolled.find((e) => e.courseId === item?.courseId)
  const [from, setFrom] = useState<number | null>(null)
  const chosen = from ?? sameCourse?.sectionId ?? null

  const close = () => {
    swap.reset()
    setFrom(null)
    onClose()
  }

  if (!item) return null
  const to = `${item.courseCode} ${item.sectionCode}`

  return (
    <Dialog
      open
      onClose={close}
      title={`Swap into ${to}`}
      description="Choose the enrolled section to give up. You keep it if the swap isn't allowed."
      actions={
        <>
          <Button variant="secondary" onClick={close}>
            Cancel
          </Button>
          <Button
            disabled={chosen === null}
            loading={swap.isPending}
            onClick={() =>
              chosen !== null &&
              swap.mutate(
                { from: chosen, to: item.sectionId },
                {
                  onSuccess: () => {
                    const old = enrolled.find((e) => e.sectionId === chosen)
                    onSwapped(`Swapped ${old ? `${old.courseCode} ${old.sectionCode}` : 'your section'} for ${to}.`)
                    close()
                  },
                },
              )
            }
          >
            Swap
          </Button>
        </>
      }
    >
      <fieldset className="space-y-2">
        <legend className="sr-only">Section to give up</legend>
        {enrolled.map((e) => (
          <label key={e.sectionId} className="flex cursor-pointer items-center gap-3 rounded-lg border border-line px-3 py-2.5 text-sm hover:bg-canvas">
            <input
              type="radio"
              name="swap-from"
              className="accent-brand"
              checked={chosen === e.sectionId}
              onChange={() => setFrom(e.sectionId)}
            />
            <span>
              <span className="font-medium text-ink">
                {e.courseCode} {e.sectionCode}
              </span>{' '}
              <span className="text-muted">{e.courseName}</span>
            </span>
          </label>
        ))}
      </fieldset>
      {swap.isError && (
        <div className="mt-4">
          <Alert>{errorMessage(swap.error)}</Alert>
        </div>
      )}
    </Dialog>
  )
}
