# Phase 0 Report — IranJob

**Date:** 2026-08-13  
**Status:** Complete  
**Scope:** Infrastructure foundation only (no business modules)

---

## Summary

Phase 0 delivers a runnable, testable foundation for IranJob:

- .NET 8 modular monolith backend with Clean Architecture building blocks
- Angular 19 RTL/Persian frontend shell
- SQL Server + EF Core infrastructure (empty schema)
- Swagger, Serilog, health checks, ProblemDetails, global exception handling
- Unit and integration test suites (all passing)
- Docker Compose for SQL Server (+ optional full stack profile)
- Project documentation

> **Note:** No Master Specification file was present in the repository at implementation time. Phase 0 was implemented from the Phase 0 prompt requirements.

---

## Files Created

### Solution & documentation

| File |
|------|
| `IranJob.sln` |
| `README.md` |
| `ARCHITECTURE.md` |
| `DEVELOPMENT.md` |
| `.gitignore` |
| `.dockerignore` |
| `docker-compose.yml` |

### Docker

| File |
|------|
| `docker/api/Dockerfile` |
| `docker/web/Dockerfile` |
| `docker/web/nginx.conf` |

### Backend — Shared Kernel

| File |
|------|
| `src/BuildingBlocks/IranJob.SharedKernel/IranJob.SharedKernel.csproj` |
| `src/BuildingBlocks/IranJob.SharedKernel/Entities/Entity.cs` |
| `src/BuildingBlocks/IranJob.SharedKernel/Results/Error.cs` |
| `src/BuildingBlocks/IranJob.SharedKernel/Results/Result.cs` |
| `src/BuildingBlocks/IranJob.SharedKernel/Guard.cs` |
| `src/BuildingBlocks/IranJob.SharedKernel/Exceptions/DomainException.cs` |
| `src/BuildingBlocks/IranJob.SharedKernel/Exceptions/NotFoundException.cs` |
| `src/BuildingBlocks/IranJob.SharedKernel/Exceptions/ValidationException.cs` |

### Backend — Infrastructure

| File |
|------|
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/IranJob.BuildingBlocks.Infrastructure.csproj` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Configuration/ApplicationOptions.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Configuration/DatabaseOptions.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Persistence/ApplicationDbContext.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Persistence/ApplicationDbContextFactory.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Persistence/Migrations/20260813063704_InitialInfrastructure.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Persistence/Migrations/20260813063704_InitialInfrastructure.Designer.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Logging/CorrelationIdConstants.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Logging/SensitiveDataDestructuringPolicy.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Middleware/CorrelationIdMiddleware.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Middleware/RequestLoggingMiddleware.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Middleware/GlobalExceptionHandler.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Extensions/ServiceCollectionExtensions.cs` |
| `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Extensions/WebApplicationExtensions.cs` |

### Backend — Host API

| File |
|------|
| `src/Host/IranJob.Api/IranJob.Api.csproj` |
| `src/Host/IranJob.Api/Program.cs` |
| `src/Host/IranJob.Api/appsettings.json` |
| `src/Host/IranJob.Api/appsettings.Development.json` |
| `src/Host/IranJob.Api/appsettings.Testing.json` |
| `src/Host/IranJob.Api/Properties/launchSettings.json` |
| `src/Host/IranJob.Api/Controllers/SystemController.cs` |
| `src/Host/IranJob.Api/Controllers/DiagnosticsController.cs` |
| `src/Host/IranJob.Api/Models/SystemInfoResponse.cs` |
| `src/Host/IranJob.Api/Services/SystemInfoService.cs` |
| `src/Host/IranJob.Api/IranJob.Api.http` |

### Modules placeholder

| File |
|------|
| `src/Modules/README.md` |

### Tests

| File |
|------|
| `Tests/IranJob.UnitTests/IranJob.UnitTests.csproj` |
| `Tests/IranJob.UnitTests/GlobalUsings.cs` |
| `Tests/IranJob.UnitTests/SharedKernel/GuardTests.cs` |
| `Tests/IranJob.IntegrationTests/IranJob.IntegrationTests.csproj` |
| `Tests/IranJob.IntegrationTests/GlobalUsings.cs` |
| `Tests/IranJob.IntegrationTests/CustomWebApplicationFactory.cs` |
| `Tests/IranJob.IntegrationTests/ApiStartupTests.cs` |

### Frontend (`frontend/iranjob-web`)

