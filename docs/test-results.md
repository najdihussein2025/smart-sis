# Final regression results (T80, AB#147)

Sprint 6 hardening, PB-23 Browser compatibility. Run on 15 Nov 2026, branch `feature/p8-browser-regression`,
Windows 11, .NET 10, Node with Playwright 1.65.1, Python 3.14, SQL Server LocalDB 15.0 for the e2e database.

## 1. How the official test cases were run

`docs/Smart-SIS-Test-Cases.xlsx` (TC_01 to TC_114 from the mobile version, PROJECT.md §1 and §10) is now in the repository
(found at `Software/Project/Smart-SIS-Test Cases.xlsx` and copied). All 114 rows were walked one by one. The mobile app
and the web rebuild don't work exactly the same way, so each row has one of three results:

| Result | Meaning | Rows |
|---|---|---|
| Pass | The web app does what the row expects and a test checks it | 86 |
| Pass (redesigned) | The web app does the same job in a different way on purpose (generic login message, deactivate instead of delete, date picker, swap dialog, rule A5 colour ranges). The test checks the web behaviour; the note says what changed | 24 |
| Not applicable | The row needs a screen or field the web app doesn't have (typing an ID to delete, to search or a payment date) | 4 |

No row fails. Nothing was marked Pass without a test that names it.

What the walk found:
- **1 real failure, fixed:** TC_17 (warning when the student already holds another section of the course). The web
  course page had no warning. Added to `CourseDetailPage` with tests.
- **11 rows had no test, so one was written** (marked "new" in §3): TC_12, TC_14, TC_16, TC_17, TC_53, TC_61, TC_62, TC_66,
  TC_68, TC_72 and TC_95. TC_66 (swap into a course with a missing prerequisite) is a backend integration test; the rest are
  Vitest page tests.
- **Wrong TC ids in the code.** The earlier mapping of TC ids (docs/decisions.md PB-10 / PB-11, "assumed") did not match the
  spreadsheet. For example TC_36–TC_43 were used for absence alerts, but in the spreadsheet TC_36–TC_43 are the
  instructor's attendance screen. The `[Trait("TC", …)]` attributes were corrected to the real ids (14 left), and the
  attributes that pointed at no real row (credit cap, window closed, absence alert levels, the 403 check) were removed.
  Those tests still run, under their `Rule` / `SR` traits.
- BG_01 to BG_08 and the other "Fail" rows in the spreadsheet were bugs of the mobile app. Each has a test in §3 that
  would catch it in the web app (BG_01 error clears, BG_06 duplicate session, BG_07 Enroll disabled, BG_08 swap clash).

The numbers of the run are in §2.

## 2. Suites

| Suite | Command | Result |
|---|---|---|
| Backend unit + integration (xUnit, EF InMemory + LocalDB) | `dotnet test` (backend/) | **979 passed**, 0 failed |
| Frontend (Vitest + RTL + MSW) | `npm test` (frontend/) | **296 passed** in 32 files, 0 failed |
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

## 3. TC_01 to TC_114

Test names are Vitest test titles (`frontend/src/features/**`), xUnit classes and methods (`backend/tests`), or Playwright
specs (`frontend/e2e`). "(new)" means the test was added during this regression. Every test named here passed in the run
in §2.

