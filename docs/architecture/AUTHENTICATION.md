# Authentication & Authorization Architecture

This document describes the Phase 1 identity, authentication, and authorization design for IranJob.
It is the authoritative reference for the token-storage strategy, cookie/CSRF decisions, rate limiting,
lockout, audit, and database schema used by the `Identity` module.

---

## 1. Overview

IranJob uses ASP.NET Core Identity (with an application user extending `IdentityUser<Guid>`),
JWT access tokens, and rotating, persisted refresh tokens delivered through secure HTTP-only cookies.

```
Registration → Authentication → JWT Access Token + Refresh Token → Authorization → Protected Resources
```

Supported roles: `Candidate`, `Employer`, `Recruiter`, `Admin`, `SuperAdmin`.

The Identity module lives under `src/Modules/Identity/` and follows Clean Architecture:

| Project | Responsibility |
|---------|----------------|
| `IranJob.Modules.Identity.Domain` | Entities (`ApplicationUser`, `ApplicationRole`, `RefreshToken`, `AuthenticationAuditEntry`), role/permission constants. Domain depends only on ASP.NET Identity primitive types (`IdentityUser<Guid>`, `IdentityRole<Guid>`), not on EF or the host. |
| `IranJob.Modules.Identity.Application` | Abstractions (`IAuthService`, `IJwtTokenService`, `IRefreshTokenService`, `IAuthenticationAuditService`, `IUserLookupService`, `IRequestContext`) and FluentValidation validators. No dependency on Angular or on the Web host. |
| `IranJob.Modules.Identity.Infrastructure` | EF Core + SQL Server, ASP.NET Identity stores, JWT signing, refresh-token persistence/rotation, request context, audit, cookie/CSRF helpers, DI extensions. |
| `IranJob.Modules.Identity.Presentation` | Controllers (`AuthController`, `AdminController`) and DTO contracts. Controllers contain no business logic. |

Dependency rule: Domain → nothing; Application → Domain; Infrastructure → Application + Domain; Presentation → Application + Infrastructure.

---

## 2. User Model

`ApplicationUser` extends `IdentityUser<Guid>` and adds only account/identity fields:

| Property | Notes |
|----------|-------|
| `Id`, `UserName`, `Email`, `PhoneNumber` | Inherited, `Guid` key |
| `FirstName`, `LastName` | Required, max 100 |
| `IsActive` | Default `true`; inactive users cannot log in |
| `CreatedAt`, `UpdatedAt` | `DateTimeOffset` |
| `LastLoginAt` | `DateTimeOffset?`, updated only after successful authentication |

> No candidate/employer/company/job fields are placed on the user. Those belong to future modules.

---

## 3. Roles & Registration

Roles are seeded idempotently on startup (`IdentitySeedData`) using `RoleManager`. Public
registration (`POST /api/v1/auth/register`) accepts **only** `Candidate` and `Employer`.
`Recruiter`, `Admin`, and `SuperAdmin` cannot be self-assigned; they are assigned through
administrative mechanisms in a later phase.

---

## 4. JWT Access Tokens

- Issued by `JwtTokenService` with `HMACSHA256` symmetric signing.
- Claims: `sub` (user id), `jti`, `nameid`, `role`(s), `iss`, `aud`, `nbf`, `exp`.
- No email/name/phone in the token (only the minimum identifiers).
- Short-lived; default `15` minutes via `Authentication:Jwt:AccessTokenExpirationMinutes`.
- Configuration keys: `Authentication:Jwt:{Issuer, Audience, SecretKey, AccessTokenExpirationMinutes}`.
- Secrets are never hard-coded; `appsettings.json` carries a development-only placeholder and real
  signing keys are provided via User Secrets or environment variables.

The JWT bearer scheme is explicitly registered as the default authenticate, challenge, forbid,
sign-in, sign-out and default scheme so unqualified `[Authorize]` attributes resolve to the bearer
---

## 5. Refresh Tokens

