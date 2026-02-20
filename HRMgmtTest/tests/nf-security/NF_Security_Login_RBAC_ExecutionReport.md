# NF-Security Test Execution Report

## Build Info
- Branch: `test/aira-security-v1`
- Commit SHA: `(update after commit)`
- Executed At: `2026-02-19 23:32:22 -08:00`
- Focus: Authentication + RBAC route access (Use Cases Table role matrix)
- Environment: Local app (`http://localhost:5175`)
- DB mode: Local SQLite (`TEST_MODE=true`)
- Reason for local DB: hosted DB/schema changed frequently; local seeded baseline keeps tests reproducible.
- Seeder baseline users:
  - `qa_test / 123456` (Admin)
  - `tommy / 123456` (Admin)
  - `JerryEmployee / jerry123`
  - `JerryHR / jerry123`
  - `JerryManager / jerry123`

## Test Scope
- `TC023_UnauthenticatedRouteProtectionTests`
- `TC019_AdminRoleAccessTests`
- `TC020_ManagerRoleAccessTests`
- `TC021_EmployeeRoleAccessTests`
- `TC022_HrRoleAccessTests`

## Routes Covered
- Shift:
  - `/Shift/Create`
  - `/Shift/Edit/{id}`
  - `/Shift/Delete/{id}`
  - `/Shift/ShiftAssignment`
  - `/Shift/AssignGrid`
  - `/Shift/Index`
  - `/Shift/MyShifts`
  - `/Shift/EmployeeShift`
- Users:
  - `/Users`
  - `/Users/Create`
  - `/Users/adminEdit/{id}`
  - `/Users/Delete/{id}`
  - `/Users/Profile`
- Payroll:
  - `/Payroll/Index`
  - `/Payroll/Create`
  - `/Payroll/AdminCalculate`
  - `/Payroll/MyPayroll`
- Account:
  - `/Account/Index`
  - `/Account/Create`
- Publicly accessible in current code (still tested as findings in TC023):
  - `/Role/Index`
  - `/ShiftAssignment/Index`

## Execution Command
`dotnet test HRMgmtTest --filter "Name~TC023|Name~TC019|Name~TC020|Name~TC021|Name~TC022" -v minimal`

## Summary
- Total: 5
- Passed: 0
- Failed: 5
- Skipped: 0

## Result by Test Case
- `TC019` Admin role matrix access: **FAIL**
  - Finding: Admin can access `/Payroll/Create` (matrix says Admin payroll is read-only).
- `TC020` Manager role matrix access: **FAIL**
  - Finding: Manager cannot access `/Shift/Create` (matrix says Manager has shift C/R/U/D).
- `TC021` Employee role matrix access: **FAIL**
  - Finding: Employee can access `/Users/Create` (matrix says Employee is read-own only for Users).
- `TC022` HR role matrix access: **FAIL**
  - Finding: HR can access `/Users/Create` (matrix says HR has Users R/U only).
- `TC023` Unauthenticated route protection: **FAIL**
  - Finding: unauthenticated user can access `/Role/Index`.

## Security Findings (Against Role Matrix)
- Missing protection on `/Role/Index` (publicly reachable while unauthenticated).
- Employee and HR can reach `/Users/Create` (over-privileged vs matrix).
- Manager blocked from shift-create flow (under-privileged vs matrix).
- Admin payroll create is enabled, while matrix expects payroll read-only for Admin.

## Matrix Source Used
- `Use Cases Table.xlsx` (Sheet1), role C/R/U/D matrix:
  - Users: Admin C/R/U/D, HR R/U, Manager R/U, Employee R-own only.
  - Shifts: Admin C/R/U/D, HR R, Manager C/R/U/D, Employee R.
  - Payroll: Admin R, HR R, Manager R, Employee R-own only.

## Security Findings (Current Codebase)
- `RoleController` has no `[Authorize]`; `/Role/Index` is publicly reachable.
- `Users/Create` is reachable by Employee and HR in current run.
- `ShiftController` create/update/delete routes are restricted to Admin/HR, so Manager shift CRUD fails matrix expectation.

## Non-Finding Fixes Applied in Test Logic
- Redirect aliases (e.g., `/Shift/AssignGrid`) are treated as allowed when they redirect to valid protected targets.

## Notes
- Security tests are designed to expose access-control gaps.
- Failures are valid QA findings, not necessarily test defects.
- Tests now assert against the role matrix from `Use Cases Table.xlsx`, not the current implementation.

