import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Pencil, Plus, Tags, Trash2 } from 'lucide-react'
import { applyFieldErrors, errorMessage, getProblem } from '@/shared/api/problem'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { Dialog } from '@/shared/ui/Dialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { QueryError } from '@/shared/ui/QueryError'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { TextField } from '@/shared/ui/TextField'
import {
  useAdminSupportCategories, useCreateSupportCategory, useDeleteSupportCategory, useRenameSupportCategory,
} from '../hooks'
import type { AdminSupportCategory } from '../types'

export const categorySchema = z.object({
  name: z.string().trim().min(1, 'Enter a name.').max(100, 'Keep the name under 100 characters.'),
})
type CategoryValues = z.infer<typeof categorySchema>

/** Puts a 409 duplicate name or a 400 field error under the name field; anything else goes to `onOther`. */
function handleNameError(error: unknown, setError: (f: 'name', e: { type: string; message: string }) => void, onOther: (m: string) => void) {
  const problem = getProblem(error)
  if (problem?.code === 'duplicate_category') return setError('name', { type: 'server', message: problem.title ?? 'Name taken.' })
  if (!applyFieldErrors(error, ['name'] as const, setError)) onOther(errorMessage(error))
}

/** PB-14 / T54: support categories — list, add, rename, delete when no request uses them. */
export function CategoriesPanel() {
  const categories = useAdminSupportCategories()
  const create = useCreateSupportCategory()
  const remove = useDeleteSupportCategory()
  const saved = useSavedMessage()
  const [topError, setTopError] = useState<string | null>(null)
  const [renaming, setRenaming] = useState<AdminSupportCategory | null>(null)
  const [deleting, setDeleting] = useState<AdminSupportCategory | null>(null)
  const { register, handleSubmit, formState, reset, setError, watch } = useForm<CategoryValues>({
    resolver: zodResolver(categorySchema),
    defaultValues: { name: '' },
  })

  useEffect(() => {
    const sub = watch(() => setTopError(null))
    return () => sub.unsubscribe()
  }, [watch])

  const onAdd = handleSubmit(({ name }) =>
    create.mutate(name.trim(), {
      onSuccess: (c) => {
        reset({ name: '' })
        saved.show(`“${c.name}” added.`)
      },
      onError: (error) => handleNameError(error, setError, setTopError),
    }),
  )

  const closeDelete = () => {
    setDeleting(null)
    remove.reset()
  }

  return (
    <div className="space-y-4">
      {saved.message && <Alert tone="success">{saved.message}</Alert>}
      <Card>
        <form onSubmit={onAdd} noValidate className="flex flex-wrap items-start gap-3 border-b border-line p-4" aria-label="Add a category">
          <div className="min-w-0 flex-1 basis-60">
            <TextField label="New category" maxLength={100} error={formState.errors.name?.message} {...register('name')} />
          </div>
          <Button type="submit" className="sm:mt-7" loading={create.isPending}>
            <Plus className="size-4" aria-hidden /> Add
          </Button>
          {topError && (
            <div className="basis-full">
              <Alert>{topError}</Alert>
            </div>
          )}
        </form>

        {categories.isPending ? (
          <TableSkeleton label="Loading categories" />
        ) : categories.isError ? (
          <QueryError error={categories.error} onRetry={() => categories.refetch()} />
        ) : categories.data.length === 0 ? (
          <EmptyState icon={Tags} title="No categories yet">
            Add the first one above. Students choose a category when they submit a request.
          </EmptyState>
        ) : (
          <ul className="divide-y divide-line" aria-label="Support categories">
            {categories.data.map((c) => (
              <li key={c.id} className="flex flex-wrap items-center justify-between gap-3 px-5 py-3">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-ink">{c.name}</span>
                  <Badge>
                    {c.requestCount} {c.requestCount === 1 ? 'request' : 'requests'}
                  </Badge>
                </div>
                <div className="flex gap-1">
                  <Button variant="ghost" className="h-8 px-2.5" onClick={() => setRenaming(c)} aria-label={`Rename ${c.name}`}>
                    <Pencil className="size-4" aria-hidden /> Rename
                  </Button>
                  <Button variant="ghost" className="h-8 px-2.5" onClick={() => setDeleting(c)} aria-label={`Delete ${c.name}`}>
                    <Trash2 className="size-4" aria-hidden /> Delete
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>

      <RenameDialog
        category={renaming}
        onClose={() => setRenaming(null)}
        onSaved={(c) => {
          setRenaming(null)
          saved.show(`Renamed to “${c.name}”.`)
        }}
      />

      <ConfirmDialog
        open={deleting !== null}
        title="Delete category?"
        confirmLabel="Delete"
        loading={remove.isPending}
        error={remove.isError ? errorMessage(remove.error) : null}
        onClose={closeDelete}
        onConfirm={() =>
          deleting &&
          remove.mutate(deleting.id, {
            onSuccess: () => {
              saved.show(`“${deleting.name}” deleted.`)
              closeDelete()
            },
          })
        }
      >
        {deleting && (
          <p>
            “{deleting.name}” will no longer be offered to students. A category that requests already use can’t be deleted;
            rename it instead.
          </p>
        )}
      </ConfirmDialog>
    </div>
  )
}

function RenameDialog({
  category, onClose, onSaved,
}: { category: AdminSupportCategory | null; onClose: () => void; onSaved: (c: AdminSupportCategory) => void }) {
  const rename = useRenameSupportCategory()
  const [topError, setTopError] = useState<string | null>(null)
  const { register, handleSubmit, formState, reset, setError } = useForm<CategoryValues>({
    resolver: zodResolver(categorySchema),
    defaultValues: { name: '' },
  })

  useEffect(() => {
    if (!category) return
    reset({ name: category.name })
    setTopError(null)
    rename.reset()
    // Only when another category is opened.
  }, [category])

  const onSubmit = handleSubmit(({ name }) =>
    category &&
    rename.mutate(
      { id: category.id, name: name.trim() },
      { onSuccess: onSaved, onError: (error) => handleNameError(error, setError, setTopError) },
    ),
  )

  return (
    <Dialog
      open={category !== null}
      onClose={onClose}
      title="Rename category"
      description="Requests filed under it keep it, with the new name."
      actions={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="rename-category-form" loading={rename.isPending}>
            Save
          </Button>
        </>
      }
    >
      <form id="rename-category-form" onSubmit={onSubmit} noValidate className="space-y-4">
        {topError && <Alert>{topError}</Alert>}
        <TextField label="Name" maxLength={100} error={formState.errors.name?.message} {...register('name')} />
      </form>
    </Dialog>
  )
}
