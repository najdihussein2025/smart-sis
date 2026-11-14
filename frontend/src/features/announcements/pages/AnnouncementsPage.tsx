import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router'
import { Megaphone, Plus } from 'lucide-react'
import { errorMessage } from '@/shared/api/problem'
import { useAuth } from '@/shared/auth/AuthProvider'
import { useSavedMessage } from '@/shared/lib/useSavedMessage'
import { Alert } from '@/shared/ui/Alert'
import { Button } from '@/shared/ui/Button'
import { Card } from '@/shared/ui/Card'
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { EmptyState } from '@/shared/ui/EmptyState'
import { PageHeader } from '@/shared/ui/PageHeader'
import { Pagination } from '@/shared/ui/Pagination'
import { QueryError } from '@/shared/ui/QueryError'
import { SelectField } from '@/shared/ui/SelectField'
import { TableSkeleton } from '@/shared/ui/TableSkeleton'
import { Tabs } from '@/shared/ui/Tabs'
import { AnnouncementFormDialog } from '../components/AnnouncementFormDialog'
import { AnnouncementItem } from '../components/AnnouncementItem'
import { useAnnouncementFeed, useDeleteAnnouncement, useManagedAnnouncements } from '../hooks'
import type { Announcement, AnnouncementStatus } from '../types'

const PAGE_SIZE = 10

type View = 'feed' | 'manage'
type DialogState = { open: false } | { open: true; announcement?: Announcement }

const intro = {
  Student: 'News from the university, your major and the sections you are enrolled in.',
  Instructor: 'News for instructors, and what you post to the sections you teach.',
  Admin: 'Post news to everyone, students, instructors or one major, and manage every announcement.',
} as const

/** PB-12: the announcements feed for every role; admins and instructors also post and manage (rule C1). */
export function AnnouncementsPage() {
  const { user } = useAuth()
  const role = user?.role ?? 'Student'
  const poster = role === 'Student' ? null : role
  const [params] = useSearchParams()
  const highlightId = Number(params.get('announcement')) || null

  const [view, setView] = useState<View>('feed')
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<AnnouncementStatus | ''>('')
  const [dialog, setDialog] = useState<DialogState>({ open: false })
  const [toDelete, setToDelete] = useState<Announcement | null>(null)
  const saved = useSavedMessage()
  const remove = useDeleteAnnouncement()

  const feed = useAnnouncementFeed({ page, pageSize: PAGE_SIZE })
  const managed = useManagedAnnouncements(role, { page, pageSize: PAGE_SIZE, status: status || undefined }, view === 'manage')
  const list = view === 'feed' ? feed : managed

  // Opened from a notification: bring that announcement into view once it is on screen.
  const highlighted = useRef<HTMLElement | null>(null)
  const shownHighlight = list.data?.items.some((a) => a.id === highlightId)
  useEffect(() => {
    if (shownHighlight) {
      highlighted.current?.scrollIntoView?.({ block: 'center' })
      highlighted.current?.focus({ preventScroll: true })
    }
  }, [shownHighlight, highlightId])

  const switchView = (v: View) => {
    setView(v)
    setPage(1)
  }

  const confirmDelete = () => {
    if (!toDelete || !poster) return
    remove.mutate(
      { role: poster, id: toDelete.id },
      {
        onSuccess: () => {
          saved.show(`"${toDelete.title}" was deleted.`)
          setToDelete(null)
        },
      },
    )
  }

  const manageLabel = role === 'Admin' ? 'All posts' : 'My posts'
  const canPost = poster !== null

  return (
    <>
      <PageHeader
        eyebrow={role === 'Admin' ? 'Administration' : role === 'Instructor' ? 'Teaching' : 'Campus news'}
        title="Announcements"
        actions={
          canPost && (
            <Button onClick={() => setDialog({ open: true })}>
              <Plus className="size-4" aria-hidden /> New announcement
            </Button>
          )
        }
      >
        {intro[role]}
      </PageHeader>

      {saved.message && (
        <div className="mb-4">
          <Alert tone="success">{saved.message}</Alert>
        </div>
      )}

      {canPost && (
        <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
          <Tabs
            label="Announcement lists"
            value={view}
            onChange={switchView}
            items={[
              { value: 'feed', label: 'Feed' },
              { value: 'manage', label: manageLabel },
            ]}
          />
          {view === 'manage' && (
            <div className="w-48">
              <SelectField
                label="Status"
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value as AnnouncementStatus | '')
                  setPage(1)
                }}
              >
                <option value="">Any status</option>
                <option value="Live">Live</option>
                <option value="Scheduled">Scheduled</option>
                <option value="Expired">Expired</option>
              </SelectField>
            </div>
          )}
        </div>
      )}

      <Card>
        {list.isPending ? (
          <TableSkeleton label="Loading announcements" rows={4} />
        ) : list.isError ? (
          <QueryError error={list.error} onRetry={() => list.refetch()} />
        ) : list.data.items.length === 0 ? (
          <EmptyState icon={Megaphone} title={view === 'feed' ? 'No announcements yet' : 'Nothing posted here'}>
            {view === 'feed'
              ? canPost
                ? 'Nothing is published for you right now. Post one with "New announcement".'
                : 'Nothing is published for you right now. New announcements also show up in the bell.'
              : status
                ? 'No announcement has this status. Choose another status.'
                : 'Post the first one with "New announcement".'}
          </EmptyState>
        ) : (
          <>
            <ul className="divide-y divide-line" aria-label={view === 'feed' ? 'Announcements' : manageLabel}>
              {list.data.items.map((a) => (
                <li key={a.id}>
                  <AnnouncementItem
                    ref={a.id === highlightId ? highlighted : undefined}
                    announcement={a}
                    highlighted={a.id === highlightId}
                    onEdit={canPost ? (x) => setDialog({ open: true, announcement: x }) : undefined}
                    onDelete={canPost ? setToDelete : undefined}
                  />
                </li>
              ))}
            </ul>
            <Pagination
              page={list.data.page}
              totalPages={list.data.totalPages}
              totalCount={list.data.totalCount}
              noun="announcement"
              onPage={setPage}
            />
          </>
        )}
      </Card>

      {poster && (
        <AnnouncementFormDialog
          open={dialog.open}
          role={poster}
          announcement={dialog.open ? dialog.announcement : undefined}
          onClose={() => setDialog({ open: false })}
          onSaved={(a) => {
            const editing = dialog.open && dialog.announcement !== undefined
            saved.show(
              editing ? `"${a.title}" saved.` : a.status === 'Scheduled' ? `"${a.title}" is scheduled.` : `"${a.title}" is published.`,
            )
            setDialog({ open: false })
          }}
        />
      )}

      <ConfirmDialog
        open={toDelete !== null}
        title="Delete announcement?"
        confirmLabel="Delete"
        onConfirm={confirmDelete}
        onClose={() => {
          setToDelete(null)
          remove.reset()
        }}
        loading={remove.isPending}
        error={remove.isError ? errorMessage(remove.error) : null}
      >
        <p>
          "{toDelete?.title}" will disappear from every feed. Notifications already sent stay in people's bells.
        </p>
      </ConfirmDialog>
    </>
  )
}