- `RefreshToken` entity stores `TokenHash` only — never the raw token. `SHA256` is used.
- Raw tokens are 64 random bytes (Base64), generated with `RandomNumberGenerator`.
- Fields: `Id`, `UserId`, `TokenHash`, `CreatedAt`, `ExpiresAt`, `RevokedAt`, `ReplacedByTokenId`,
  `CreatedByIp`, `RevokedByIp`, `RevocationReason`.
- Default lifetime: 7 days (`Authentication:Identity:RefreshTokenExpirationDays`).

### Rotation & reuse detection

```
Original A ──refresh──▶ A revoked, ReplacedByTokenId=B, B returned
```

If a revoked token is reused (an attacker replaying a rotated/revoked token), the request is treated
as possible theft: the entire token family/descendants are revoked and `RefreshTokenReuseDetected`
is audited. Reusing an expired or revoked token returns `401`.

### Delivery mechanism — decision

Refresh tokens are delivered via **`HttpOnly`, `SameSite=Lax` cookies** scoped to the `/api/v1/auth`
path. The raw refresh token never touches `localStorage`/`sessionStorage` and is not readable by
JavaScript.

Rationale & evaluation:

- **XSS**: `HttpOnly` prevents JavaScript from reading the refresh token. The access token is held
  only in memory (signals) and is short-lived, limiting the XSS blast radius.
- **CSRF**: The state-changing auth endpoints (`/refresh`, `/logout`) validate a separate
  double-submit CSRF token: a non-`HttpOnly` `iranjob_csrf` cookie plus the `X-CSRF-TOKEN` header,
  compared with constant-time comparison. `SameSite=Lax` provides a second line of defense.
- **CORS**: The local frontend origin (`http://localhost:4200`) is allowed with
  `AllowCredentials()`; the `X-CSRF-TOKEN` header is exposed.
- **Development**: `Secure` is disabled for Development/Testing so the cookie works over plain HTTP.
- **Production**: `Secure` cookies are enforced; production must use HTTPS.

Cookies are set with `Path=/api/v1/auth` so the refresh-token cookie is only sent to auth endpoints.

---

