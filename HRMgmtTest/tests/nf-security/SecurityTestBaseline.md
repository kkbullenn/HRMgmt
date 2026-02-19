# Security Test Baseline (Pre-Filled)

## Build Info
- Branch: `test/aira-security-v1`
- Commit SHA: `1f21198`
- Test Date: `2026-02-19`
- Test Time: `13:32:37 -08:00`
- Tester: `Aira`
- Environment (URL/DB): `Local (fill app URL + DB name)`

## Security Scope
- Authentication
- Role-based Access Control (RBAC)
- Session/Logout
- Direct URL access control

## Access Matrix (Expected Policy)
| Route | Unauth | Admin | HR | Manager | Employee |
|---|---|---|---|---|---|
| `/Shift/ShiftAssignment` | Deny | Allow | Allow | Deny (unless policy says allow) | Deny |
| `/Payroll/Index` | Deny | Allow | Allow | Deny | Deny |
| `/Payroll/AdminCalculate` | Deny | Allow | Deny (if admin-only policy) | Deny | Deny |
| `/Users` | Deny | Allow | Allow | Deny/Policy | Limited self-only |
| `/Role` | Deny | Admin only | Deny | Deny | Deny |

## Test Cases

### TC-019 (Admin access)
- Expected (Policy): Admin can access admin/HR views and functions.
- Actual (Current Build): Likely allowed on Shift/Payroll/Users; role pages may be globally open due to missing `[Authorize]`.
- Result: `Needs execution`
- Evidence: `(add screenshots/status codes)`

### TC-020 (Manager access)
- Expected (Policy): Define explicitly with team (currently unclear).
- Actual (Current Build): Manager role appears in UI, but backend role attributes mostly allow only `Admin,HR`.
- Result: `Needs execution`
- Evidence: `(add screenshots/status codes)`

### TC-021 (Employee access)
- Expected (Policy): Employee should only see personal/limited pages.
- Actual (Current Build): Some protections exist, but unprotected controllers may still be reachable by direct URL.
- Result: `Needs execution`
- Evidence: `(add screenshots/status codes)`

### TC-022 (HR access)
- Expected (Policy): HR can access HR/admin operational pages except admin-only pages.
- Actual (Current Build): `[Authorize(Roles="Admin,HR")]` exists on many Shift/Payroll actions.
- Result: `Needs execution`
- Evidence: `(add screenshots/status codes)`

## Core Security Checks

### SC-01 Unauthenticated route protection
- Steps: Access protected routes directly without login.
- Expected: Redirect to `/Account/Login` or 401/403.
- Actual: Mixed; some controllers lack `[Authorize]`.
- Result: `Potential FAIL`
- Evidence: `(add route-by-route results)`

### SC-02 Hidden-link bypass (direct URL)
- Steps: Login as Employee, then manually visit admin URLs.
- Expected: 403/redirect.
- Actual: Risk present for unprotected controllers (`RoleController`, `ShiftAssignmentController`, parts of `AccountController`).
- Result: `Potential FAIL`
- Evidence: `(add route + response)`

### SC-03 Logout invalidates session
- Steps: Login, open protected page, logout, retry protected URL.
- Expected: Access blocked; redirected to login.
- Actual: Logout code signs out cookie and clears session.
- Result: `Likely PASS`
- Evidence: `(add screenshot/log)`

## Findings (from static analysis of current code)

| ID | Severity | Area | Description | Evidence | Recommended Fix | Status |
|---|---|---|---|---|---|---|
| SEC-001 | High | Authorization | `RoleController` has no `[Authorize]`; routes may be publicly accessible | `HRMgmt/Controllers/RoleController.cs` | Add `[Authorize(Roles="Admin")]` at controller/action level | Open |
| SEC-002 | High | Authorization | `ShiftAssignmentController` has no `[Authorize]`; CRUD may be exposed | `HRMgmt/Controllers/ShiftAssignmentController.cs` | Add `[Authorize(Roles="Admin,HR")]` | Open |
| SEC-003 | High | Authorization | `AccountController` CRUD endpoints not role-protected | `HRMgmt/Controllers/AccountController.cs` (`Index/Create/Edit/Delete`) | Restrict with `[Authorize(Roles="Admin")]` (or per policy) | Open |
| SEC-004 | Medium | RBAC Consistency | Manager role exists in UI but not consistently in backend role attributes | `Views/Account/Create.cshtml`, role attributes in controllers | Define Manager policy and enforce consistently | Open |
| SEC-005 | Medium | Access Control Design | UI role-based menu exists, but UI-only hiding is insufficient without backend checks | `Views/Shared/_Layout.cshtml` + unprotected controllers | Ensure all sensitive routes are server-authorized | Open |

## Change Log
- v1 (2026-02-19 13:32:37 -08:00): Initial baseline from static analysis on `test/aira-security-v1`.
