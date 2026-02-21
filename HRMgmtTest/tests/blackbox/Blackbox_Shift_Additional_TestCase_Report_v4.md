# Blackbox Shift Additional Testing Report (Template + Actual + Notes)

## Build / Run Info
- Project: HRMgmtTest
- Scope: TC-003, TC-004, TC-006, TC-013, TC-014
- Command:
  `dotnet test HRMgmtTest --filter "Name~TC003|Name~TC004|Name~TC006|Name~TC013|Name~TC014" -v minimal`
- Latest summary (rerun):
  - Passed: 1
  - Failed: 4
  - Skipped: 0

---

## TC-003

**Identifier:** TC-003  
**Type:** F-Blackbox  
**Test Owner:** Tim  
**Version:** v4  
**User Story Code:** US-07  

**Test Name:** Assign one shift to all employees (max 10 expected)

**Purpose:** Verify assigning an 11th employee to the same shift is blocked by max-capacity validation.

**Dependencies:**  
- Employees exist  
- Shifts exist  
- Schedule matrix UI available  
- Admin can create users (for boundary setup)  

**Testing Environment:** Software running locally

**Initialization:**  
- Login as Admin  
- Open Shift Assignment page  
- If employee count is below 11, test auto-creates additional Employee users via `Users/Create` to reach boundary

**Finalization:**  
- Delete temp template

**Actions:**  
1. Login as Admin  
2. Ensure at least 11 employees in schedule grid  
3. Create template  
4. Assign the same shift to first 11 rows in Monday column  
5. Save  
6. Capture alert/error/success signals  

**Input Data:**  
- 11 employees (boundary)  
- One shift value reused across all 11 Monday cells  

**Expected Results:**  
- 11th assignment should be blocked by max-capacity validation

**Actual Results (latest):**  
- **FAIL**  
- No max-capacity signal observed at 11th assignment (`Alert=''`, `Error=''`, `Success=''`).

**Notes:**  
- Test improvement applied: boundary setup is now deterministic (auto-create users if grid has <11 rows).  
- This converts TC003 from a setup-limited case into a true product-behavior check.  
- Current failure suggests the max-10-per-shift rule is not enforced in current code path.

---

## TC-004

**Identifier:** TC-004  
**Type:** F-Blackbox  
**Test Owner:** Tim  
**Version:** v4  
**User Story Code:** N/A  

**Test Name:** Validation of Shift Chronology and Duration

**Purpose:** Verify invalid shift date/time combinations are rejected.

**Dependencies:**  
- Shift Create page accessible as Admin

**Testing Environment:** Software running locally

**Initialization:**  
- Login as Admin  
- Open `Shift/Create`

**Finalization:** N/A

**Actions:**  
1. Submit StartDate after EndDate and StartTime after EndTime  
2. Submit equal StartTime/EndTime (zero duration)  
3. Verify submission is blocked

**Input Data:**  
- Case 1: `Start=2026-02-01`, `End=2026-01-31`, `Time=14:00-13:00`  
- Case 2: `Start=2026-02-02`, `End=2026-02-02`, `Time=09:00-09:00`

**Expected Results:**  
- Invalid submissions are rejected

**Actual Results (latest):**  
- **FAIL**  
- Both invalid cases were accepted and redirected to `/Shift`, no validation error shown.

**Notes:**  
- This is a functional validation gap, not a selector issue.

---

## TC-006

**Identifier:** TC-006  
**Type:** F-Blackbox  
**Test Owner:** Tim  
**Version:** v4  
**User Story Code:** US-07  

**Test Name:** Assignments persist after refresh & navigation

**Purpose:** Ensure saved template and generated assignments persist after refresh/navigation.

**Dependencies:**  
- Employee and shifts exist  
- Shift Assignment UI and Employee Shift page available

**Testing Environment:** Software running locally

**Initialization:**  
- Login as Admin  
- Use weekly template path  
- Use fixed seeded shifts and fixed date range

