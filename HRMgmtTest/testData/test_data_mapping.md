# Test Data Mapping

This document maps test cases to their required test data (employees and shifts).

## Test Case Data Requirements

### TC-002: Assign Same Shift to Multiple Employees
**File:** `TC002_AssignSameShiftToMultipleEmployees.cs`

**Employees:**
- E001 (Emily Anderson) - `11111111-1111-1111-1111-000000000001`
- E002 (Michael Brown) - `11111111-1111-1111-1111-000000000002`
- E003 (Sarah Chen) - `11111111-1111-1111-1111-000000000003`

**Shifts:**
- D2 Day (09:00-17:00) - `22222222-2222-2222-2222-000000000002`

**Purpose:** Verify that one shift can be assigned to multiple employees

---

### TC-005: Assign Different Shifts to Different Employees
**File:** `TC005_AssignDifferentShifts.cs`

**Employees:**
- E005 (James Wilson) - `11111111-1111-1111-1111-000000000005`
- E006 (Jennifer Taylor) - `11111111-1111-1111-1111-000000000006`
- E007 (David Martinez) - `11111111-1111-1111-1111-000000000007`

**Shifts:**
- D4 Afternoon (12:00-17:00) - `22222222-2222-2222-2222-000000000004`
- D5 Mid-Morning (10:00-13:30) - `22222222-2222-2222-2222-000000000005`
- D6 Mid-Afternoon (13:30-19:00) - `22222222-2222-2222-2222-000000000006`

**Purpose:** Verify system handles different shifts for different employees simultaneously

---

### TC-007: Prevent Overlapping Shifts for Same Employee
**File:** `TC007_PreventOverlappingShifts.cs`

**Employees:**
- E008 (Jessica Lee) - `11111111-1111-1111-1111-000000000008`

**Shifts:**
- D7 Early (07:00-15:00) - `22222222-2222-2222-2222-000000000007`
- D8 Overlapping (10:00-18:00) - `22222222-2222-2222-2222-000000000008`

**Purpose:** Verify system prevents overlapping shift assignments
**Note:** D7 and D8 intentionally overlap (10:00-15:00) to test validation

---

### TC-008: Prevent Duplicate Assignment on Re-Generation
**File:** `TC008_PreventDuplicateAssignment.cs`

**Employees:**
- E001 (Emily Anderson) - `11111111-1111-1111-1111-000000000001`

**Shifts:**
- D1 Morning (08:00-16:00) - `22222222-2222-2222-2222-000000000001`

**Purpose:** Verify generating schedule multiple times doesn't create duplicates

---

### TC-012: Delete Assignment from Employee Calendar
**File:** `TC012_DeleteAssignment.cs`

**Employees:**
- E001 (Emily Anderson) - `11111111-1111-1111-1111-000000000001`

**Shifts:**
- D1 Morning (08:00-16:00) - `22222222-2222-2222-2222-000000000001`

**Purpose:** Verify deleting an assignment works and persists after refresh

---

## GUID Patterns

### Employees
All employee test data uses GUIDs with the pattern:
```
11111111-1111-1111-1111-00000000000X
```
Where X is the employee number (1, 2, 3, 5, 6, 7, 8)

### Shifts
All shift test data uses GUIDs with the pattern:
```
22222222-2222-2222-2222-00000000000X
```
Where X is the shift number (1, 2, 4, 5, 6, 7, 8)

These patterns ensure:
1. Test data is easily identifiable
2. Test data doesn't conflict with production data
3. Test data can be easily cleaned up with pattern-based queries

---

## Database Setup

1. Run migrations to create the schema
2. Execute `test_employees_shifts.sql` to insert test data
3. Run tests - all GUIDs are hardcoded in the test files

## Cleanup Queries

To remove all test data after testing:

```sql
-- Delete test shift assignments
DELETE FROM ShiftAssignments 
WHERE UserId LIKE '11111111-1111-1111-1111-%' 
   OR ShiftId LIKE '22222222-2222-2222-2222-%';

-- Delete test scheduling templates
DELETE FROM SchedulingTemplates 
WHERE UserId LIKE '11111111-1111-1111-1111-%' 
   OR ShiftType LIKE '22222222-2222-2222-2222-%';

-- Delete test template generation logs (if exists)
DELETE FROM TemplateGenerationLogs 
WHERE TemplateName IN ('WK_TC002', 'WK_TC004', 'WK_TC008');

-- Delete test shifts
DELETE FROM Shifts WHERE ShiftId LIKE '22222222-2222-2222-2222-%';

-- Delete test employees
DELETE FROM Users WHERE UserId LIKE '11111111-1111-1111-1111-%';
```
