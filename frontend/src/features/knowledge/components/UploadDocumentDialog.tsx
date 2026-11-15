import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { FileText, Upload } from 'lucide-react'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { cn } from '@/shared/lib/cn'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Dialog } from '@/shared/ui/Dialog'
import { TextField } from '@/shared/ui/TextField'
import { useUploadDocument } from '../hooks'
import type { KnowledgeDocument } from '../types'

export const MAX_FILE_BYTES = 1_000_000

const schema = z.object({
  title: z.string().trim().min(1, 'Enter a title.').max(200, 'Keep the title under 200 characters.'),
  file: z
    .custom<FileList>()
    .refine((files) => files instanceof FileList && files.length === 1, 'Choose a file.')
    .refine((files) => !files?.[0] || /\.(txt|md)$/i.test(files[0].name), 'Upload a .txt or .md file.')
    .refine((files) => !files?.[0] || files[0].size <= MAX_FILE_BYTES, 'The file must be 1 MB or smaller.'),
})
type Values = z.infer<typeof schema>
const fields = ['title', 'file'] as const

const FORM_ID = 'knowledge-upload-form'

/** Title + one .txt / .md file. The native file input is visually hidden behind a styled "Choose file" button. */
export function UploadDocumentDialog({ open, onClose, onUploaded }: {
  open: boolean
  onClose: () => void
  onUploaded: (doc: KnowledgeDocument) => void
}) {
  const upload = useUploadDocument()
  const { register, handleSubmit, reset, setError, watch, formState } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { title: '' },
  })
  const files = watch('file')
  const chosen = files instanceof FileList ? files[0] : undefined

  // Closing (or a successful upload) clears the form, so the next upload starts empty.
  const close = () => {
    reset({ title: '' })
    upload.reset()
    onClose()
  }

  const onSubmit = handleSubmit((values) =>
    upload.mutate(
      { title: values.title.trim(), file: values.file[0]! },
      {
        onSuccess: (doc) => {
          reset({ title: '' })
          upload.reset()
          onUploaded(doc)
        },
        onError: (error) => applyFieldErrors(error, fields, setError),
      },
    ),
  )

  const problem = getProblem(upload.error)
  const showError = upload.isError && !(problem?.errors && fields.some((f) => problem.errors?.[f]))
  const fileError = formState.errors.file?.message

  return (
    <Dialog
      open={open}
      onClose={close}
      title="Upload document"
      description="Plain text or Markdown. The assistant can use it as soon as it’s processed."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={close}>
            Cancel
          </Button>
          <Button type="submit" form={FORM_ID} loading={upload.isPending}>
            <Upload className="size-4" aria-hidden /> Upload
          </Button>
        </>
      }
    >
      <form id={FORM_ID} onSubmit={onSubmit} noValidate className="space-y-4" aria-label="Upload a document">
        {showError && <Alert>{errorMessage(upload.error)}</Alert>}
        <TextField label="Title" placeholder="e.g. Registration rules" {...register('title')} error={formState.errors.title?.message} />
        <div className="space-y-1.5">
          <p id="knowledge-file-label" className="text-sm font-medium text-ink">
            File (.txt or .md, up to 1 MB)
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <input
              id="knowledge-file"
              type="file"
              accept=".txt,.md,text/plain,text/markdown"
              aria-labelledby="knowledge-file-label"
              aria-invalid={!!fileError}
              aria-describedby={fileError ? 'knowledge-file-error' : 'knowledge-file-name'}
              {...register('file')}
              className="peer sr-only"
            />
            <label
              htmlFor="knowledge-file"
              className={cn(
                'inline-flex h-11 cursor-pointer items-center gap-2 rounded-lg border bg-surface px-4 text-sm font-medium text-ink shadow-sm hover:bg-canvas sm:h-10',
                'peer-focus-visible:ring-2 peer-focus-visible:ring-focus peer-focus-visible:ring-offset-2',
                fileError ? 'border-danger' : 'border-line-strong/60',
              )}
            >
              <FileText className="size-4" aria-hidden /> {chosen ? 'Change file' : 'Choose file'}
            </label>
            <span id="knowledge-file-name" className={cn('min-w-0 text-sm break-all', chosen ? 'text-ink' : 'text-muted')}>
              {chosen ? chosen.name : 'No file chosen'}
            </span>
          </div>
          {fileError && (
            <p id="knowledge-file-error" className="text-[13px] text-danger">
              {fileError}
            </p>
          )}
        </div>
      </form>
    </Dialog>
  )
}