**Finalization:**  
- Delete temp template

**Actions:**  
1. Clear existing assignments for target employee on `2026-02-16..2026-02-18`  
2. Create template and set row0 Mon/Tue/Wed to fixed shifts  
3. Save and generate  
4. Refresh and navigate away/back, verify template cells persist  
5. Verify Employee Shift page has Mon/Tue/Wed events

**Input Data:**  
- Fixed shifts:  
  - Monday `D1` = `22222222-2222-2222-2222-000000000001`  
  - Tuesday `D2` = `22222222-2222-2222-2222-000000000002`  
  - Wednesday `D7` = `22222222-2222-2222-2222-000000000007`  
- Fixed range: `2026-02-16` to `2026-02-18`

**Expected Results:**  
- Template cells persist after refresh/navigation  
- Employee has generated shifts on all 3 dates

**Actual Results (latest):**  
- **FAIL**  
- Monday generated shift is missing on employee calendar.

**Notes:**  
- Test improvements applied:
  - fixed seeded shift IDs (no first-option randomness),  
  - pre-clean target dates before generate,  
  - fixed date range for deterministic verification.  
- Because setup is now deterministic, failure is a stronger functional finding (generation/persistence gap for Monday).

---

## TC-013

**Identifier:** TC-013  
**Type:** F-Blackbox  
**Test Owner:** Tim  
**Version:** v4  
**User Story Code:** US-07  

**Test Name:** Cross-page consistency (template vs employee calendar)

**Purpose:** Verify generated employee assignments match template mapping.

**Dependencies:**  
- At least 3 employees in grid  
- Shift options available

**Testing Environment:** Software running locally

**Initialization:**  
- Login as Admin  
- Create weekly template  
- Pre-clear target dates for 3 mapped employees

**Finalization:**  
- Delete temp template

**Actions:**  
1. Map row0 Monday, row1 Tuesday, row2 Wednesday with fixed seeded shifts  
2. Save and generate (`2026-02-16..2026-02-18`)  
3. Fetch `/Shift/GetEmployeeShifts` for each employee  
4. Compare date + expected token

**Input Data:**  
- Tokens expected: `D1 Morning`, `D2 Day`, `D7 Early`

**Expected Results:**  
- Employee events match template mapping exactly

**Actual Results (latest):**  
- **FAIL**  
- Row0 Monday token (`D1 Morning`) not found.

**Notes:**  
- Failure remains after deterministic setup; this is now a valid mapping inconsistency finding.

---

## TC-014

**Identifier:** TC-014  
**Type:** F-Blackbox  
**Test Owner:** Tim  
**Version:** v4  
**User Story Code:** US-07  

**Test Name:** Concurrent save/generate integrity

**Purpose:** Verify near-simultaneous admin operations do not produce duplicate/corrupt assignments.

**Dependencies:**  
- Two browser sessions  
- Existing employees and shifts

**Testing Environment:** Software running locally

**Initialization:**  
- Admin A and Admin B active on same template

**Finalization:**  
- Delete temp template  
- Close second driver

**Actions:**  
1. Admin A save  
2. Admin B save same template shortly after  
3. Both generate  
4. Verify no duplicate same employee/date rows

**Input Data:**  
- Same employee/day target with overlapping operations

**Expected Results:**  
- No duplicate same-date assignment and no corruption

**Actual Results (latest):**  
- **PASS**

**Notes:**  
- Current behavior aligns with one-shift-per-employee-per-date safeguard.

---

## Conclusion
- Current run confirms 4 functional findings and 1 pass.
- Most important:
  - TC003 now runs at true boundary (11 users) and still fails -> likely missing max-capacity enforcement.
  - TC006 deterministic setup still misses Monday generation.
  - TC013 deterministic setup still mismatches Monday mapping.
  - TC004 accepts invalid chronology/duration inputs.
