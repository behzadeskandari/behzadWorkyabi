# IranJob

IranJob is a modular-monolith recruitment platform for the Iranian market. Phase 0 establishes the technical foundation: backend API, frontend shell, database infrastructure, testing, and local development tooling. Phase 1 adds the Identity & Authentication foundation (ASP.NET Core Identity, JWT, refresh tokens, roles, authorization, and the Angular auth experience).

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0.x |
| Node.js | 20+ (verified with 24.x) |
| Angular CLI | 19+ |
| Docker Desktop | Latest (for SQL Server container) |
| SQL Server | 2022 (via Docker or local instance) |

## Repository layout

```
src/
  BuildingBlocks/     Shared kernel and cross-cutting infrastructure
  Modules/
    Identity/         Identity & authentication module (Phase 1)
  Host/IranJob.Api/   ASP.NET Core Web API host
Tests/
  IranJob.UnitTests/
  IranJob.IntegrationTests/
frontend/iranjob-web/ Angular frontend
docker/               Docker build assets
docs/                 Architecture and phase reports
```

## Quick start

### 1. Start SQL Server

```bash
docker compose up -d sqlserver
```

### 2. Run the backend

```bash
dotnet restore
dotnet build
dotnet run --project src/Host/IranJob.Api/IranJob.Api.csproj
```

- Health: http://localhost:5158/health
- Health (database ready): http://localhost:5158/health/ready
- System info: http://localhost:5158/api/v1/system/info
- Auth: http://localhost:5158/swagger (or the `/api/v1/auth/*` endpoints)
- Swagger: http://localhost:5158/swagger

### 3. Run the frontend

```bash
cd frontend/iranjob-web
npm install --registry https://registry.npmjs.org
npm start
```

Open http://localhost:4200

### 4. Run tests

```bash
dotnet test
cd frontend/iranjob-web
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Documentation

- [ARCHITECTURE.md](./ARCHITECTURE.md)
- [DEVELOPMENT.md](./DEVELOPMENT.md)
- [Authentication architecture](./docs/architecture/AUTHENTICATION.md)
- [Phase 0 Report](./docs/phases/PHASE-0-REPORT.md)
- [Phase 1 Report](./docs/phases/PHASE-1-REPORT.md)

## Phase 1 scope

Phase 1 adds the Identity & Authentication foundation only:

- User registration (Candidate / Employer), login (email or phone), `/auth/me`
- JWT access tokens and rotating, HTTP-only-cookie refresh tokens
- Role-based authorization (`Candidate`, `Employer`, `Recruiter`, `Admin`, `SuperAdmin`)
- Account lockout, rate limiting on auth endpoints, and authentication audit
- Angular login, registration, account, guards, and an HTTP auth interceptor

Business modules (candidates, companies, jobs, payments, etc.) are intentionally excluded and remain
for future phases.