| File |
|------|
| `frontend/iranjob-web/package.json` |
| `frontend/iranjob-web/package-lock.json` |
| `frontend/iranjob-web/angular.json` |
| `frontend/iranjob-web/tsconfig.json` |
| `frontend/iranjob-web/tsconfig.app.json` |
| `frontend/iranjob-web/tsconfig.spec.json` |
| `frontend/iranjob-web/.npmrc` |
| `frontend/iranjob-web/.editorconfig` |
| `frontend/iranjob-web/.gitignore` |
| `frontend/iranjob-web/README.md` |
| `frontend/iranjob-web/public/favicon.ico` |
| `frontend/iranjob-web/src/index.html` |
| `frontend/iranjob-web/src/main.ts` |
| `frontend/iranjob-web/src/styles.scss` |
| `frontend/iranjob-web/src/environments/environment.ts` |
| `frontend/iranjob-web/src/environments/environment.development.ts` |
| `frontend/iranjob-web/src/app/app.component.ts` |
| `frontend/iranjob-web/src/app/app.component.spec.ts` |
| `frontend/iranjob-web/src/app/app.config.ts` |
| `frontend/iranjob-web/src/app/app.routes.ts` |
| `frontend/iranjob-web/src/app/core/interceptors/correlation.interceptor.ts` |
| `frontend/iranjob-web/src/app/core/interceptors/error.interceptor.ts` |
| `frontend/iranjob-web/src/app/core/services/error-handler.service.ts` |
| `frontend/iranjob-web/src/app/core/services/system-info.service.ts` |
| `frontend/iranjob-web/src/app/layout/header/header.component.ts` |
| `frontend/iranjob-web/src/app/layout/header/header.component.html` |
| `frontend/iranjob-web/src/app/layout/header/header.component.scss` |
| `frontend/iranjob-web/src/app/layout/footer/footer.component.ts` |
| `frontend/iranjob-web/src/app/layout/footer/footer.component.html` |
| `frontend/iranjob-web/src/app/layout/footer/footer.component.scss` |
| `frontend/iranjob-web/src/app/layout/main-layout/main-layout.component.ts` |
| `frontend/iranjob-web/src/app/layout/main-layout/main-layout.component.html` |
| `frontend/iranjob-web/src/app/layout/main-layout/main-layout.component.scss` |
| `frontend/iranjob-web/src/app/pages/home/home.component.ts` |
| `frontend/iranjob-web/src/app/pages/home/home.component.html` |
| `frontend/iranjob-web/src/app/pages/home/home.component.scss` |
| `frontend/iranjob-web/src/app/pages/home/home.component.spec.ts` |
| `frontend/iranjob-web/.vscode/extensions.json` |
| `frontend/iranjob-web/.vscode/launch.json` |
| `frontend/iranjob-web/.vscode/tasks.json` |

---

## Files Modified

The workspace was empty at the start of Phase 0. All files listed above were **created**; no pre-existing project files were modified.

Template-generated files removed during setup:

- `src/BuildingBlocks/IranJob.SharedKernel/Class1.cs` (deleted)
- `src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/Class1.cs` (deleted)
- `src/Host/IranJob.Api/WeatherForecast.cs` (deleted)
- `src/Host/IranJob.Api/Controllers/WeatherForecastController.cs` (deleted)
- `Tests/IranJob.UnitTests/UnitTest1.cs` (deleted)
- `Tests/IranJob.IntegrationTests/UnitTest1.cs` (deleted)
- `frontend/iranjob-web/src/app/app.component.html` (deleted, replaced by inline template)
- `frontend/iranjob-web/src/app/app.component.scss` (deleted)

---

## Commands Executed

### Backend

```bash
dotnet new sln -n IranJob
dotnet new classlib -n IranJob.SharedKernel -o src/BuildingBlocks/IranJob.SharedKernel -f net8.0
dotnet new classlib -n IranJob.BuildingBlocks.Infrastructure -o src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure -f net8.0
dotnet new webapi -n IranJob.Api -o src/Host/IranJob.Api -f net8.0 --use-controllers
dotnet new xunit -n IranJob.UnitTests -o Tests/IranJob.UnitTests -f net8.0
dotnet new xunit -n IranJob.IntegrationTests -o Tests/IranJob.IntegrationTests -f net8.0
dotnet sln add ...
dotnet add ... reference ...
dotnet add ... package ... (EF Core, Serilog, FluentValidation, Asp.Versioning, Swashbuckle, Mvc.Testing, FluentAssertions, EF Sqlite)
dotnet ef migrations add InitialInfrastructure --project src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure --startup-project src/Host/IranJob.Api --output-dir Persistence/Migrations
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Host/IranJob.Api/IranJob.Api.csproj
```

### Frontend

