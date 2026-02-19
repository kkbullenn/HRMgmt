# TC Actual Results and Notes (Template Paste-Ready)

## TC-019 (Login as Admin)
- Actual Results:
  - PASS. Admin accessed schedule, shifts, payroll, and users pages successfully.
- Notes:
  - Admin role behaved as expected on tested protected routes.

## TC-020 (Login as Manager)
- Actual Results:
  - FAIL. Manager was able to access `/Users` when expected to be blocked.
- Notes:
  - Indicates missing/insufficient role restriction for manager on user-management endpoint.

## TC-021 (Login as Employee)
- Actual Results:
  - FAIL. Employee was able to access `/Users/Create` when expected to be blocked.
- Notes:
  - High-risk access-control issue; employee should not access user-creation functions.

## TC-022 (Login as HR)
- Actual Results:
  - FAIL. HR was able to access `/Role/Index` when expected to be admin-only.
- Notes:
  - If role-management is intended admin-only, backend route restriction is missing.

## SC-001 (Unauthenticated Route Protection)
- Actual Results:
  - FAIL. Unauthenticated user was able to access `/Role/Index` (expected redirect to login).
- Notes:
  - Critical authentication/authorization gap; route appears publicly accessible.
