# Decisions and assumptions

Choices made while building the remaining sprints where the board or `PROJECT.md` left room for more than one reading.
Each one follows the project doc where it says anything; change it here and in the code together.

## Sprint 3

### Board vs code (Sprint 2 carry-over)
The board shows the Sprint 2 backend items as done, but the code only had entities for Cart and Enrollments.
The missing backend was built first, as part of the carried-over items:
cart API (T14), enroll + swap API with `EnrollmentPolicy` / `SwapPolicy` (T17, T18), my enrollments (T20) and the drop API (T24).
Work item numbers for these were inferred from the sequence (T13 = AB#66 … T16 = AB#69).

### Enrolling several sections at once
`POST /api/enrollment/enroll` takes up to 10 section ids and tries each **in its own transaction, in order**, returning
`200` with one result per section (`success`, `message`, `code`). It does not fail the whole request with a single `409`,
because the UI must show every failure (T19). A later section sees the credits and meetings of the earlier ones that succeeded.
Swap and drop are single actions and still answer `409` with the rule's message.

### Cart
- Adding to the cart is allowed outside the registration windows (students plan ahead). Rules run when they enroll.
- Two sections of the same course may sit in the cart, so one can be swapped for the other.
- A section must belong to the current term and the student must not already be enrolled in it.

### Seats
Seats taken = enrollments with status `Enrolled` **or** `Completed` (PROJECT.md §4 "Sections"), the same count the admin
sections page uses. `business-rules.md` E6 mentions only `Enrolled`; in the current term the two are the same until grades
are finalized, and counting `Completed` keeps the number stable after finalization.

### Auto-drop audit row
`EnrollmentActions.PerformedBy` for an `AutoDrop` is the instructor whose attendance submission caused it (there is no
system user account).

### Drop
Dropping sets `Status = Dropped`, `CountsInGpa = false`. Re-enrolling later creates a new enrollment row.

### Frontend placement
- The student catalog lives in `features/catalog` (the admin course pages stay in `features/courses`).
- Enroll results, swap and drop live on the Cart page (one registration screen), using the `enrollment` feature's hooks.

## PB-12 Announcements and notifications

### Audiences and feed (rule C1)
- Admin posts to All, Students, Instructors or one Major. Instructor posts to one Section, only a section they teach
  (403 otherwise, 400 if the section doesn't exist). "All" includes admins.
- `GET /api/announcements` is one feed for every role, newest `PublishAt` first, showing posts that are published and not
  expired: student = All + Students + own major + sections they are **Enrolled** in; instructor = All + Instructors +
  sections they teach; admin = everything. The author also sees their own **scheduled** posts there (status `Scheduled`).
- Manage lists show every status: `GET /api/admin/announcements` (all posts) and `GET /api/instructor/announcements`
  (own posts), with an optional `status` filter (`Scheduled` / `Live` / `Expired`, computed from the dates, never stored).
- An admin may edit or delete any post; an instructor only their own. When an admin edits an instructor's section post it
  keeps its section (the admin can't move a post onto a section).

### Publish and expiry times
- `PublishAt` empty or in the past means "now"; a future time schedules the post. Editing a post that is already live
  keeps its original `PublishAt` (fixing a typo doesn't push it to the top of the feed); editing a scheduled post with an
  empty time publishes it now.
- `ExpiresAt` is optional, must be in the future and after `PublishAt` (400 on `expiresAt` otherwise).
- Times are UTC in the API, like term windows.

### Delete is a hard delete
`Announcements` has no `DeletedAt` column and isn't a user record (rule D1 is about users), so deleting removes the row
without a migration. Notifications already sent stay in the bell; their link opens the feed, where the post is gone.

### Notifications for an announcement
- One `Notification` (type `Announcement`) per active audience member except the author. The title is the post title,
  the body the post text (cut to 1000 characters), the link the reader's own feed: `/<role>/announcements?announcement=<id>`.
- A post that is live when saved notifies in the same transaction. A scheduled post is notified by
  `AnnouncementPublishJob` (every minute) once its `PublishAt` passes, looking back 7 days.
- "Already notified" = a notification whose link ends in `?announcement=<id>` exists, so restarts and edits never notify
  twice. No migration was needed. A `NotifiedAt` column on `Announcements` would make this check cheaper if the table
  grows; it was left out because this sprint must not add migrations.
- The audience is resolved with one query of user ids and inserted in one `SaveChanges` (`INotificationService.NotifyMany`).
