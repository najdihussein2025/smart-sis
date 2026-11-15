import { useState } from 'react'
import { Pencil, Plus, Search, UserX, Users } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { useAuth } from '@/shared/auth/AuthProvider'
import type { Role } from '@/shared/auth/session'
import { useDebouncedValue } from '@/shared/lib/useDebouncedValue'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { Pagination } from '@/shared/ui/Pagination'
import { QueryError } from '@/shared/ui/QueryError'
import { SearchField } from '@/shared/ui/SearchField'
import { SelectField } from '@/shared/ui/SelectField'
import { SortHeader, useSort } from '@/shared/ui/SortHeader'
import { TableScroll } from '@/shared/ui/TableScroll'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { UserFormDialog } from '../components/UserFormDialog'
import { useDeactivateUser, useUsers } from '../hooks'
import type { UserListItem, UserStatus } from '../types'

const PAGE_SIZE = 20

type DialogState = { open: false } | { open: true; userId?: number }
type SortKey = 'name' | 'id'

/** PB-01 / T11: admin searches accounts, creates students and instructors, edits them and deactivates them. */
export function UsersPage() {
  const { user: me } = useAuth()
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<'' | Role>('')
  const [status, setStatus] = useState<UserStatus>('Active')
  const [page, setPage] = useState(1)
  const term = useDebouncedValue(search.trim())
  const users = useUsers({ search: term, role: role || undefined, status, page, pageSize: PAGE_SIZE })
  const { sorted, sort, toggle } = useSort<UserListItem, SortKey>(users.data?.items, (u, key) =>
    key === 'name' ? u.fullName : u.universityId,
  )
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const [confirm, setConfirm] = useState<UserListItem | null>(null)
  const deactivate = useDeactivateUser()
  const saved = useSavedMessage()
  const filtered = !!term || !!role || status !== 'Active'

  const openNew = () => setDialog({ open: true })
  const close = () => setDialog({ open: false })
  const closeConfirm = () => {
    setConfirm(null)
    deactivate.reset()
  }
  const clearFilters = () => {
    setSearch('')
    setRole('')
    setStatus('Active')
    setPage(1)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Users"
        icon={Users}
        actions={
          <Button onClick={openNew}>
            <Plus className="size-4" aria-hidden /> New user
          </Button>
        }
      >
        Student and instructor accounts. Deactivated accounts can’t sign in but stay on record.
      </PageHeader>

      {saved.message && <Alert tone="success">{saved.message}</Alert>}

      <Card>
        <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-[minmax(0,1fr)_11rem_11rem] sm:items-end sm:px-5">
          <SearchField
            label="Search"
            placeholder="Name, email or University ID"
            value={search}
            onChange={(v) => {
              setSearch(v)
              setPage(1)
            }}
          />
          <SelectField
            label="Role"
            value={role}
            onChange={(e) => {
              setRole(e.target.value as '' | Role)
              setPage(1)
            }}
          >
            <option value="">All roles</option>
            <option value="Student">Students</option>
            <option value="Instructor">Instructors</option>
            <option value="Admin">Admins</option>
          </SelectField>
          <SelectField
            label="Status"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as UserStatus)
              setPage(1)
            }}
          >
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
            <option value="All">All</option>
          </SelectField>
        </div>

        {users.isPending ? (
          <TableSkeleton label="Loading users" />
        ) : users.isError ? (
          <QueryError error={users.error} onRetry={() => users.refetch()} />
        ) : users.data.items.length === 0 ? (
          filtered ? (
            <EmptyState
              icon={Search}
              title="No users match"
              action={
                <Button variant="ghost" onClick={clearFilters}>
                  Clear filters
                </Button>
              }
            >
              Try another name, email or University ID, or change the role and status filters.
            </EmptyState>
          ) : (
            <EmptyState
              icon={Users}
              title="No users yet"
              action={
                <Button onClick={openNew}>
                  <Plus className="size-4" aria-hidden /> New user
                </Button>
              }
            >
              Create the first student or instructor account.
            </EmptyState>
          )
        ) : (
          <>
            <TableScroll>
              <table className="w-full text-left text-sm max-lg:block lg:min-w-[760px]">
                <thead className="border-b border-line bg-canvas/60 text-xs uppercase tracking-wide text-muted max-lg:hidden">
                  <tr>
                    <SortHeader label="Name" sortKey="name" sort={sort} onSort={toggle} className="px-4" />
                    <SortHeader label="University ID" sortKey="id" sort={sort} onSort={toggle} className="whitespace-nowrap px-4" />
                    <th scope="col" className="px-4 py-3 font-medium">Role</th>
                    <th scope="col" className="px-4 py-3 font-medium">Major / department</th>
                    <th scope="col" className="px-4 py-3 font-medium">Status</th>
                    <th scope="col" className="px-4 py-3"><span className="sr-only">Actions</span></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line max-lg:block">
                  {sorted.map((u) => (
                    <UserRow
                      key={u.id}
                      user={u}
                      isMe={u.id === me?.id}
                      onEdit={() => setDialog({ open: true, userId: u.id })}
                      onDeactivate={() => setConfirm(u)}
                    />
                  ))}
                </tbody>
              </table>
            </TableScroll>
            <Pagination page={users.data.page} totalPages={users.data.totalPages} totalCount={users.data.totalCount} noun="user" onPage={setPage} />
          </>
        )}
      </Card>

      <UserFormDialog
        open={dialog.open}
        userId={dialog.open ? dialog.userId : undefined}
        onClose={close}
        onSaved={(u) => {
          saved.show(`${u.fullName} saved.`)
          close()
        }}
      />

      <ConfirmDialog
        open={confirm !== null}
        title={confirm ? `Deactivate ${confirm.fullName}?` : 'Deactivate'}
        confirmLabel="Deactivate"
        loading={deactivate.isPending}
        error={deactivate.isError ? errorMessage(deactivate.error) : null}
        onClose={closeConfirm}
        onConfirm={() =>
          confirm &&
          deactivate.mutate(confirm.id, {
            onSuccess: () => {
              saved.show(`${confirm.fullName} deactivated.`)
              closeConfirm()
            },
          })
        }
      >
        <p>
          {confirm?.fullName} ({confirm?.universityId}) won’t be able to sign in any more. Their record stays and you’ll still
          find them under “Inactive”. This can’t be undone here yet.
        </p>
      </ConfirmDialog>
    </div>
  )
}

