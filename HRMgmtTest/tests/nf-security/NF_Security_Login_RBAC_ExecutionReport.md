# NF-Security Test Execution Report (Expanded Route Coverage)

## Build Info
- Branch: `test/aira-security-v1`
- Commit SHA: `1f21198`
- Test Focus: Authentication + Role-based Access Control (RBAC)
- Tester: Aira
- Environment: Local app (`http://localhost:5175`)

## Test Scope
Executed:
- `TC019_AdminRoleAccessTests`
- `TC020_ManagerRoleAccessTests`
- `TC021_EmployeeRoleAccessTests`
- `TC022_HrRoleAccessTests`
- `SC001_UnauthenticatedRouteProtectionTests`

Routes covered:
- Shift: `/Shift/ShiftAssignment`, `/Shift/AssignGrid`, `/Shift/Index`, `/Shift/MyShifts`, `/Shift/EmployeeShift`
- Users: `/Users`, `/Users/Create`, `/Users/Profile`
- Payroll: `/Payroll/Index`, `/Payroll/Create`, `/Payroll/AdminCalculate`
- Role: `/Role/Index`
- Legacy ShiftAssignment: `/ShiftAssignment/Index`
- Account management: `/Account/Index`, `/Account/Create`

## Execution Command
`dotnet test HRMgmtTest --filter "Name~TC019|Name~TC020|Name~TC021|Name~TC022|Name~SC001" -v minimal`

## Result Summary
- Total: 5
- Passed: 1
- Failed: 4
- Skipped: 0

## Detailed Results

### TC-019 Admin Access
- Result: PASS
- Summary: Admin was able to access required admin/HR routes in this test set.

### TC-020 Manager Access
- Result: FAIL
- Failure observed:
  - `/Users` expected `AccessDenied`, actual `Allowed`
- Interpretation: Manager has broader access than expected policy.

### TC-021 Employee Access
- Result: FAIL
- Failure observed:
  - `/Users/Create` expected `AccessDenied`, actual `Allowed`
- Interpretation: Employee can reach user creation page (privilege escalation risk).

### TC-022 HR Access
- Result: FAIL
- Failure observed:
  - `/Role/Index` expected `AccessDenied`, actual `Allowed`
- Interpretation: HR can access role-management endpoint.

### SC-001 Unauthenticated Route Protection
- Result: FAIL
- Failure observed:
  - `/Role/Index` expected `RedirectedToLogin`, actual `Allowed`
- Interpretation: Role endpoint lacks authentication guard.

## Security Findings (from this run)

| ID | Severity | Area | Finding | Evidence |
|---|---|---|---|---|
| SEC-NF-001 | High | AuthZ | Unauthenticated access allowed to role endpoint | SC001 fail on `/Role/Index` |
| SEC-NF-002 | High | AuthZ | Employee can access `/Users/Create` | TC021 fail |
| SEC-NF-003 | Medium | AuthZ | Manager can access `/Users` unexpectedly | TC020 fail |
| SEC-NF-004 | Medium | AuthZ | HR can access `/Role/Index` (if admin-only expected) | TC022 fail |

## Conclusion
- Login security and RBAC are **not fully satisfied** across all roles/endpoints.
- Testing successfully identified access-control gaps.
- This is an expected and valid QA outcome: tests are doing their job by exposing security issues.

## Recommended Next Actions
1. Add `[Authorize]` / `[Authorize(Roles=...)]` on exposed controllers/actions (especially role management and user creation endpoints).
2. Define explicit policy for Manager role and enforce consistently.
3. Re-run the same test suite after fixes to verify closure of findings.
