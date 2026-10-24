# Phase 23 deployment configuration

## Secrets and local development

The base settings and EF design-time factories contain no database password or JWT key. Supply development values through process environment variables or an untracked local secret store. PowerShell session variables are one option:

```powershell
$env:ConnectionStrings__DefaultConnection = '<your local SQL Server connection string>'
$env:Authentication__Jwt__SecretKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project src/Host/IranJob.Api/IranJob.Api.csproj
```

The development settings keep local migrations enabled and allow only `http://localhost:4200`. Testing uses a separate test signing key and does not apply migrations on startup.

## Production environment variables

Supply settings from the deployment secret manager. Do not put values in source, image build arguments, or Docker labels.

Required for every Production start:

- `ConnectionStrings__DefaultConnection`
- `Authentication__Jwt__SecretKey` (at least 32 UTF-8 bytes; not a development or test placeholder)
- `Cors__AllowedOrigins__0` and further indexed entries for each exact HTTPS browser origin
- `Payment__PublishFeeAmount` (explicitly set `0` only when paid publishing is intentionally disabled)
- `Database__ApplyMigrationsOnStartup=false`

For paid publishing (`Payment__PublishFeeAmount` greater than zero), also supply:

- `Payment__PublishFeeCurrency`
- `Payment__ZarinPal__MerchantId`
- `Payment__ZarinPal__CallbackUrl`, an HTTPS URL for the Angular route `/employer/payments/return`
- `Payment__ZarinPal__Sandbox`, explicitly set to `true` for sandbox or `false` for live gateway traffic

The provider client selects both request and verification endpoints from `Sandbox`. In Docker Compose, optional endpoint overrides are `ZARINPAL_SANDBOX_REQUEST_URL`, `ZARINPAL_SANDBOX_VERIFY_URL`, `ZARINPAL_REQUEST_URL`, `ZARINPAL_VERIFY_URL`, `ZARINPAL_SANDBOX_START_PAY_URL`, and `ZARINPAL_START_PAY_URL`; each selected URL must use HTTPS. The corresponding API configuration keys are `Payment__ZarinPal__SandboxRequestUrl`, `Payment__ZarinPal__SandboxVerifyUrl`, `Payment__ZarinPal__RequestUrl`, and `Payment__ZarinPal__VerifyUrl`. The default sandbox mode uses sandbox request and verification endpoints, while live mode uses live endpoints.

The API adds a payment identifier to the configured callback URL. The callback page sends the provider authority and status to the API; the API verifies the transaction with ZarinPal. A browser query parameter alone never marks a payment paid.

The production Angular build uses same-origin relative API paths. The supplied Nginx configuration proxies `/api/` to the `api` service in Docker Compose. For another topology, configure the web server or reverse proxy to route `/api/` to the API. If serving the browser application from a different origin, that exact HTTPS origin must also be in `Cors__AllowedOrigins__N`.

The Docker Compose service requires `MSSQL_SA_PASSWORD`, `IRANJOB_DB_CONNECTION_STRING`, `IRANJOB_JWT_SIGNING_KEY`, `PAYMENT_PUBLISH_FEE_AMOUNT`, `ZARINPAL_SANDBOX`, and `CORS_ALLOWED_ORIGIN` from the shell or a local untracked `.env` file. Compose defaults to `ASPNETCORE_ENVIRONMENT=Production` and `DATABASE_APPLY_MIGRATIONS_ON_STARTUP=false`; its production CORS origin has no localhost fallback. For local Compose development, explicitly set `ASPNETCORE_ENVIRONMENT=Development`, `DATABASE_APPLY_MIGRATIONS_ON_STARTUP=true`, and `CORS_ALLOWED_ORIGIN=http://localhost:4200` in the local environment. Never reuse that Development environment configuration for deployment. For Production, provide the exact HTTPS browser origin and the payment settings required by the selected fee.

## Credential rotation

The previously embedded database credential must be treated as exposed. Rotation was not performed by this code change. An authorized database operator should:

1. Create or select a replacement SQL login and grant only the application’s required database permissions.
2. Store the replacement connection string in the deployment secret manager and update the application’s `ConnectionStrings__DefaultConnection` value.
3. Deploy to a non-production or isolated environment and verify API readiness and database-backed workflows using the replacement credential.
4. Roll the replacement through production instances, confirm readiness, then revoke the old SQL credential.
5. Review access logs and repository access history as appropriate. Removing a value from current files does not remove it from prior Git history; history rewriting is a separate authorized operation.

## JWT signing-key rollout

Replacing the JWT HMAC key invalidates access tokens signed by the old key. Coordinate a deployment boundary, rotate the refresh-token/session state if required by the chosen rollout, and require users to authenticate again if the API cannot validate both keys during a controlled overlap. The application currently has one configured signing key and no multi-key overlap mechanism.

## Controlled migrations

Production API startup refuses `Database__ApplyMigrationsOnStartup=true`. Apply each module’s checked-in EF migrations as a deployment step against the intended database before starting or rolling out API instances. Run the commands from the repository root, with `ConnectionStrings__DefaultConnection` supplied in the migration process environment:

