# Development Guide

## Prerequisites

Install the following before working on IranJob:

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. [Node.js 20+](https://nodejs.org/) and npm
3. [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server)
4. Optional: [Angular CLI 19+](https://angular.dev/tools/cli) (`npm install -g @angular/cli@19`)

Verified versions during Phase 0:

- .NET SDK: `8.0.302`
- Node.js: `24.11.1`
- npm: `11.6.2`
- Angular: `19.1.x`

## SQL Server configuration

Start SQL Server with Docker Compose:

```bash
docker compose up -d sqlserver
```

Default credentials (development only):

| Setting | Value |
|---------|-------|
| Server | `localhost,1433` |
| Database | `IranJob` |
| User | `sa` |
| Password | `Your_strong_Password123` |

## Connection string

Configured in `src/Host/IranJob.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=IranJob;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False"
}
```

Override via environment variable:

```bash
set ConnectionStrings__DefaultConnection=Server=...
```

## Migrations

Create a migration:

```bash
dotnet ef migrations add <Name> ^
  --project src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/IranJob.BuildingBlocks.Infrastructure.csproj ^
  --startup-project src/Host/IranJob.Api/IranJob.Api.csproj ^
  --output-dir Persistence/Migrations
```

Apply migrations manually:

```bash
dotnet ef database update ^
  --project src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/IranJob.BuildingBlocks.Infrastructure.csproj ^
  --startup-project src/Host/IranJob.Api/IranJob.Api.csproj
```

By default, migrations run automatically on startup when `Database:ApplyMigrationsOnStartup` is `true`.

## Running the backend

```bash
dotnet restore
dotnet build
dotnet run --project src/Host/IranJob.Api/IranJob.Api.csproj
```

URLs:

- API base: http://localhost:5158
- Swagger: http://localhost:5158/swagger
- Health: http://localhost:5158/health
- System info: http://localhost:5158/api/v1/system/info

## Running the frontend

```bash
cd frontend/iranjob-web
npm install --registry https://registry.npmjs.org
npm start
```

Open http://localhost:4200

API base URL is configured in:

- `src/environments/environment.development.ts` (local dev)
- `src/environments/environment.ts` (production build)

## Running tests

### Backend

```bash
dotnet test
```

### Frontend

```bash
cd frontend/iranjob-web
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Docker commands

Database only (recommended for local development):

```bash
docker compose up -d sqlserver
docker compose logs -f sqlserver
docker compose down
```

Full stack (optional):

```bash
docker compose --profile full up -d --build
docker compose --profile full down
```

## Local vs containerized app services

| Service | Recommendation |
|---------|----------------|
| SQL Server | Docker container |
| Backend API | Local `dotnet run` (hot reload, easier debugging) |
| Frontend | Local `ng serve` (faster rebuilds) |

Backend and frontend Dockerfiles are provided for CI/CD and optional full-stack container runs via the `full` Compose profile.

## npm registry note

If `npm install` fails against a private mirror, use the public registry:

```bash
npm install --registry https://registry.npmjs.org
```

Or rely on `frontend/iranjob-web/.npmrc`.

## Swagger

Swagger UI is enabled in Development at:

http://localhost:5158/swagger

## Troubleshooting

### Database health check fails

Ensure SQL Server is running and the connection string matches Docker credentials.

### EF migration errors

Verify SQL Server is reachable and the `IranJob` database can be created. Check logs for Serilog output including correlation IDs.

### Angular build fails on npm install

Use `--registry https://registry.npmjs.org` or configure `.npmrc` in `frontend/iranjob-web`.
