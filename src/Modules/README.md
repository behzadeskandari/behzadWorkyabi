# Modules

This folder contains the bounded-context modules of the IranJob modular monolith.

Each module follows Clean Architecture layers:

- Domain
- Application
- Infrastructure
- Presentation (module endpoints)

Existing modules:

- **Identity** — users, authentication, roles, refresh tokens.
- **Candidates** — candidate professional profiles (one profile per user, linked by `UserId`).