| TC | View | Title | Test(s) | Result | Note |
|---|---|---|---|---|---|
| TC_01 | Login | Empty Fields Validation | LoginPage.test 'shows field errors and does not call the API when fields are empty'; API `Login_EmptyFields_Returns400`; e2e `auth.spec` empty form | Pass | Field errors under each field instead of one red box |
| TC_02 | Login | Invalid Credentials - Wrong Password | API `Login_BadCredentials_Returns401WithGenericMessage`; e2e `auth.spec` wrong password | Pass |  |
| TC_03 | Login | User Not Found | API `Login_BadCredentials_Returns401WithGenericMessage` (unknown ID case) | Pass (redesigned) | Same generic message on purpose (SR-22: never say which part was wrong) |
| TC_04 | Login | Error Cleared | LoginPage.test 'shows the server message on bad credentials and clears it when the user types'; e2e `auth.spec` (BG_01) | Pass |  |
| TC_05 | Login | Successful Login - Student | API `Login_ValidCredentials_ReturnsTokenAndRole` (Student); e2e `auth.spec` student lands on dashboard | Pass |  |
| TC_06 | Login | Successful Login - Instructor | API `Login_ValidCredentials_ReturnsTokenAndRole` (Instructor); e2e `auth.spec` instructor lands on dashboard | Pass |  |
| TC_07 | Courses | View Major Courses | API `Catalog_DefaultsToStudentMajor_WithMajorAndElectiveTypes`, `Catalog_TypeTab_FiltersCourses` | Pass |  |
| TC_08 | Courses | View Elective Courses | CatalogPage.test 'filters by the Elective tab' | Pass |  |
| TC_09 | Courses | Navigate to Course Details Page | CatalogPage.test (CourseDetailPage) 'shows prerequisites and sections...'; e2e `student.spec` enroll journey | Pass |  |
| TC_10 | Courses | Empty Courses | CatalogPage.test 'shows an empty state when nothing matches' | Pass |  |
| TC_11 | Course Details | View Course Information | CatalogPage.test (CourseDetailPage) 'shows prerequisites and sections...'; API `CourseDetail_ShowsPrerequisitesAndSectionsWithSeats` | Pass |  |
| TC_12 | Course Details | View Schedule | CatalogPage.test (CourseDetailPage) 'TC_12: each section shows its code, instructor, seats and weekly day, time and room' (new) | Pass | BG_02: time shown as 10:00–11:15 |
| TC_13 | Course Details | Add to Cart - Available | CatalogPage.test (CourseDetailPage) 'Add to cart turns into In Cart'; e2e `student.spec` | Pass |  |
| TC_14 | Course Details | Add to Cart - Enrolled | CatalogPage.test (CourseDetailPage) 'TC_14 and TC_17: an enrolled section says Enrolled...' (new) | Pass | BG_03 |
| TC_15 | Course Details | Add to Cart - In Cart | CatalogPage.test (CourseDetailPage) 'Add to cart turns into In Cart' (In Cart button disabled) | Pass | BG_04 |
| TC_16 | Course Details | No Schedule Available | CatalogPage.test (CourseDetailPage) 'TC_16: a course with no sections says it is not offered this term' (new) | Pass (redesigned) | Text is "Not offered this term" |
| TC_17 | Course Details | Warning for Different Sections | CatalogPage.test (CourseDetailPage) 'TC_14 and TC_17...' and 'no warning when not enrolled' (new) | Pass | **Failure found:** the warning did not exist. Added to `CourseDetailPage` |
| TC_18 | Student Schedule | View Schedule by Day | SchedulePage.test 'opens on today's tab with its classes in time order'; API `Student_SeesOnlyEnrolledSections_ByDayAndTime` | Pass |  |
| TC_19 | Student Schedule | Switch Between Days | SchedulePage.test 'switches days and shows an empty day' | Pass |  |
| TC_20 | Student Schedule | Empty Day | SchedulePage.test 'switches days and shows an empty day' | Pass | Text is the web empty state |
| TC_21 | Student Schedule | Schedule Car Details | SchedulePage.test 'opens on today's tab with its classes in time order' | Pass |  |
| TC_22 | Student Attendance | View Absence Summary Cards | StudentAttendancePage.test 'shows each course with its colour status in words'; API `SomeAbsences_Warning_WithTheDates_NewestFirst` (BG_05) | Pass |  |
| TC_23 | Student Attendance | Absence Count Colors | StudentAttendancePage.test 'shows each course with its colour status in words'; `AbsencePolicyTests.StandingFor_FollowsTheColourRule` | Pass (redesigned) | Ranges follow rule A5 (limit = credits x 3), not the mobile 1-4 / 5+ |
| TC_24 | Student Attendance | Status Labels | StudentAttendancePage.test 'shows each course with its colour status in words' | Pass (redesigned) | Perfect / Warning / Critical by rule A5 |
| TC_25 | Student Attendance | Select Course to View Absences | StudentAttendancePage.test 'drills down to the dates missed' | Pass |  |
| TC_26 | Student Attendance | No Absences | StudentAttendancePage.test 'says "No absences" when opening a perfect course' | Pass |  |
| TC_27 | Student Attendance | Absence Card Details | StudentAttendancePage.test 'drills down to the dates missed' | Pass |  |
| TC_28 | Student Attendance | No Enrolled Courses | StudentAttendancePage.test 'shows the empty state with a link to the cart' | Pass |  |
| TC_29 | Student Grades | View Course Grades List | StudentGradesPage.test 'shows each course with components, not graded parts, progress and GPA' | Pass | Course cards are always open (no collapse) |
| TC_30 | Student Grades | Expand Course Card | StudentGradesPage.test 'shows each course with components...' | Pass (redesigned) | Components are always visible, so the mobile expand bug (BG_06) can't happen |
| TC_31 | Student Grades | Grade Components Display | StudentGradesPage.test 'shows each course with components...' (score / max and %) | Pass |  |
| TC_32 | Student Grades | Progress Bar | StudentGradesPage.test same test (progressbar `aria-valuenow`) | Pass |  |
| TC_33 | Student Grades | Not Graded Components | StudentGradesPage.test same test ('Not graded') | Pass |  |
| TC_34 | Student Grades | Grade Color Coding | StudentGradesPage.test same test | Pass (redesigned) | Status shown in words (colour is never the only signal) |
| TC_35 | Student Grades | Empty State | StudentGradesPage.test 'shows an empty state when not enrolled this term'; API `NewStudent_NothingYet_EmptyViews` | Pass |  |
| TC_36 | Instructor Attendance | Load Sessions by Date | InstructorAttendancePage.test 'loads the roster for the latest Monday...'; `SessionDatePolicyTests` | Pass | Web picks section, session and date in 3 steps |
| TC_37 | Instructor Attendance | Load Students List | InstructorAttendancePage.test 'loads the roster...'; API `Sheet_BeforeSubmission_ListsEnrolledStudents_Unmarked` | Pass |  |
| TC_38 | Instructor Attendance | Toggle Attendance | InstructorAttendancePage.test 'loads the roster...' (Present/Absent radios) and 'marks everyone absent at once' | Pass |  |
| TC_39 | Instructor Attendance | Summary Counts | InstructorAttendancePage.test 'loads the roster...' ("2 present, 1 absent") | Pass |  |
| TC_40 | Instructor Attendance | Submit Attendance | InstructorAttendancePage.test 'loads the roster... and submits the marks'; API `Submit_SavesEveryMark_AndTheSheetShowsThem`; e2e `instructor.spec` | Pass |  |
| TC_41 | Instructor Attendance | Prevent Duplicate | InstructorAttendancePage.test 'shows the API's 409 when attendance was already submitted'; API `Submit_SameMeetingAndDateTwice_Returns409_AndKeepsTheFirst`; e2e (BG_06 / A4) | Pass |  |
| TC_42 | Instructor Attendance | No Sessions on Date | InstructorAttendancePage.test 'refuses a date that is not on the session's weekday'; API `Submit_WrongWeekday_Returns409`, `Sheet_FutureDate_Returns409` | Pass (redesigned) | Sessions come from the weekly meetings, so a wrong day is refused |
| TC_43 | Instructor Attendance | Empty Courses | InstructorAttendancePage.test 'says so when the instructor teaches nothing this term' | Pass |  |
| TC_44 | Instructor Grading | View Grade Components | InstructorGradingPage.test 'opens the first section and component with existing scores filled in' | Pass |  |
| TC_45 | Instructor Grading | No Components Warning | InstructorGradingPage.test 'explains when the course has no components yet' | Pass |  |
| TC_46 | Instructor Grading | Select Component | InstructorGradingPage.test 'switches component and saves the scores' | Pass |  |
| TC_47 | Instructor Grading | Enter Grades | InstructorGradingPage.test 'switches component and saves the scores'; 'refuses a score above the max' | Pass |  |
| TC_48 | Instructor Grading | Submit Grades | InstructorGradingPage.test 'switches component and saves the scores'; API `SaveScores_UpdatesAndClears_OneGradePerEnrollmentAndComponent`; e2e `instructor.spec` | Pass |  |
| TC_49 | Instructor Grading | Pre-fill Existing Grades | InstructorGradingPage.test 'opens the first section and component with existing scores filled in'; API `Roster_ExistingScoresArePrefilled_AndNotGradedIsCounted`; e2e `instructor.spec` reload | Pass |  |
| TC_50 | Instructor Grading | Un-graded Count Hint | InstructorGradingPage.test 'opens the first section...' ("1 not graded") | Pass |  |
| TC_51 | Instructor Grading | Empty Courses | InstructorGradingPage.test 'shows an empty state without sections' | Pass |  |
| TC_52 | Student Shopping Cart | Enroll Multiple Courses | CartPage.test 'enrolls the chosen sections and shows each rule failure'; API `Enroll_Several_ReportsEachOutcome`; e2e `student.spec` | Pass |  |
| TC_53 | Student Shopping Cart | Enroll Button Disabled | CartPage.test 'TC_53: the Enroll button is disabled until a section is ticked...' (new) | Pass | BG_07 |
| TC_54 | Student Shopping Cart | Enroll Same Course Different Section | `EnrollmentPolicyTests.CheckEnroll_OtherSectionOfSameCourse_Throws`; API `Enroll_SecondSectionOfSameCourse_Fails` | Pass |  |
| TC_55 | Student Shopping Cart | Enroll with Missing Prerequisites | `EnrollmentPolicyTests.CheckEnroll_PrerequisiteNotPassed_ThrowsNamingIt`; API `Enroll_PrerequisitePassedWithD_Succeeds_ButFailedAttemptDoesNot` | Pass |  |
| TC_56 | Student Shopping Cart | Enroll with Schedule Conflict | `EnrollmentPolicyTests.CheckEnroll_OverlappingMeeting_ThrowsNamingTheOtherSection`; e2e `student.spec` clash | Pass |  |
| TC_57 | Student Shopping Cart | Remove from Cart | CartPage.test 'removes a section'; API `Cart_Remove_EmptiesCart_ThenSecondRemoveIs404` | Pass |  |
| TC_58 | Student Shopping Cart | Cart Refreshes After Enrollment | API `Enroll_FromCart_EnrollsRemovesFromCartAndLogsAction` | Pass |  |
| TC_59 | Student Shopping Cart | Empty Cart | CartPage.test 'shows an empty cart with a link to the catalog' | Pass |  |
| TC_60 | Student Swap | Select Course | CartPage.test 'swaps an enrolled section for a cart section...' | Pass (redesigned) | Swap starts from a cart row; the section to give up is chosen in a dialog |
| TC_61 | Student Swap | Clear Selection | CartPage.test 'TC_61 and TC_62: cancelling the swap dialog...' (new) | Pass (redesigned) | No Clear button: Cancel closes the dialog |
| TC_62 | Student Swap | Reset All Selections | CartPage.test 'TC_61 and TC_62...' (new) | Pass (redesigned) | No Reset All: one dialog, one choice |
| TC_63 | Student Swap | Successful Swap - Different Course | `SwapPolicyTests.Check_DifferentCourse_Passes` | Pass |  |
| TC_64 | Student Swap | Successful Swap - Same Course (Section Change) | `SwapPolicyTests.Check_OtherSectionOfSameCourse_AtSameTime_Passes`; API `Swap_ToOtherSectionOfSameCourse_MovesTheEnrollment` | Pass |  |
| TC_65 | Student Swap | Swap Fails - Section Full | `SwapPolicyTests.Check_FullSection_Throws`; CartPage.test 'swaps ... and shows a refused swap' (full) | Pass |  |
| TC_66 | Student Swap | Swap Fails - Missing Prerequisites | API `Swap_IntoCourseWithMissingPrerequisite_Returns409NamingIt_AndKeepsTheOldSection` (new) | Pass |  |
| TC_67 | Student Swap | Swap Fails - Schedule Conflict | `SwapPolicyTests.Check_ClashWithAnotherHeldSection_Throws` (BG_08) | Pass |  |
| TC_68 | Student Swap | Empty Cart | CartPage.test 'TC_68: an empty cart offers no swap and links to the catalog' (new) | Pass |  |
| TC_69 | Student Drop | Drop Course - Confirm | CartPage.test 'drops an enrolled course after confirming, during add/drop'; API `Drop_InAddDrop_FreesTheSeat` | Pass |  |
| TC_70 | Student Drop | Drop Course - Cancel | CartPage.test 'cancelling the confirmation keeps the course' | Pass |  |
| TC_71 | Student Drop | Drop Course - Seat Restored | API `CourseDropApiTests.Drop_ReturnsTheSeatToTheSection` | Pass |  |
| TC_72 | Student Drop | No Enrolled Courses | CartPage.test 'TC_72: with no enrolled courses the enrolled list says so' (new) | Pass |  |
| TC_73 | Admin Add Student | Add Student Success | UsersPage.test 'creates a student'; API `Create_Student_Returns201WithDetail`; e2e `admin.spec` | Pass |  |
| TC_74 | Admin Add Student | Missing Required Fields | UsersPage.test 'checks the fields before sending' | Pass |  |
| TC_75 | Admin Add Student | Invalid ID Format | UsersPage.test 'checks the fields before sending' (ID rule) | Pass (redesigned) | University ID must have 4 to 9 digits |
| TC_76 | Admin Add Student | Invalid Date Format - DOB | UsersPage.test 'checks the fields before sending' (date of birth); `shared/lib/dates.test` | Pass (redesigned) | Date picker in DD/MM/YYYY, so a wrong format can't be typed |
| TC_77 | Admin Add Student | Invalid Date Format - Enrollment | UsersPage.test 'checks the fields before sending'; `shared/lib/dates.test` | Pass (redesigned) | Same date field as TC_76 |
| TC_78 | Admin Add Student | Duplicate Student ID | UsersPage.test 'shows duplicate University ID and email (409) under their fields'; API `Create_DuplicateUniversityId_Returns409` | Pass |  |
| TC_79 | Admin Delete Student | Delete Existing Student | UsersPage.test 'deactivates through a confirm dialog, and Cancel does nothing'; API `Deactivate_UserCantLogInButStaysVisibleToAdmin` | Pass (redesigned) | Deactivate, never hard delete (D1) |
| TC_80 | Admin Delete Student | Cancel Deletion | UsersPage.test 'deactivates through a confirm dialog, and Cancel does nothing' | Pass (redesigned) |  |
| TC_81 | Admin Delete Student | Empty ID Field | - | Not applicable | No ID field: the account is picked from the list |
| TC_82 | Admin Delete Student | Student Not Found | API `Deactivate_UnknownId_Returns404` | Pass (redesigned) | No ID field: unknown IDs only reach the API |
| TC_83 | Admin Add Instructor | Add Instructor Success | UsersPage.test 'creates an instructor with a department'; API `Create_Instructor_Returns201WithDetail` | Pass |  |
| TC_84 | Admin Add Instructor | Missing Required Fields | UsersPage.test 'checks the fields before sending' | Pass |  |
| TC_85 | Admin Add Instructor | Invalid ID Format | UsersPage.test 'checks the fields before sending' | Pass (redesigned) | University ID must have 4 to 9 digits |
| TC_86 | Admin Add Instructor | Invalid Date Format | UsersPage.test 'checks the fields before sending' | Pass (redesigned) | Date picker, as TC_76 |
| TC_87 | Admin Add Instructor | Duplicate Instructor ID | UsersPage.test 'shows duplicate University ID and email (409)...' | Pass |  |
| TC_88 | Admin Delete Instructor | Delete Existing Instructor | UsersPage.test 'deactivates through a confirm dialog, and Cancel does nothing' | Pass (redesigned) | Deactivate, as TC_79 |
| TC_89 | Admin Delete Instructor | Cancel Deletion | UsersPage.test 'deactivates through a confirm dialog, and Cancel does nothing' | Pass (redesigned) |  |
| TC_90 | Admin Delete Instructor | Empty ID Field | - | Not applicable | No ID field, as TC_81 |
| TC_91 | Admin Delete Instructor | Instructor Not Found | API `Deactivate_UnknownId_Returns404` | Pass (redesigned) | As TC_82 |
| TC_92 | Admin Add Payment | Add Payment Success | AdminPaymentsPage.test 'charges a whole major'; API `CreateBatch_ChargesOnlyActiveStudentsOfThatMajor`; e2e `admin.spec` | Pass |  |
| TC_93 | Admin Add Payment | Add Payment – No Students | AdminPaymentsPage.test 'shows field errors and the 409 when the major has nobody to charge'; API `CreateBatch_MajorWithNoActiveStudents_Returns409AndCreatesNothing` | Pass (redesigned) | Refused with a 409 instead of a "success, no students" message |
| TC_94 | Admin Add Payment | Missing Fields | AdminPaymentsPage.test 'shows field errors and the 409...' (Choose a major / Enter a title / Choose a due date) | Pass |  |
| TC_95 | Admin Add Payment | Invalid Amount | AdminPaymentsPage.test 'TC_95: an amount that is negative, zero or not a number is refused before sending' (new) | Pass |  |
| TC_96 | Admin Add Payment | Invalid Due Date Format | AdminPaymentsPage.test 'shows field errors...' (Choose a due date); API `CreateBatch_UnknownReferencesAndBadFields_Return400WithFieldErrors` | Pass (redesigned) | Date picker in DD/MM/YYYY |
| TC_97 | Admin Mark Payments | Search Student with Pending Payments | AdminPaymentsPage.test 'lists charges with status, balance and overdue'; API `SearchCharges_FiltersByStudentAndStatus` | Pass | Charges list filtered by student and status |
| TC_98 | Admin Mark Payments | Search Student with No Payments | AdminPaymentsPage.test 'shows the empty state' | Pass |  |
| TC_99 | Admin Mark Payments | Invalid Student ID | API `CreateCharge_UnknownOrDeactivatedStudent_Returns400OnUniversityId` | Pass (redesigned) | No search box for an ID; unknown IDs only reach the charge form |
| TC_100 | Admin Mark Payments | Empty Search Field | - | Not applicable | No ID search field (list with filters) |
| TC_101 | Admin Mark Payments | Tap Payment to Mark Paid | AdminPaymentsPage.test 'records a partial payment and shows the remaining balance' (dialog) | Pass |  |
| TC_102 | Admin Mark Payments | Confirm Payment Success | AdminPaymentsPage.test 'records a partial payment...'; API `RecordPayment_PartialThenRest_GoesPendingPartialPaid` (payments read back from the database) | Pass | Payment persists after reload |
| TC_103 | Admin Mark Payments | Invalid Amount | AdminPaymentsPage.test 'blocks an overpayment before sending it'; API `RecordPayment_ZeroAmount_Returns400_UnknownCharge_Returns404` | Pass |  |
| TC_104 | Admin Mark Payments | Invalid Date Format | - | Not applicable | No payment date field: the payment time is set by the server |
| TC_105 | Admin Grade Components | Select Course | GradeComponentsPage.test 'asks for a course first, then lists its components with the total weight' | Pass |  |
| TC_106 | Admin Grade Components | View Existing Components | GradeComponentsPage.test same test | Pass |  |
| TC_107 | Admin Grade Components | Empty Components | GradeComponentsPage.test 'shows an empty state for a course without components' | Pass |  |
| TC_108 | Admin Grade Components | Weight Tracker | GradeComponentsPage.test 'asks for a course first...' (progressbar Total weight) | Pass |  |
| TC_109 | Admin Grade Components | Add Component Success | GradeComponentsPage.test 'adds a component'; API `Create_ThenList_ShowsComponentsAndTotal` | Pass |  |
| TC_110 | Admin Grade Components | Add Component – Missing Fields | GradeComponentsPage.test 'shows field errors without calling the API' | Pass |  |
| TC_111 | Admin Grade Components | Add Component – Invalid Values | GradeComponentsPage.test 'shows field errors without calling the API'; API `Create_InvalidFields_Returns400WithFieldErrors` | Pass |  |
| TC_112 | Admin Grade Components | Add Component – Weight Exceeds 100% | GradeComponentsPage.test 'shows the 409 over-100% message under Weight'; `GradeComponentPolicyTests`; API `Create_WeightsOver100_Returns409` | Pass |  |
| TC_113 | Admin Grade Components | Delete Component | GradeComponentsPage.test 'deletes after confirming...'; API `Delete_WithoutScores_Returns204` | Pass |  |
| TC_114 | Admin Grade Components | Delete Cancellation | GradeComponentsPage.test 'deletes after confirming...' (Cancel keeps the component) | Pass |  |