## 6. API surface

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/v1/auth/register` | Anonymous | Register `Candidate`/`Employer` |
| POST | `/api/v1/auth/login` | Anonymous | Login by email or phone; sets refresh cookie + CSRF cookie |
| POST | `/api/v1/auth/refresh` | Anonymous + CSRF | Rotate refresh token, issue new access token |
| POST | `/api/v1/auth/logout` | Anonymous + CSRF | Revoke server-side refresh token, clear cookies |
| GET | `/api/v1/auth/me` | Bearer | Return the current user profile |
| GET | `/api/v1/admin/ping` | `Admin`,`SuperAdmin` | Authorization verification endpoint |

Register/login/refresh are protected by rate limiting (see below).

The login/refresh responses return the access token in the JSON body and the refresh token via the
cookie. Sensitive fields (`PasswordHash`, `SecurityStamp`, refresh tokens, secrets) are never
returned.

---

## 7. Security measures

### Password policy (ASP.NET Identity)

`MinimumLength = 8`, `RequireDigit`, `RequireUppercase`, `RequireLowercase`,
`RequireNonAlphanumeric`. Password hashing is ASP.NET Identity's (no custom hashing).

### Lockout

`MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 minutes` (configurable via
`Authentication:Identity`). Failed attempts are tracked by `SignInManager`.

### Rate limiting

Built-in ASP.NET Core rate limiting protects `register`, `login`, and `refresh` with a fixed-window
policy keyed by client IP (default `PermitLimit = 20` / `WindowSeconds = 60`, overridable via
`Authentication:RateLimiting`). Rejection status is `429`.

> Distributed rate limiting is intentionally not implemented in Phase 1. When the application is
> scaled horizontally, a shared backing store (e.g. Redis) will be required.

### Authentication audit

`AuthenticationAuditEntry` persists security events: `RegistrationSucceeded`, `LoginSucceeded`,
`LoginFailed`, `Logout`, `RefreshSucceeded`, `RefreshFailed`, `RefreshTokenReuseDetected`,
`AccountLocked`. Each record stores `UserId`, `EventType`, `Timestamp`, `CorrelationId`, `IpAddress`,
`UserAgent`, and optional `Metadata`.

> Passwords, password hashes, access tokens, refresh tokens, and the JWT secret are never logged.

### ProblemDetails

Errors use the Phase 0 ProblemDetails / global-exception infrastructure:
`400` (validation), `401` (authentication), `403` (authorization), `409` (duplicate email/phone),
`429` (rate limit), `500` (generic, no stack traces/Db exceptions exposed).

---

## 8. Database schema

All Identity tables live in the `identity` schema:

`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`,
`AspNetUserLogins`, `AspNetUserTokens`, `RefreshTokens`, `AuthenticationAuditEntries`.

Constraints:

- Unique `NormalizedUserName` and `NormalizedEmail` (the registration flow sets `UserName` to the
  normalized email, so the unique user-name index also enforces unique email).
- Unique `PhoneNumber` (with null filter).
- Unique `RefreshTokens.TokenHash` (enables reuse detection).

Migrations are stored in
`src/Modules/Identity/IranJob.Modules.Identity.Infrastructure/Persistence/Migrations/`
(the initial migration `InitialIdentity`). Migrations are applied automatically on startup when
`Database:ApplyMigrationsOnStartup = true`, or manually via `dotnet ef database update`.

The module does **not** own any candidate/company/job/payment tables.

---

## 9. Authorization

Role-based authorization (`[Authorize(Roles = "...")]`) covers the admin endpoint. A reusable
permission-ready foundation exists as constants (`IdentityPermissions`) for future modules, but the
full permission system is intentionally not implemented in Phase 1.

`GET /api/v1/admin/ping` behavior:

| Caller | Result |
|--------|--------|
| Anonymous | 401 |
| Candidate / Employer / Recruiter | 403 |
| Admin / SuperAdmin | 200 |

---

## 10. Configuration

Required `appsettings` keys:

```jsonc
"Authentication": {
  "Jwt": {
    "Issuer": "IranJob",
    "Audience": "IranJob.Client",
    "SecretKey": "DEVELOPMENT_ONLY",        // override with user-secret/env var in any real env
    "AccessTokenExpirationMinutes": 15
  },
  "Identity": {
    "MaxFailedAccessAttempts": 5,
    "LockoutMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "RefreshTokenCookieName": "iranjob_refresh_token",
    "CsrfCookieName": "iranjob_csrf",
    "CsrfHeaderName": "X-CSRF-TOKEN"
  },
  "RateLimiting": {
    "PermitLimit": 20,
    "WindowSeconds": 60
  }
}
```

Never commit real signing secrets. Use User Secrets or environment variables for the JWT secret in
real environments.

---

## 11. Migrations

Run from the repository root:

```bash
dotnet ef migrations add <Name> ^
  --project src/Modules/Identity/IranJob.Modules.Identity.Infrastructure/IranJob.Modules.Identity.Infrastructure.csproj ^
  --startup-project src/Host/IranJob.Api/IranJob.Api.csproj ^
  --output-dir Persistence/Migrations
```

```bash
dotnet ef database update ^
  --project src/Modules/Identity/IranJob.Modules.Identity.Infrastructure/IranJob.Modules.Identity.Infrastructure.csproj ^
  --startup-project src/Host/IranJob.Api/IranJob.Api.csproj
```

---

## 12. Testing

- Unit tests: JWT validation, refresh-token rotation/reuse/revocation, validators, role seeding rules.
- Integration tests (`CustomWebApplicationFactory`, SQLite in-memory): register, login, me, refresh,
  logout, admin/role authorization, duplicate handling, lockout, inactive accounts.

---

## 13. Known limitations

- No email verification flow (future phase).
- No "forgot password" (future phase).
- No logout-all-devices/active-session UI yet; the persistence model (per-user refresh tokens with
  `RevokeAllForUserAsync`) supports it without redesign.
- No password change/rotation UI yet.
- Distributed rate limiting requires shared infrastructure under horizontal scaling.