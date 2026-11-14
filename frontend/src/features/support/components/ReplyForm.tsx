import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Send } from 'lucide-react'
import { applyFieldErrors, errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { TextAreaField } from '@/shared/ui/TextAreaField'

export const replySchema = z.object({
  body: z.string().trim().min(1, 'Write a reply.').max(4000, 'Keep the reply under 4000 characters.'),
})
type ReplyValues = z.infer<typeof replySchema>

interface Props {
  /** Sends the reply; rejects with the API error. */
  send: (body: string) => Promise<unknown>
  pending: boolean
  label?: string
  hint?: string
}

/** Reply box under a thread. Field errors under the field, other server errors (409) at the top; both clear on typing. */
export function ReplyForm({ send, pending, label = 'Your reply', hint }: Props) {
  const [topError, setTopError] = useState<string | null>(null)
  const { register, handleSubmit, formState, reset, setError, watch } = useForm<ReplyValues>({
    resolver: zodResolver(replySchema),
    defaultValues: { body: '' },
  })

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const onSubmit = handleSubmit(async ({ body }) => {
    try {
      await send(body.trim())
      reset({ body: '' })
    } catch (error) {
      if (!applyFieldErrors(error, ['body'] as const, setError)) setTopError(errorMessage(error))
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-3" aria-label="Reply">
      {topError && <Alert>{topError}</Alert>}
      <TextAreaField label={label} rows={4} hint={hint} error={formState.errors.body?.message} {...register('body')} />
      <div className="flex justify-end">
        <Button type="submit" loading={pending}>
          <Send className="size-4" aria-hidden /> Send reply
        </Button>
      </div>
    </form>
  )
}
