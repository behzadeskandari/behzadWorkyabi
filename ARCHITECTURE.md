# Architecture

## Overview

IranJob follows **Clean Architecture** within a **Modular Monolith** structure. Phase 0 establishes building blocks and the API host without business modules.

## Layers

### Shared Kernel (`IranJob.SharedKernel`)

Cross-cutting domain primitives shared by future modules:

- Base `Entity`
- `Result` / `Error` pattern
- Domain exceptions (`ValidationException`, `NotFoundException`, etc.)
- Guard helpers

### Building Blocks Infrastructure

Technical infrastructure used by the host and future modules:

- EF Core `ApplicationDbContext` (empty schema, `infra` schema reserved)
- Configuration binding (`ApplicationOptions`, `DatabaseOptions`)
- Serilog logging with correlation ID enrichment
- Sensitive data destructuring (passwords, tokens, secrets redacted)
- Global exception handling with RFC 7807 ProblemDetails
- Correlation ID and request logging middleware
- Health checks (database connectivity)

### Host (`IranJob.Api`)

Composition root for Phase 0:

- API versioning (`/api/v1/...`)
- Swagger/OpenAPI
- System info endpoint
- Diagnostic endpoints (testing only)

### Modules

The `src/Modules` folder is reserved for future bounded contexts. Each module should expose:

- Domain
- Application (use cases, validators)
- Infrastructure (persistence, integrations)
- Optional module-specific API surface

## API conventions

| Concern | Approach |
|---------|----------|
| Versioning | URL segment: `/api/v1/...` |
| Errors | ProblemDetails (`application/problem+json`) |
| Correlation | `X-Correlation-ID` request/response header |
| Health | `GET /health` |
| Logging | Serilog structured console output |

## Database

- SQL Server 2022
- EF Core migrations stored in `IranJob.BuildingBlocks.Infrastructure`
- Default schema: `infra`
- No business tables in Phase 0

## Frontend

Angular 19 standalone application with:

- RTL layout and Persian typography (Vazirmatn)
- Environment-based API configuration
- HTTP interceptors (correlation ID, error handling)
- Layout shell (header, footer, home page)

## Testing strategy

| Project | Purpose |
|---------|---------|
| `IranJob.UnitTests` | Shared kernel and pure logic |
| `IranJob.IntegrationTests` | API startup, endpoints, exception handling via `WebApplicationFactory` |

Integration tests use SQLite in-memory to avoid requiring Docker during `dotnet test`.

## Docker strategy

- **SQL Server**: containerized by default (`docker compose up -d sqlserver`)
- **Backend / Frontend**: optional `full` profile for container builds; local `dotnet run` and `ng serve` are recommended for day-to-day development due to faster iteration and simpler debugging

```bash
docker compose up -d sqlserver          # database only (recommended)
docker compose --profile full up -d     # sqlserver + api + web
```
