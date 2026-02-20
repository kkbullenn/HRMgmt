# Blackbox Shift Test Case Report (Local DB)

This report follows the testcase template format and reflects the current codebase/local-db run.

## Common Local Baseline
- DB: Local SQLite with `TEST_MODE=true`
- Seeder: `HRMgmt/SeedData/QaSeeder.cs`
- Login used by automation: `qa_test / 123456`
- Additional seeded accounts: `tommy`, `JerryEmployee`, `JerryHR`, `JerryManager`
- Baseline templates: `QA_WEEKLY_BASE`, `QA_BIWEEKLY_BASE`

---

## TC-001
- Identifier: TC-001
- Type: F-Blackbox
- Test Owner: Aira
- Version: Current
- User Story Code: US-07
- Test Name: Assign single shift to single employee
- Purpose: Verify assigning one shift to one employee is saved and generated correctly using an existing template.
- Dependencies: Employee exists; shift exists; schedule matrix UI available; existing template exists.
- Testing Environment: Software running locally.
- Initialization: Load existing template (prefer `QA_WEEKLY_BASE`).
- Finalization: N/A
- Actions:
  1. Login as Admin
  2. Open Schedule Page
  3. Load existing template
  4. Assign one available shift to one employee on Monday
  5. Save
  6. Generate (with date range)
  7. Verify via employee shift data API and persisted grid
- Input Data: Runtime-selected existing employee/shift/template (local seeded DB).
- Expected Results: Save and generate succeed; assignment persists in grid and at least one generated event exists in target date range.
- Actual Results: PASS. Save+generate completed; persisted grid assignment and generated range event were both validated.
- Notes: Placeholder IDs like E001/D1 are logical examples only; automation uses runtime data.
- Status: DONE

## TC-009
- Identifier: TC-009
- Type: F-Blackbox
- Test Owner: Aira
- Version: Current
- User Story Code: US-07
- Test Name: Save behavior for no-change and empty-state cases
- Purpose: Verify save behavior for unchanged template state and empty-grid validation.
- Dependencies: Schedule matrix UI available; existing template exists (weekly preferred).
- Testing Environment: Software running locally.
- Initialization: Login and navigate to Schedule page; load existing template.
- Finalization: N/A
- Actions:
  1. Login as Admin
  2. Open Schedule Page
  3. Load existing template
  4. Scenario 1: Save without edits
  5. Scenario 2: Clear all visible assignments and Save
  6. Validate behavior/messages and template-list count integrity
- Input Data:
  - Scenario 1: Existing template, no edits
  - Scenario 2: Empty visible grid
- Expected Results:
  - Scenario 1: Save works; data unchanged
  - Scenario 2: Save is blocked by validation signal (alert or error), no false success
  - Template count does not change in either scenario
- Actual Results: PASS. Both scenarios behaved as expected; template count remained unchanged.
- Notes: Updated from old “Save disabled” assumption to current implementation behavior.
- Status: DONE

## TC-010
- Identifier: TC-010
- Type: F-Blackbox
- Test Owner: Aira
- Version: Current
- User Story Code: US-07
- Test Name: Duplicate template name is rejected
- Purpose: Verify duplicate template name is rejected in create flow.
- Dependencies: At least one existing template in Template List.
- Testing Environment: Software running locally.
- Initialization: Existing template loaded from menu (prefer `QA_WEEKLY_BASE`).
- Finalization: N/A
- Actions:
  1. Login as Admin
  2. Open Schedule Page
  3. Click Create new template
  4. Enter an existing template name
  5. Trigger validation path (blur/save)
  6. Capture duplicate rejection signal
  7. Verify template count is unchanged
- Input Data: Existing template name from list.
- Expected Results: Duplicate template is rejected; validation shown; no new template created.
- Actual Results: PASS. Duplicate-name rejection captured and template count remained unchanged.
- Notes: Current UI may validate on blur before final save.
- Status: DONE

## TC-011
- Identifier: TC-011
- Type: F-Blackbox
- Test Owner: Aira
- Version: Current
- User Story Code: US-07
- Test Name: Biweekly generation requires Week 1 and Week 2
- Purpose: Verify generation is blocked when only one biweekly week is populated.
- Dependencies: Biweekly mode available; at least one employee and shift available.
- Testing Environment: Software running locally.
- Initialization: Open Schedule page; create biweekly template.
- Finalization: N/A
- Actions:
  1. Login as Admin
  2. Set Week Type = Biweekly
  3. Populate Week 1 only
  4. Save
  5. Attempt Generate
  6. Verify no new employee assignments are created when blocked
- Input Data: Week 1 populated, Week 2 empty.
- Expected Results: Generation blocked with message requiring both Week 1 and Week 2; no new assignments created.
- Actual Results: PASS. Blocking message shown and employee assignment count remained unchanged.
- Notes: Test re-establishes biweekly state after save refresh.
- Status: DONE

## TC-015
- Identifier: TC-015
- Type: F-Blackbox
- Test Owner: Aira
- Version: Current
- User Story Code: US-07
- Test Name: Batch generation prevents second same-date shift for employee
- Purpose: Verify Save+Generate flow enforces one shift per employee per date.
- Dependencies: Existing template, employee, and at least 2 shifts available.
- Testing Environment: Software running locally.
- Initialization: Load existing template (prefer `QA_WEEKLY_BASE`).
- Finalization: N/A
- Actions:
  1. Login as Admin
  2. Load existing template
  3. Set Shift A for employee/day and Save
  4. Generate
  5. Change same employee/day to Shift B and Save
  6. Generate again
  7. Verify employee shifts on target date
  8. Verify there are no duplicate assignments for any single date
- Input Data: Runtime-selected employee + two shift options; fixed generation date window used by test.
- Expected Results: Only one assignment remains for employee on same date after both generations; no duplicate dates exist for employee assignments.
- Actual Results: PASS. No duplicate second same-date assignment created; no duplicate date entries detected.
- Notes: This covers batch generation path and is distinct from manual overlapping-shift assignment tests.
- Status: DONE