Totals: 86 Pass, 24 Pass (redesigned), 4 Not applicable, 0 Fail.

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
covered by the backend tests (rules unit tests per `Rules/` class, integration tests per feature). All passed.

## 5. Failures found and fixes

| # | Found by | Problem | Fix |
|---|---|---|---|
| 1 | Running the API on SQL Server (LocalDB) for the e2e tests; e2e "create a term" | SQL Server `datetime2` reads back as `DateTimeKind.Unspecified`, so the API sent every UTC time without `Z` (`"2026-10-31T21:49:43"`). All browsers then read it as **local** time: term windows showed 2 h early in Beirut and shifted again on every edit. Not seen before because the integration tests use EF InMemory, which keeps the kind. | `backend: fix DateTime kind read from SQL Server`: `SisDbContext.UtcDateTimeConverter` marks every `DateTime`/`DateTime?` UTC on read. No migration. Tests: `UtcDateTimeConverterTests` (5) and the e2e term test (window times read back as typed, in all 4 browsers). |
| 2 | First WebKit run | The career quiz test timed out at 60 s (24 questions under 4 parallel browsers). The page itself worked: the snapshot shows the results. | Test only: marked `test.slow()`. Not an app bug. |
| 3 | Final full run, WebKit | The student support test clicked "New request" after the empty list had loaded, which repeats that button, so the locator matched two buttons (strict mode). Timing, not WebKit behaviour. | Test only: `.first()`. |
| 4 | Walking TC_01–TC_114 (TC_17) | The course page had no warning when the student already holds another section of that course | `frontend: warn on course page when already enrolled in another section`: an alert on `CourseDetailPage` that points to Swap. Tests: `CatalogPage.test` TC_14/TC_17 |

No browser-specific app bug was found: forms, the custom DD/MM/YYYY `DateField`, native selects, dialogs, `Intl`
date/money formatting and the layout behaved the same in Chromium, Firefox, WebKit and Edge. A scan for newer JS APIs
(`structuredClone`, `toSorted`, `Object.groupBy`, `Intl.Segmenter`, …) found none in `src/`.
