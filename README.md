# Nido Backend

This repository documents and enforces backend team conventions for implementing and reviewing .NET Clean Architecture code.

## Purpose of this README

- Define architecture boundaries and coding rules used by the backend team.
- Provide implementation patterns that keep HTTP, application, domain, and infrastructure concerns separated.
- Provide review and testing checklists to preserve behavior while code evolves.

## Clean Architecture split

| Path | Responsibility |
|------|----------------|
| `src/Nido.Api` | HTTP exposure (controllers, contracts, startup wiring) |
| `src/Nido.Application` | Use cases and ports |
| `src/Nido.Domain` | Entities, value objects, pure business rules |
| `src/Nido.Infrastructure` | EF Core, repositories, persistence wiring |
| `tests/` | Domain, Application, and API integration tests |

## Backend coding conventions

This backend uses a pragmatic Clean Architecture approach. The goal is to keep dependency direction clear, make use cases easy to test, and prevent framework details from leaking into business code.

### Layer communication rule

Runtime flow should be:

```txt
HTTP request
  -> Nido.Api controller
    -> Nido.Application use case / handler
      -> repository port
        -> Nido.Infrastructure repository implementation
          -> EF Core / PostgreSQL
```

Compile-time dependency direction:

```txt
Nido.Domain          -> no project dependencies
Nido.Application     -> Nido.Domain
Nido.Infrastructure  -> Nido.Application + Nido.Domain
Nido.Api             -> Nido.Application + Nido.Infrastructure for composition root only
```

Rules for all backend code:

- Controllers must not inject `NidoDbContext`.
- Controllers must not use `Nido.Infrastructure.Persistence.Entities` directly.
- Controllers handle HTTP concerns only: route, request, response, status codes.
- Application owns use-case orchestration and repository ports.
- Infrastructure owns EF Core, persistence entities, migrations, JWT, hashing, and external technical details.
- Domain stays pure and grows only when business rules need a stable model.

### Coding rules by layer

| Layer | Do | Do not |
|------|----|--------|
| `Nido.Api` | Define controllers, routes, HTTP request/response contracts, status-code mapping, and startup wiring. | Do not query EF Core, inject `NidoDbContext`, or reference persistence entities. |
| `Nido.Application` | Define use cases, commands/queries, handlers, results, validations, and repository ports. | Do not reference `Nido.Infrastructure`, EF Core, or database-specific types. |
| `Nido.Domain` | Define pure business concepts, invariants, value objects, and core behavior. | Do not depend on frameworks, persistence, HTTP, or configuration. |
| `Nido.Infrastructure` | Implement repository ports, EF Core mappings, persistence entities, JWT, hashing, and external integrations. | Do not decide business policy that belongs to Application/Domain. |

### Use-case implementation pattern

Use this shape for new API behavior:

```txt
Controller action
  -> Application command/query
  -> Application handler
  -> Application repository port
  -> Infrastructure repository adapter
  -> EF Core / PostgreSQL
```

Recommended conventions:

- Keep controllers thin and boring.
- Prefer explicit request/response DTOs over exposing entities.
- Use one handler per use case when behavior is non-trivial.
- Pass `CancellationToken` through async application and infrastructure calls.
- Use `AsNoTracking()` for read-only EF queries.
- Keep repository ports intention-based, not table-based, when the use case needs behavior rather than raw CRUD.

### Governance checklist for PR reviews

Use this checklist as an explicit gate in reviews:

- [ ] Controller classes in `Nido.Api` do not depend on `Microsoft.EntityFrameworkCore`.
- [ ] Controller classes in `Nido.Api` do not inject `NidoDbContext`.
- [ ] Controller classes in `Nido.Api` do not reference `Nido.Infrastructure.Persistence.Entities`.
- [ ] HTTP contracts (`Request`/`Response`) are mapped in API; use-case contracts live in `Nido.Application`.
- [ ] Persistence logic lives in Infrastructure repository adapters only.
- [ ] New use cases accept and forward `CancellationToken`.
- [ ] Behavior-preserving refactors are protected by tests before and after code movement.

### Migration ownership

- Infrastructure owns migrations and persistence model configuration.
- Production migration execution path is `Nido.Migrator`.
- Do not mutate DB schema manually in containers or scripts; create/apply EF migrations.

### Testing discipline

When changing behavior or moving code across layers, follow strict TDD:

1. RED: write failing unit/integration tests first.
2. GREEN: implement minimum code to pass.
3. TRIANGULATE: add at least one additional meaningful scenario per behavior.
4. REFACTOR: clean code while tests stay green.

Architecture boundaries should be protected by tests where practical. At minimum, reviews must verify that controllers do not depend on EF Core or Infrastructure persistence entities.

### Entity strategy for the MVP

We use a pragmatic incremental model strategy:

- EF scaffolded entities may remain in `Nido.Infrastructure.Persistence.Entities`.
- Those persistence entities must not leak into `Nido.Api` or `Nido.Application` contracts.
- API behavior should expose request/response DTOs and application/domain models instead of EF entities.
- Move a model into `Nido.Domain` only when it carries business behavior, invariants, or rules worth protecting.

### Repository port rule

For this MVP, repository interfaces belong in `Nido.Application` by default because they are ports required by use cases.

Move a contract to `Nido.Domain` only when it represents a core domain concept needed by domain behavior, not just data access for an application use case.

## Configuration rules

- Do not hardcode integration values in code.
- Use configuration files/environment variables for runtime settings.
- CORS origins come from `Cors:AllowedOrigins` (default local: `http://localhost:4200`).
- Database connection comes from `ConnectionStrings__DefaultConnection` (legacy `ConnectionStrings__Nido` still supported as fallback).

## Local Development Setup

`docker-compose.yml` lives in this repository to provide local infrastructure (PostgreSQL).

### 1. Start the Database
Create your local environment file (if you haven't already) and start the PostgreSQL container:

```bash
cp .env.example .env
docker compose up -d
```

Connection string host depends on where the API runs:

- **API running from host (`dotnet run`)**: use `Host=localhost`.
- **API running inside Docker/Compose**: use `Host=postgres` (the Compose service name).

Example:

```dotenv
# Host runtime example
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=nido;Username=nido;Password=Your_password123

# Compose runtime example (API container)
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=nido;Username=nido;Password=Your_password123
```

### 2. Run Database Migrations
Apply the EF Core migrations to create the database schema:

```bash
dotnet ef database update --project src/Nido.Infrastructure --startup-project src/Nido.Api
```
*(Note: If the `ef` command is not found, install it via: `dotnet tool install --global dotnet-ef`)*

### 3. Run Tests
Run the full suite of Domain, Application, and Integration tests:

```bash
dotnet test
```

> Note: integration tests use in-memory SQLite via `WebApplicationFactory` to keep test runs fast and isolated.

### 4. Start the Application
Run the API project:

```bash
dotnet run --project src/Nido.Api
```

### 5. Verify Health
Check if the API is up and running by hitting the integration smoke endpoint:

```bash
curl http://localhost:8080/hello
```

## Frontend local connection

`nido-frontend` must call this backend through HTTP (no monorepo coupling):

- Backend base URL for local dev: `http://localhost:8080`
- Health/integration endpoint: `GET http://localhost:8080/hello`

In the frontend local environment config, point the API base URL to `http://localhost:8080`.
