import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { errorMessage } from '@/shared/api/problem'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { TextField } from '@/shared/ui/TextField'
import { useRenameConversation } from '../hooks'
import type { ConversationSummary } from '../types'

const schema = z.object({
  title: z.string().trim().min(1, 'Enter a title.').max(200, 'Keep the title under 200 characters.'),
})
type Values = z.infer<typeof schema>

interface Props {
  conversation: ConversationSummary | null
  onClose: () => void
}

export function RenameDialog({ conversation, onClose }: Props) {
  const rename = useRenameConversation()
  const { register, handleSubmit, reset, formState } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { title: '' } })

  useEffect(() => {
    if (conversation) {
      reset({ title: conversation.title })
      rename.reset()
    }
    // Only when another conversation is opened for renaming.
  }, [conversation])

  const onSubmit = handleSubmit((values) => {
    if (!conversation) return
    rename.mutate({ id: conversation.id, title: values.title.trim() }, { onSuccess: onClose })
  })

  return (
    <Dialog
      open={conversation !== null}
      onClose={onClose}
      title="Rename conversation"
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="rename-conversation" loading={rename.isPending}>
            Save
          </Button>
        </>
      }
    >
      <form id="rename-conversation" onSubmit={onSubmit} className="space-y-4" noValidate>
        {rename.isError && <Alert>{errorMessage(rename.error)}</Alert>}
        <TextField label="Title" {...register('title')} error={formState.errors.title?.message} />
      </form>
    </Dialog>
  )
}