```bash
npx -y @angular/cli@19 new iranjob-web --directory frontend/iranjob-web --routing --style=scss --ssr=false --skip-git --defaults
npm install --registry https://registry.npmjs.org
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npm start
```

### Docker (attempted)

```bash
docker compose up -d sqlserver
```

### Verification

```powershell
Invoke-WebRequest http://localhost:5158/health
Invoke-RestMethod http://localhost:5158/api/v1/system/info
Invoke-WebRequest http://localhost:5158/swagger/index.html
Invoke-WebRequest http://localhost:4200
```

---

## Test Results

### .NET (`dotnet test`)

```
Passed!  IranJob.UnitTests        — 4 passed, 0 failed
Passed!  IranJob.IntegrationTests — 6 passed, 0 failed
Total: 10 passed, 0 failed
```

**Integration test coverage:**

- API startup
- `GET /health`
- `GET /api/v1/system/info`
- Correlation ID response header
- Global exception handling (500 ProblemDetails)
- NotFound exception handling (404 ProblemDetails)

### Angular

```bash
npm run build   → SUCCESS
npm test -- --watch=false --browsers=ChromeHeadless → 3 passed, 0 failed
```

---

## Runtime Verification

| Check | URL | Result |
|-------|-----|--------|
| Health (liveness) | http://localhost:5158/health | `200 OK` |
| System info | http://localhost:5158/api/v1/system/info | `200 OK` — `{ applicationName: "IranJob", version: "0.1.0", environment: "Development" }` |
| Swagger UI | http://localhost:5158/swagger | `200 OK` |
| Angular dev server | http://localhost:4200 | `200 OK` — RTL shell (`lang="fa" dir="rtl"`) |
| Homepage content | Component unit tests | Displays **ایران‌جاب** and welcome message |

### Additional endpoint

| Endpoint | Purpose |
|----------|---------|
| `GET /health/ready` | Database readiness (returns `503` when SQL Server is unavailable) |

---

## Technology Versions

| Component | Version |
|-----------|---------|
| .NET SDK | 8.0.302 |
| ASP.NET Core | 8.0 |
| Entity Framework Core | 8.0.11 |
| Angular | 19.1.x |
| Node.js | 24.11.1 |
| npm | 11.6.2 |
| SQL Server (Docker image) | 2022-latest |

---

## Known Limitations

1. **Master Specification missing** — No master spec file was found in the repository; implementation followed the Phase 0 prompt only.

2. **Docker Desktop unavailable during verification** — `docker compose up -d sqlserver` failed because Docker Desktop was not running. Start Docker before using the SQL Server container.

3. **Local SQL Server credential mismatch** — A local SQL Server instance on port 1433 rejected the default `sa` password. The API still starts in Development (migrations log a warning). Use Docker Compose SQL Server or update the connection string.

4. **Database readiness** — `GET /health/ready` requires a working SQL Server connection. `GET /health` is a fast liveness check and does not depend on the database.

5. **npm mirror** — Initial `npm install` failed against a private mirror (`402 Payment Required`). Resolved by using `registry.npmjs.org` via `.npmrc` or `--registry`.

6. **No business modules** — Candidates, companies, jobs, payments, and other domain features are intentionally excluded.

7. **Diagnostic endpoints** — `/api/v1/diagnostics/*` exist for testing only and should be removed or secured before production.

8. **Frontend API integration** — Home page displays static Persian content; live system-info fetch from the API is wired via `SystemInfoService` but not yet shown on the homepage UI.

---

## Docker Decision

| Service | Containerized? | Recommendation |
|---------|----------------|----------------|
| SQL Server | Yes (`docker compose up -d sqlserver`) | Use Docker |
| Backend API | Optional (`--profile full`) | Local `dotnet run` for development |
| Frontend | Optional (`--profile full`) | Local `ng serve` for development |

Local development without containerizing backend/frontend provides faster iteration, simpler debugging, and hot reload.

---

## Phase 0 Deliverables Checklist

- [x] .NET 8 ASP.NET Core Web API
- [x] Clean Architecture foundation
- [x] Modular Monolith structure
- [x] Angular 19 frontend (RTL, Persian-ready)
- [x] SQL Server + EF Core configuration
- [x] Swagger
- [x] Health checks
- [x] Serilog with correlation ID
- [x] Global exception handling + ProblemDetails
- [x] Configuration infrastructure
- [x] Shared kernel
- [x] Testing infrastructure
- [x] Docker Compose
- [x] Documentation (README, ARCHITECTURE, DEVELOPMENT, this report)
- [x] All tests passing
- [x] Endpoints verified

**Phase 0 is complete. Ready for Phase 1 business module implementation.**
