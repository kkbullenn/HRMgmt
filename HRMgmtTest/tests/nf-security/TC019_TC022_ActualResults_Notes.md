# TC Actual Results and Notes (Template Paste-Ready)

## TC-019 (Login as Admin)
- Actual Results:
  - FAIL. Admin could access `/Payroll/Create`, but matrix expects payroll read-only.
- Notes:
  - Matrix mismatch: Admin payroll permissions are broader than expected.

## TC-020 (Login as Manager)
- Actual Results:
  - FAIL. Manager was denied on `/Shift/Create`, but matrix expects shift CRUD for Manager.
- Notes:
  - Matrix mismatch: Manager is under-privileged for shift management routes.

## TC-021 (Login as Employee)
- Actual Results:
  - FAIL. Employee was able to access `/Users/Create` when expected to be blocked.
- Notes:
  - High-risk access-control issue; employee should not access user-creation functions.

## TC-022 (Login as HR)
- Actual Results:
  - FAIL. HR was able to access `/Users/Create` when expected to be blocked.
- Notes:
  - Matrix mismatch: HR should be Users R/U only.

## TC-023 (Unauthenticated Route Protection)
- Actual Results:
  - FAIL. Unauthenticated user was able to access `/Role/Index` (expected redirect to login).
- Notes:
  - Critical authentication/authorization gap; route appears publicly accessible.
