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

  // Posters land on what they manage; the feed is the second tab. A link from a notification opens the feed.
  const [view, setView] = useState<View>(poster && !highlightId ? 'manage' : 'feed')
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
  const newButton = (
    <Button onClick={() => setDialog({ open: true })}>
      <Plus className="size-4" aria-hidden /> New announcement
    </Button>
  )

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={role === 'Admin' ? 'Communication' : 'Campus'}
        title="Announcements"
        icon={Megaphone}
        actions={canPost && newButton}
      >
        {intro[role]}
      </PageHeader>

      {saved.message && <Alert tone="success">{saved.message}</Alert>}

      {canPost && (
        <div className="flex flex-wrap items-end justify-between gap-3">
          <Tabs
            label="Announcement lists"
            value={view}
            onChange={switchView}
            items={[
              { value: 'manage', label: manageLabel },
              { value: 'feed', label: 'Feed' },
            ]}
          />
          {view === 'manage' && (
            <div className="w-full sm:w-48">
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
          view === 'feed' ? (
            <EmptyState icon={Megaphone} title={canPost ? 'No announcements for you yet' : 'No announcements yet'}>
              {canPost
                ? 'Announcements addressed to you will show up here and in the bell.'
                : 'Nothing is published for you right now. New announcements also show up in the bell.'}
            </EmptyState>
          ) : status ? (
            <EmptyState
              icon={Megaphone}
              title="Nothing posted here"
              action={
                <Button variant="ghost" onClick={() => setStatus('')}>
                  Clear filters
                </Button>
              }
            >
              No announcement has this status. Choose another status.
            </EmptyState>
          ) : (
            <EmptyState icon={Megaphone} title="Nothing posted here" action={newButton}>
              Post one with “New announcement”.
            </EmptyState>
          )
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
    </div>
  )
}
