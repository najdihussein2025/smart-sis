// TODO: replace with generated types from `npm run gen:api` (NotificationDto, UnreadCountDto).

export type NotificationType =
  | 'General'
  | 'Announcement'
  | 'Enrollment'
  | 'Grade'
  | 'AbsenceWarning'
  | 'AbsenceLimit'
  | 'AutoDropped'
  | 'Payment'
  | 'Support'

/** An in-app notification of the signed-in user (the bell). */
export interface AppNotification {
  id: number
  type: NotificationType
  title: string
  body: string
  /** Frontend route to open, e.g. "/student/announcements?announcement=4". */
  link: string | null
  /** UTC ISO. */
  createdAt: string
  /** UTC ISO, or null when unread. */
  readAt: string | null
}

export interface UnreadCount {
  count: number
}
