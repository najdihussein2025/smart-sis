# Final regression results (T80, AB#147)

Sprint 6 hardening, PB-23 Browser compatibility. Run on 15 Nov 2026, branch `feature/p8-browser-regression`,
Windows 11, .NET 10, Node with Playwright 1.65.1, Python 3.14, SQL Server LocalDB 15.0 for the e2e database.

## 1. The official test cases are blocked

`docs/Smart-SIS-Test-Cases.xlsx` (TC_01 to TC_114 from the mobile version, referenced in PROJECT.md §1 and §10) is
**not in the repository**. Without it we can't know what each TC id asks for, its steps or its expected result, so the
official TC_01–TC_114 run is **blocked until the spreadsheet is added to `docs/`**. We did not guess its contents.

What we can show instead:
- §3: the TC ids the code already maps (from `[Trait("TC", …)]` and the docs), with the test behind each and its result.
- §4: a requirements-based regression built from PROJECT.md §3 (features) and §4 (business rules), each with its
  e2e / integration / unit evidence and a result per browser.

Once the spreadsheet is in `docs/`, walk its 114 rows against §4 below: most rows should map to a line that already
exists; anything left over needs a new test.

## 2. Suites

| Suite | Command | Result |
|---|---|---|
| Backend unit + integration (xUnit, EF InMemory + LocalDB) | `dotnet test` (backend/) | **947 passed**, 0 failed |
| Frontend (Vitest + RTL + MSW) | `npm test` (frontend/) | **275 passed** in 31 files, 0 failed |
| Frontend build + type-check (includes `e2e/`) | `npm run build` | **passed** |
| End-to-end, 4 browsers (Playwright) | `npm run e2e` (frontend/) | **88 tests** (22 specs × 4 browsers). Full run 1: 87 passed, 1 timeout (fix #2). Full run 2 (`npm run e2e`, self-starting servers): 87 passed, 1 failed on a test-selector race (fix #3). After the fix, `student.spec` in all 4 browsers: 19 passed, 5 timed out in Firefox/WebKit while other agents were load-testing on the same machine (12 min for 24 tests; failures were fixture set-up timeouts, not assertions); rerun with `E2E_WORKERS=2`: Firefox + WebKit 12 / 12 passed |
| AI service (pytest) | `pytest` (ai-service/, Python 3.14 venv) | **49 passed** |

E2E per browser (22 specs each, real API on SQL Server LocalDB, production build served by `vite preview`):

| Browser | Engine | Result |
|---|---|---|
| Chromium | Blink (Chrome) | 22 / 22 passed |
| Firefox | Gecko | 22 / 22 passed |
| WebKit | WebKit (Safari's engine, Playwright build) | 22 / 22 passed (after test fixes #2 and #3) |
| Microsoft Edge | Blink, installed Edge (`channel: 'msedge'`) | 22 / 22 passed |

On a busy machine use `E2E_WORKERS=2` (default 4): every browser shares one API and one LocalDB.

Not covered: real Safari on macOS / iOS (WebKit on Windows is the closest available), mobile viewports (360 px), and
the AI service answering for real (no LLM key in tests; the e2e checks the friendly "not available" path).

## 3. TC ids already mapped in the code

Only these ids are referenced anywhere in the repository. The meaning of TC_36–TC_43 is an assumption recorded in
`docs/decisions.md` (PB-10/PB-11 "Test case mapping (assumed)"); the others come from PROJECT.md §7.3. All passed in
the `dotnet test` run above.

| TC | What the test shows | Test(s) | Result |
|---|---|---|---|
| TC_01 | Valid login returns a token and the role (student, instructor, admin) | `Auth/AuthApiTests.Login_ValidCredentials_ReturnsTokenAndRole`; e2e `auth.spec` role landing ×3 | Pass |
| TC_02 | Bad credentials → 401 with the one generic message | `Auth/AuthApiTests.Login_BadCredentials_Returns401WithGenericMessage`; e2e `auth.spec` wrong password | Pass |
| TC_36 | Attendance roster lists enrolled students | `Attendance/AttendanceApiTests.Sheet_BeforeSubmission_ListsEnrolledStudents_Unmarked` | Pass |
| TC_37 | Submit attendance saves every mark | `Attendance/AttendanceApiTests.Submit_SavesEveryMark_AndTheSheetShowsThem`; e2e `instructor.spec` attendance | Pass |
| TC_38 | Duplicate session (BG_06) blocked | `AttendanceApiTests.Submit_SameMeetingAndDateTwice_Returns409_AndKeepsTheFirst`, `AttendanceIntegrityTests.Model_SessionsAreUniquePerMeetingAndDate`; e2e attendance second submit | Pass |
| TC_39 | Warning at limit − 1 | `AbsencePolicyTests.Evaluate_AtLimitMinusOne_SendsWarning`, `AbsenceAlertApiTests.EighthAbsence_SendsWarning_EmailAndNotification_Once` | Pass |
| TC_40 | Limit alert at the limit | `AbsencePolicyTests.Evaluate_AtLimit_SendsLimitAlert`, `AbsenceAlertApiTests.NinthAbsence_SendsLimitAlert_StillEnrolled` | Pass |
| TC_41 | Auto-drop at limit + 1 with F in GPA | `AbsencePolicyTests.Evaluate_AtLimitPlusOne_AutoDropsAndTellsTheStudent`, `AbsenceAlertApiTests.TenthAbsence_AutoDrops_WithFCountingInGpa_AndLeavesTheRoster`, `AttendanceIntegrityTests.FourMoreAbsences_ThroughTheApi_WarnOnce_AlertOnce_ThenDrop` | Pass |
| TC_42 | Student attendance view (colours, dates) | `MyAttendanceApiTests.SomeAbsences_Warning_WithTheDates_NewestFirst`; e2e student attendance tab | Pass |
| TC_43 | Another instructor's section → 403 | `AttendanceApiTests.AnotherInstructorsSection_Returns403` | Pass |
| TC_54 | Credit cap (18 passes, 19 refused) | `EnrollmentPolicyTests.CheckEnroll_ExactlyAtCap_Passes`, `CheckEnroll_OneOverCap_Throws` | Pass |
| TC_55 | Missing prerequisite refused, naming it | `EnrollmentPolicyTests.CheckEnroll_PrerequisiteNotPassed_ThrowsNamingIt` | Pass |
| TC_56 | Time conflict refused, naming the other section | `EnrollmentPolicyTests.CheckEnroll_OverlappingMeeting_ThrowsNamingTheOtherSection`; e2e student enroll with a clash | Pass |
| TC_63 | Swap to another section of the same course | `SwapPolicyTests.Check_OtherSectionOfSameCourse_AtSameTime_Passes`, `EnrollmentApiTests.Swap_ToOtherSectionOfSameCourse_MovesTheEnrollment` | Pass |
| TC_64 | Swap to a different course | `SwapPolicyTests.Check_DifferentCourse_Passes` | Pass |
| TC_65 | Swap clashing with another held section refused | `SwapPolicyTests.Check_ClashWithAnotherHeldSection_Throws` | Pass |
| TC_66 | Swap into a full section refused | `SwapPolicyTests.Check_FullSection_Throws` | Pass |
| TC_67 | Swap outside the windows refused | `SwapPolicyTests.Check_WindowClosed_Throws` | Pass |

PROJECT.md §7.3 also says "auth TC_01–06"; only TC_01 and TC_02 carry a trait today. TC_03–TC_06 and every other id
up to TC_114 have no mapping until the spreadsheet says what they are.

## 4. Requirements-based regression (PROJECT.md §3 and §4)

Evidence: **E2E** = Playwright spec in `frontend/e2e/` (run in all 4 browsers), **API** = xUnit integration test
(WebApplicationFactory), **Unit** = xUnit rules test, **UI** = Vitest page test. Browser columns: C = Chromium,
F = Firefox, W = WebKit, E = Edge. "n/a" = no browser involved (server rule).

### Shared by all roles
| Requirement | Evidence | C | F | W | E |
|---|---|---|---|---|---|
| Login with University ID + password; lands on the role's dashboard | E2E `auth.spec` (3 roles); API `AuthApiTests` | ✔ | ✔ | ✔ | ✔ |
| Wrong credentials: one generic message, clears when typing | E2E `auth.spec`; UI `LoginPage.test` | ✔ | ✔ | ✔ | ✔ |
| Empty form shows field errors | E2E `auth.spec` | ✔ | ✔ | ✔ | ✔ |
| Wrong role's area redirects to own dashboard | E2E `auth.spec` ×3 | ✔ | ✔ | ✔ | ✔ |
| Sign out; protected pages go back to login | E2E `auth.spec` | ✔ | ✔ | ✔ | ✔ |
| Forgot password always confirms (no ID discovery) | E2E `auth.spec`; API `AuthApiTests` | ✔ | ✔ | ✔ | ✔ |
| Notifications bell (unread count, list) | E2E `student.spec` support + announcements | ✔ | ✔ | ✔ | ✔ |
| Announcements feed | E2E `student.spec` | ✔ | ✔ | ✔ | ✔ |
| SR-22 role access (wrong role 403, no token 401) | API `Security/*RoleAccessTests` (8 classes) | n/a | | | |

### Student
| Requirement | Evidence | C | F | W | E |
|---|---|---|---|---|---|
| Catalog: search, open a course (details, prerequisites "None.") | E2E `student.spec` enroll journey | ✔ | ✔ | ✔ | ✔ |
| Add to cart; button turns "In Cart" | E2E `student.spec` | ✔ | ✔ | ✔ | ✔ |
| Enroll several at once; each failure shown with its reason (time conflict) | E2E `student.spec`; UI `CartPage.test`; Unit `EnrollmentPolicyTests` | ✔ | ✔ | ✔ | ✔ |
| Schedule day tabs show the enrolled class with time | E2E `student.spec` | ✔ | ✔ | ✔ | ✔ |
| Grades page (course listed, cumulative GPA) | E2E `student.spec` tabs; UI `StudentGradesPage.test`; Unit `GpaCalculatorTests` | ✔ | ✔ | ✔ | ✔ |
| Attendance per course, green "Perfect" with 0 absences | E2E `student.spec` tabs; API `MyAttendanceApiTests` | ✔ | ✔ | ✔ | ✔ |
| Financial account: pending charge, amount, due date, completed tab empty | E2E `student.spec` tabs; UI `StudentFinancePage.test` | ✔ | ✔ | ✔ | ✔ |
| Support request: submit, see admin reply, reply in the thread | E2E `student.spec` support | ✔ | ✔ | ✔ | ✔ |
| Assistant page renders; AI service down → friendly message | E2E `student.spec` assistant; API chat tests | ✔ | ✔ | ✔ | ✔ |
| Career quiz (all questions) → top 3 careers, fallback explanation | E2E `student.spec` career; API career tests; Unit `CareerScoringTests` | ✔ | ✔ | ✔ | ✔ |
| Swap and drop | API `EnrollmentApiTests`; Unit `SwapPolicyTests`; UI `CartPage.test` (not in e2e: the e2e term is in its enrollment window, where drop is closed) | n/a | | | |

### Instructor
| Requirement | Evidence | C | F | W | E |
|---|---|---|---|---|---|
| Attendance in 3 steps (section → session + DD/MM/YYYY date → students) and submit | E2E `instructor.spec` | ✔ | ✔ | ✔ | ✔ |
| Duplicate attendance blocked (A4 / BG_06) | E2E `instructor.spec`; API TC_38 tests | ✔ | ✔ | ✔ | ✔ |
| Grading: section → component → score; saved score pre-filled after reload; weight hint | E2E `instructor.spec`; UI `InstructorGradingPage.test`; Unit `GradeCalculatorTests` | ✔ | ✔ | ✔ | ✔ |
| Announcements to own section only (C1) | E2E `instructor.spec`; API announcements tests | ✔ | ✔ | ✔ | ✔ |

### Admin
| Requirement | Evidence | C | F | W | E |
|---|---|---|---|---|---|
| Create a term with both windows (DateField / DateTimeField); times read back unchanged | E2E `admin.spec`; UI `TermsPage.test`; Unit `UtcDateTimeConverterTests` | ✔ | ✔ | ✔ | ✔ |
| Create a course (code normalised to upper case, major mapping) | E2E `admin.spec`; API courses tests | ✔ | ✔ | ✔ | ✔ |
| Create a section with a weekly meeting (code upper-cased) | E2E `admin.spec`; API sections tests (clashes, capacity) | ✔ | ✔ | ✔ | ✔ |
| Users list, search, create a student | E2E `admin.spec`; API users tests | ✔ | ✔ | ✔ | ✔ |
| Charge a major (F1), record a partial payment (F2), overpayment refused (F3) | E2E `admin.spec`; API finance tests | ✔ | ✔ | ✔ | ✔ |
| Support inbox: open request, reply, status → In progress | E2E `admin.spec` | ✔ | ✔ | ✔ | ✔ |
| Post an announcement to all students | E2E `admin.spec` | ✔ | ✔ | ✔ | ✔ |
| Grade components ≤ 100 % (G1), finalize, GPA, retakes (G3–G6) | API + Unit grading tests | n/a | | | |
| Absence alerts and auto-drop (A1–A3) | API + Unit (TC_39–TC_41) | n/a | | | |
| Knowledge base, career admin | UI `KnowledgePage.test`, `AdminCareerPage.test`; API tests | n/a | | | |

### Business rules with server-only evidence (§4)
Enrollment E1–E9, grading G1–G6, attendance A1–A5, finance F1–F3, data safety D1–D3 and communication C1–C3 are all
covered by the 947 backend tests (rules unit tests per `Rules/` class, integration tests per feature). All passed.

## 5. Failures found and fixes

| # | Found by | Problem | Fix |
|---|---|---|---|
| 1 | Running the API on SQL Server (LocalDB) for the e2e tests; e2e "create a term" | SQL Server `datetime2` reads back as `DateTimeKind.Unspecified`, so the API sent every UTC time without `Z` (`"2026-10-31T21:49:43"`). All browsers then read it as **local** time: term windows showed 2 h early in Beirut and shifted again on every edit. Not seen before because the integration tests use EF InMemory, which keeps the kind. | `backend: fix DateTime kind read from SQL Server`: `SisDbContext.UtcDateTimeConverter` marks every `DateTime`/`DateTime?` UTC on read. No migration. Tests: `UtcDateTimeConverterTests` (5) and the e2e term test (window times read back as typed, in all 4 browsers). |
| 2 | First WebKit run | The career quiz test timed out at 60 s (24 questions under 4 parallel browsers). The page itself worked: the snapshot shows the results. | Test only: marked `test.slow()`. Not an app bug. |
| 3 | Final full run, WebKit | The student support test clicked "New request" after the empty list had loaded, which repeats that button, so the locator matched two buttons (strict mode). Timing, not WebKit behaviour. | Test only: `.first()`. |

No browser-specific app bug was found: forms, the custom DD/MM/YYYY `DateField`, native selects, dialogs, `Intl`
date/money formatting and the layout behaved the same in Chromium, Firefox, WebKit and Edge. A scan for newer JS APIs
(`structuredClone`, `toSorted`, `Object.groupBy`, `Intl.Segmenter`, …) found none in `src/`.