```powershell
dotnet ef database update --project src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/IranJob.BuildingBlocks.Infrastructure.csproj --startup-project src/BuildingBlocks/IranJob.BuildingBlocks.Infrastructure/IranJob.BuildingBlocks.Infrastructure.csproj --context ApplicationDbContext
dotnet ef database update --project src/Modules/Identity/IranJob.Modules.Identity.Infrastructure/IranJob.Modules.Identity.Infrastructure.csproj --startup-project src/Modules/Identity/IranJob.Modules.Identity.Infrastructure/IranJob.Modules.Identity.Infrastructure.csproj --context IdentityDbContext
dotnet ef database update --project src/Modules/Candidates/IranJob.Modules.Candidates.Infrastructure/IranJob.Modules.Candidates.Infrastructure.csproj --startup-project src/Modules/Candidates/IranJob.Modules.Candidates.Infrastructure/IranJob.Modules.Candidates.Infrastructure.csproj --context CandidateDbContext
dotnet ef database update --project src/Modules/EmployerProfile/IranJob.Modules.EmployerProfile.Infrastructure/IranJob.Modules.EmployerProfile.Infrastructure.csproj --startup-project src/Modules/EmployerProfile/IranJob.Modules.EmployerProfile.Infrastructure/IranJob.Modules.EmployerProfile.Infrastructure.csproj --context EmployerDbContext
dotnet ef database update --project src/Modules/ReferenceData/IranJob.Modules.ReferenceData.Infrastructure/IranJob.Modules.ReferenceData.Infrastructure.csproj --startup-project src/Modules/ReferenceData/IranJob.Modules.ReferenceData.Infrastructure/IranJob.Modules.ReferenceData.Infrastructure.csproj --context ReferenceDataDbContext
dotnet ef database update --project src/Modules/JobPostings/IranJob.Modules.JobPostings.Infrastructure/IranJob.Modules.JobPostings.Infrastructure.csproj --startup-project src/Modules/JobPostings/IranJob.Modules.JobPostings.Infrastructure/IranJob.Modules.JobPostings.Infrastructure.csproj --context JobPostingDbContext
dotnet ef database update --project src/Modules/Payments/IranJob.Modules.Payments.Infrastructure/IranJob.Modules.Payments.Infrastructure.csproj --startup-project src/Modules/Payments/IranJob.Modules.Payments.Infrastructure/IranJob.Modules.Payments.Infrastructure.csproj --context PaymentDbContext
dotnet ef database update --project src/Modules/Notifications/IranJob.Modules.Notifications.Infrastructure/IranJob.Modules.Notifications.Infrastructure.csproj --startup-project src/Modules/Notifications/IranJob.Modules.Notifications.Infrastructure/IranJob.Modules.Notifications.Infrastructure.csproj --context NotificationDbContext
dotnet ef database update --project src/Modules/Messages/IranJob.Modules.Messages.Infrastructure/IranJob.Modules.Messages.Infrastructure.csproj --startup-project src/Modules/Messages/IranJob.Modules.Messages.Infrastructure/IranJob.Modules.Messages.Infrastructure.csproj --context MessagesDbContext
```

Review generated SQL for the target environment before execution. This audit/implementation did not connect to or migrate any database.

### Payments migration target checks

`20261024054224_InitialPayments` creates the `payments` schema when absent, the Payments migration-history table when absent, the `payments.Payments` table, and its authority/employer/job indexes. The generated idempotent script checks migration history; it does not reconcile an existing table whose schema or history differs. Before applying it, the database operator should:

1. Take or confirm a restorable backup and run the script against a staging copy of the target database first.
2. Inspect whether schema `payments`, table `payments.Payments`, and table `payments.__EFMigrationsHistory` already exist. If the table exists without the `20261024054224_InitialPayments` history row, stop and compare every column, constraint, and index with the migration. Do not rerun the initial create-table script over an existing table or insert a history row without an approved schema reconciliation.
3. If the Payments table exists, check for duplicate non-empty `Authority` values before creating its unique filtered index. Confirm the stored amount/currency conventions and payment history are compatible with the configured fee.
4. Confirm the deployment principal can create the schema/table/index and write the Payments migration-history row. Review the generated SQL and the migration's `Down` operation; do not run rollback SQL as part of deployment.
5. After the controlled apply, verify the migration-history row, table columns, filtered unique authority index, and employer/job lookup indexes before starting API instances with startup migrations disabled.

Generate a review script without connecting to the database:

```powershell
dotnet ef migrations script --idempotent --project src/Modules/Payments/IranJob.Modules.Payments.Infrastructure/IranJob.Modules.Payments.Infrastructure.csproj --startup-project src/Modules/Payments/IranJob.Modules.Payments.Infrastructure/IranJob.Modules.Payments.Infrastructure.csproj --context PaymentDbContext --output payments-migrations.sql
```

The Payments schema intentionally has no database foreign keys to EmployerProfile or JobPosting tables owned by other module contexts. The application resolves the authenticated employer and owns the job check before it creates a publish-fee payment. The database operator should confirm this matches the target deployment's cross-module integrity policy.

### ZarinPal non-production release check

Only use an approved non-production environment with a sandbox merchant configuration. Confirm the merchant account's current currency and amount-unit requirements before choosing `Payment:PublishFeeCurrency` and `Payment:PublishFeeAmount`; do not infer the unit from the display label. Use the approved HTTPS callback URL and explicitly enable sandbox mode. Run one test job through initiation, successful provider verification, cancellation, provider-declared failure, and repeated callback. Verify the persisted payment amount/currency, that only the initiated job is published on verified success, and that cancellation/failure do not publish or create duplicate effects. Never use live credentials or a live payment for this check. No sandbox credentials or provider calls were used during Phase 25.
