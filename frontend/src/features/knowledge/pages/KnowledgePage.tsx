import { useState } from 'react'
import { FileStack, RefreshCw, Trash2, Upload } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { formatDateTime } from '@/shared/lib/format'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { CardHeader } from '@/shared/ui/CardHeader'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { QueryError } from '@/shared/ui/QueryError'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { UploadDocumentDialog } from '../components/UploadDocumentDialog'
import { useDeleteDocument, useKnowledgeDocuments, useReingestDocument } from '../hooks'
import type { KnowledgeDocument, KnowledgeStatus } from '../types'


const statusTone: Record<KnowledgeStatus, 'success' | 'warning' | 'danger'> = { Ready: 'success', Processing: 'warning', Failed: 'danger' }

/** PB-15 / T63: university policies and FAQs the assistant searches. Admins upload text files and watch their status. */
export function KnowledgePage() {
  const docs = useKnowledgeDocuments()
  const reingest = useReingestDocument()
  const remove = useDeleteDocument()
  const saved = useSavedMessage()
  const [uploading, setUploading] = useState(false)
  const [deleting, setDeleting] = useState<KnowledgeDocument | null>(null)

  const uploadButton = (
    <Button onClick={() => setUploading(true)}>
      <Upload className="size-4" aria-hidden /> Upload document
    </Button>
  )

  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Setup" title="Knowledge base" icon={FileStack} actions={uploadButton}>
        Policies and FAQs the assistant searches to answer students. Upload plain text or Markdown.
      </PageHeader>

      {(saved.message || reingest.isError) && (
        <div className="space-y-3">
          {saved.message && <Alert tone="success">{saved.message}</Alert>}
          {reingest.isError && <Alert>{errorMessage(reingest.error)}</Alert>}
        </div>
      )}

      <Card>
        <CardHeader
          title="Documents"
          description={docs.data ? `${docs.data.length} ${docs.data.length === 1 ? 'document' : 'documents'}` : undefined}
        />
        {docs.isPending ? (
          <TableSkeleton label="Loading documents" />
        ) : docs.isError ? (
          <QueryError error={docs.error} onRetry={() => docs.refetch()} />
        ) : docs.data.length === 0 ? (
          <EmptyState icon={FileStack} title="No documents yet" action={uploadButton}>
            Upload the university's policies or FAQs. Until then the assistant can't answer policy questions.
          </EmptyState>
        ) : (
          <TableScroll>
            <table className="w-full text-left text-sm max-md:block">
              <thead className="border-b border-line bg-canvas/60 text-xs uppercase tracking-wide text-muted max-md:hidden">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Document</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">Passages</th>
                  <th scope="col" className="px-4 py-3 font-medium">Uploaded</th>
                  <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line max-md:block">
                {docs.data.map((d) => (
                  <tr
                    key={d.id}
                    className="transition-colors hover:bg-canvas/50 max-md:grid max-md:grid-cols-[minmax(0,1fr)_auto] max-md:items-center max-md:gap-x-3 max-md:gap-y-1.5 max-md:px-4 max-md:py-4"
                  >
                    <th scope="row" className="px-4 py-3.5 font-medium text-ink max-md:p-0">{d.title}</th>
                    <td className="px-4 py-3.5 max-md:col-start-2 max-md:row-start-1 max-md:p-0">
                      <Badge tone={statusTone[d.status]} dot>{d.status}</Badge>
                    </td>
                    <td className="tabular px-4 py-3.5 text-right text-muted max-md:col-span-2 max-md:p-0 max-md:text-left">
                      <span>{d.chunks}</span>
                      <span className="md:hidden"> {d.chunks === 1 ? 'passage' : 'passages'}</span>
                    </td>
                    <td className="px-4 py-3.5 text-muted max-md:col-span-2 max-md:p-0">
                      {formatDateTime(d.createdAt)}
                      {d.uploadedByName && <span className="block text-xs max-md:inline max-md:before:content-['_·_']">{d.uploadedByName}</span>}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3.5 text-right max-md:col-span-2 max-md:p-0 max-md:text-left">
                      <div className="inline-flex gap-1 max-md:-ml-3 max-md:mt-1">
                        {d.canReingest && (
                          <Button
                            variant="ghost"
                            size="sm"
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
                          variant="danger-ghost"
                          size="sm"
                          aria-label={`Delete ${d.title}`}
                          onClick={() => {
                            remove.reset()
                            setDeleting(d)
                          }}
                        >
                          <Trash2 className="size-4" aria-hidden /> Delete
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableScroll>
        )}
      </Card>

      <UploadDocumentDialog
        open={uploading}
        onClose={() => setUploading(false)}
        onUploaded={(doc) => {
          setUploading(false)
          saved.show(
            doc.status === 'Ready'
              ? `${doc.title} uploaded. The assistant can use it now.`
              : `${doc.title} was saved but could not be processed. Try “Re-ingest” later.`,
          )
        }}
      />

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
    </div>
  )
}
