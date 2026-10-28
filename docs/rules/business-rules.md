# Business rules (must never be broken)

Condensed from PROJECT.md §4 and §5.10. Each rule says **where** it lives. If code enforces a rule
somewhere else, it's in the wrong place.

## Enrollment (feature: Enrollment, rules in `Rules/EnrollmentPolicy.cs`, `Rules/SwapPolicy.cs`)
| # | Rule | Where |
|---|---|---|
| E1 | Enrollment window: enroll + swap only. Add/Drop window: add + drop + swap. Outside both: locked | Policy, dates from `Terms` |
| E2 | Credit cap = `Students.MaxCreditsOverride ?? Majors.DefaultMaxCredits` (default 18) per term | Policy |
| E3 | Prerequisite passed = a `Completed` enrollment with `GradePoints >= 1.0` (D or better) | Policy |
| E4 | One section per course per term | Policy |
| E5 | No time overlap with the student's other `Enrolled` sections (same day, `start < otherEnd && otherStart < end`) | Policy |
| E6 | Free seat: count `Enrolled` rows for the section inside the transaction with `UPDLOCK, ROWLOCK` | Service + repository |
| E7 | Same section can't be in the cart twice | Unique index on `CartItems(StudentId, SectionId)` |
| E8 | Seats are **counted**, never stored on `Sections` | — |
| E9 | Every enroll / swap / drop / auto-drop writes an `EnrollmentActions` row | Service |

Check order on enroll and swap: **window → credit cap → prerequisites → same course → time conflict → capacity**.
The first failing check throws `BusinessRuleException` with a human message ("Schedule conflict with CSC 210", "Section is full").

## Grading (feature: Grading, rules in `Rules/GradeCalculator.cs`, `Rules/GpaCalculator.cs`)
| # | Rule |
|---|---|
| G1 | Admin defines components per course per term; sum of `Weight` ≤ 100 (validated on insert and update) |
| G2 | Instructor enters scores, only for sections they teach; one grade per enrollment per component (unique index) |
| G3 | Final % = Σ (score / max × weight), **rounded to nearest whole number** (`MidpointRounding.AwayFromZero`), then looked up in `GradeScales` |
| G4 | GPA on 4.0 scale, credit-weighted. Only enrollments with `CountsInGpa = true` |
| G5 | Retake: when a new attempt completes, set `CountsInGpa = false` on older attempts of the same course |
| G6 | `TermResults` is a cache; rebuild it when grades are finalized. Never edit it by hand |

## Attendance (feature: Attendance, rules in `Rules/AbsencePolicy.cs`)
| # | Rule |
|---|---|
| A1 | Limit = `Credits × 3` |
| A2 | At `limit − 1` → Warning alert; at `limit` → Limit alert. Each = email + notification, sent **once** (checked via `AbsenceAlerts`) |
| A3 | At `limit + 1` → auto-drop via `IEnrollmentService.DropForAbsence()` → `Status = DroppedAbsence`, `LetterGrade = F`, `GradePoints = 0`, counts in GPA |
| A4 | One attendance session per meeting per date (unique index `MeetingId + SessionDate`) — fixes BG_06 |
| A5 | Student colours: 0 → green "Perfect"; 1 … limit−2 → orange "Warning"; ≥ limit−1 → red "Critical" |

## Finance (feature: Finance)
| # | Rule |
|---|---|
| F1 | A charge batch creates one `Charge` per active student in the major |
| F2 | Partial payments allowed. Status from Σ payments: 0 → Pending, < amount → Partial, ≥ amount → Paid. Recompute after every payment |
| F3 | A payment can't exceed the remaining balance |

## Data safety (everywhere)
| # | Rule |
|---|---|
| D1 | Users are never hard-deleted: `IsActive = false` + `DeletedAt`. The soft-delete interceptor converts deletes |
| D2 | All foreign keys are `Restrict` (applied globally in `SisDbContext`). Never add `Cascade` |
| D3 | Simulated data only in dev and tests (SR-23) |

## Communication
| # | Rule |
|---|---|
| C1 | Admin announcements: All / Students / Instructors / Major. Instructor: Section scope, **own sections only** |
| C2 | Emails go through `EmailOutbox` (queued, sent by `EmailOutboxJob`). Never send mail inline in a request |
| C3 | All support requests are handled by admins |
