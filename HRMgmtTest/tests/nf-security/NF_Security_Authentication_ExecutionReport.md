# NF-Security Authentication Test Execution Report

## Build Info
- Branch: `test/aira-security-v1`
- Commit SHA: `1877f45`
- Executed At: `2026-02-20 09:03:59 -08:00`
- Focus: Authentication coverage (TC024-TC030)
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
- `TC024_ValidLoginPerRoleTests`
- `TC025_InvalidCredentialsRejectedTests`
- `TC026_EmptyCredentialsRejectedTests`
- `TC027_LogoutInvalidatesSessionTests`
- `TC028_DirectProtectedRouteRedirectTests`
- `TC029_SessionTimeoutInactivityTests`
- `TC030_PasswordPolicyChecksTests`

## Execution Command
`dotnet test HRMgmtTest --filter "Name~TC024|Name~TC025|Name~TC026|Name~TC027|Name~TC028|Name~TC029|Name~TC030" -v minimal`

## Summary
- Total: 7
- Passed: 5
- Failed: 2
- Skipped: 0

## Result by Test Case
- `TC024` Valid login per role: **PASS**
- `TC025` Invalid username/password rejected: **PASS**
- `TC026` Empty credentials rejected: **PASS**
- `TC027` Logout invalidates session: **PASS**
- `TC028` Direct protected-route access redirects to login: **FAIL**
  - Finding: unauthenticated access to `/Account/Index` is allowed.
- `TC029` Session timeout/inactivity behavior (cookie invalidation simulation): **PASS**
- `TC030` Password policy checks: **FAIL**
  - Finding: weak password (`abcdef`) is accepted and account is created.

## Findings
- `AccountController.Index` is not protected by `[Authorize]` and is reachable without login.
- Password policy for account creation currently enforces only minimum length (`>= 6`), not complexity.

## Notes
- Tests are designed to expose auth/security gaps; failures are valid QA findings.
- `TC026` treats "stays on login page after submit" as rejection, even if no banner text is shown.
- `TC029` uses auth-cookie invalidation simulation as inactivity/session-loss check.
- `TC030` is requirements-level: expects stronger password complexity than current implementation.