function StatusBadge({ active }: { active: boolean }) {
  return active ? <Badge tone="success" dot>Active</Badge> : <Badge>Inactive</Badge>
}

/** One account: a table row from `lg`, a stacked card below it. */
function UserRow({ user: u, isMe, onEdit, onDeactivate }: {
  user: UserListItem
  isMe: boolean
  onEdit: () => void
  onDeactivate: () => void
}) {
  // Admin accounts and deactivated accounts can't be changed here; nor can your own be deactivated.
  // The API has no reactivate endpoint, so an inactive account only gets a note.
  const managed = u.role !== 'Admin' && u.isActive
  return (
    <tr className="transition-colors hover:bg-canvas/50 max-lg:grid max-lg:grid-cols-1 max-lg:gap-y-1.5 max-lg:px-4 max-lg:py-4">
      <th scope="row" className="px-4 py-3.5 font-normal max-lg:p-0">
        <span className="block font-medium text-ink">{u.fullName}</span>
        <span className="block text-muted max-lg:[overflow-wrap:anywhere]">{u.email}</span>
      </th>
      <td className="tabular px-4 py-3.5 text-ink max-lg:p-0">
        <span className="mr-1.5 text-muted lg:hidden">ID</span>
        {u.universityId}
      </td>
      <td className="px-4 py-3.5 max-lg:p-0">
        <span className="inline-flex flex-wrap gap-1.5">
          <Badge tone="outline">{u.role}</Badge>
          <span className="lg:hidden"><StatusBadge active={u.isActive} /></span>
        </span>
      </td>
      <td className={`px-4 py-3.5 text-muted max-lg:p-0 ${u.majorName || u.departmentName ? '' : 'max-lg:hidden'}`}>
        {u.majorName ?? u.departmentName ?? '—'}
      </td>
      <td className="px-4 py-3.5 max-lg:hidden">
        <StatusBadge active={u.isActive} />
      </td>
      <td className="whitespace-nowrap px-4 py-3.5 text-right max-lg:p-0 max-lg:text-left">
        {managed ? (
          <div className="inline-flex gap-1 max-lg:-ml-3 max-lg:mt-1">
            <Button variant="ghost" size="sm" onClick={onEdit} aria-label={`Edit ${u.fullName}`}>
              <Pencil className="size-4" aria-hidden /> Edit
            </Button>
            {!isMe && (
              <Button variant="danger-ghost" size="sm" onClick={onDeactivate} aria-label={`Deactivate ${u.fullName}`}>
                <UserX className="size-4" aria-hidden /> Deactivate
              </Button>
            )}
          </div>
        ) : (
          <span className="text-xs text-muted">{u.role === 'Admin' ? 'Protected' : 'No changes while inactive'}</span>
        )}
      </td>
    </tr>
  )
}
