import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { FileStack, RefreshCw, Trash2, Upload } from 'lucide-react'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { formatDateTime } from '@/shared/lib/format'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { TextField } from '@/shared/ui/TextField'
import { useDeleteDocument, useKnowledgeDocuments, useReingestDocument, useUploadDocument } from '../hooks'
import type { KnowledgeDocument, KnowledgeStatus } from '../types'

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

const statusTone: Record<KnowledgeStatus, 'success' | 'warning' | 'danger'> = { Ready: 'success', Processing: 'warning', Failed: 'danger' }

/** PB-15 / T63: university policies and FAQs the assistant searches. Admins upload text files and watch their status. */
export function KnowledgePage() {
  const docs = useKnowledgeDocuments()
  const upload = useUploadDocument()
  const reingest = useReingestDocument()
  const remove = useDeleteDocument()
  const saved = useSavedMessage()
  const [deleting, setDeleting] = useState<KnowledgeDocument | null>(null)

  const { register, handleSubmit, reset, setError, formState } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { title: '' },
  })

  const onSubmit = handleSubmit((values) =>
    upload.mutate(
      { title: values.title.trim(), file: values.file[0]! },
      {
        onSuccess: (doc) => {
          reset({ title: '' })
          saved.show(
            doc.status === 'Ready'
              ? `${doc.title} uploaded. The assistant can use it now.`
              : `${doc.title} was saved but could not be processed. Try “Re-ingest” later.`,
          )
        },
        onError: (error) => applyFieldErrors(error, fields, setError),
      },
    ),
  )

  const uploadProblem = getProblem(upload.error)
  const showUploadError = upload.isError && !(uploadProblem?.errors && fields.some((f) => uploadProblem.errors?.[f]))

  return (
    <>
      <PageHeader eyebrow="Administration" title="Knowledge base">
        Policies and FAQs the assistant searches to answer students. Upload plain text or Markdown.
      </PageHeader>

      <div className="mb-4 space-y-3 empty:hidden">
        {saved.message && <Alert tone="success">{saved.message}</Alert>}
        {reingest.isError && <Alert>{errorMessage(reingest.error)}</Alert>}
      </div>

      <Card className="mb-6 p-5">
        <form onSubmit={onSubmit} noValidate className="space-y-4" aria-label="Upload a document">
          {showUploadError && <Alert>{errorMessage(upload.error)}</Alert>}
          <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto] md:items-start">
            <TextField label="Title" placeholder="e.g. Registration rules" {...register('title')} error={formState.errors.title?.message} />
            <div className="space-y-1.5">
              <label htmlFor="knowledge-file" className="block text-sm font-medium text-ink">
                File (.txt or .md, up to 1 MB)
              </label>
              <input
                id="knowledge-file"
                type="file"
                accept=".txt,.md,text/plain,text/markdown"
                aria-invalid={!!formState.errors.file}
                aria-describedby={formState.errors.file ? 'knowledge-file-error' : undefined}
                {...register('file')}
                className="block w-full rounded-lg border border-line-strong bg-surface text-sm text-ink file:mr-3 file:h-11 file:border-0 file:bg-brand-50 file:px-3 file:text-ink"
              />
              {formState.errors.file && (
                <p id="knowledge-file-error" className="text-[13px] text-danger">
                  {formState.errors.file.message}
                </p>
              )}
            </div>
            <Button type="submit" loading={upload.isPending} className="md:mt-7">
              <Upload className="size-4" aria-hidden /> Upload
            </Button>
          </div>
        </form>
      </Card>

      <Card>
        {docs.isPending ? (
          <TableSkeleton label="Loading documents" />
        ) : docs.isError ? (
          <QueryError error={docs.error} onRetry={() => docs.refetch()} />
        ) : docs.data.length === 0 ? (
          <EmptyState icon={FileStack} title="No documents yet">
            Upload the university's policies or FAQs above. Until then the assistant can't answer policy questions.
          </EmptyState>
        ) : (
          <TableScroll>
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="border-b border-line text-xs uppercase tracking-wide text-muted">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Document</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3 font-medium">Passages</th>
                  <th scope="col" className="px-4 py-3 font-medium">Uploaded</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {docs.data.map((d) => (
                  <tr key={d.id}>
                    <th scope="row" className="px-4 py-3.5 font-medium text-ink">{d.title}</th>
                    <td className="px-4 py-3.5">
                      <Badge tone={statusTone[d.status]}>{d.status}</Badge>
                    </td>
                    <td className="px-4 py-3.5 text-muted">{d.chunks}</td>
                    <td className="px-4 py-3.5 text-muted">
                      {formatDateTime(d.createdAt)}
                      {d.uploadedByName && <span className="block text-xs">{d.uploadedByName}</span>}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-right">
                      {d.canReingest && (
                        <Button
                          variant="ghost"
                          className="h-8 px-2.5"
                          aria-label={`Re-ingest ${d.title}`}
                          loading={reingest.isPending && reingest.variables === d.id}
                          onClick={() =>
                            reingest.mutate(d.id, {
                              onSuccess: (doc) => saved.show(doc.status === 'Ready' ? `${doc.title} is ready.` : `${doc.title} still failed. Try again later.`),
                            })
                          }
                        >
                          <RefreshCw className="size-4" aria-hidden /> Re-ingest
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        className="h-8 px-2.5 hover:text-danger"
                        aria-label={`Delete ${d.title}`}
                        onClick={() => {
                          remove.reset()
                          setDeleting(d)
                        }}
                      >
                        <Trash2 className="size-4" aria-hidden /> Delete
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableScroll>
        )}
      </Card>

      <ConfirmDialog
        open={deleting !== null}
        title="Delete document?"
        confirmLabel="Delete"
        loading={remove.isPending}
        error={remove.isError ? errorMessage(remove.error) : null}
        onClose={() => setDeleting(null)}
        onConfirm={() =>
          deleting &&
          remove.mutate(deleting.id, {
            onSuccess: () => {
              saved.show(`${deleting.title} deleted.`)
              setDeleting(null)
            },
          })
        }
      >
        The assistant will stop using “{deleting?.title}” straight away.
      </ConfirmDialog>
    </>
  )
}
