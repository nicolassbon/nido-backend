# Nido Backend

.NET 10 backend for Nido MVP 1. This repository owns the HTTP contract and implements the backend Clean Architecture layers.

## Role in the architecture

- `nido-backend` and `nido-frontend` are separate repositories.
- This repo is the **source of truth** for API endpoints, payloads, and response semantics.
- Frontend integration must happen through HTTP, based on this contract.

## MVP HTTP contract (owned here)

### `GET /hello`

- Purpose: integration smoke check between the two repos.
- Response `200 OK`:

```json
{ "message": "Bienvenido a Nido!" }
```

### `POST /household`

- Purpose: create a household.
- Request body:

```json
{ "name": "Casa de Nico" }
```

- Response `201 Created`:

```json
{ "id": "uuid", "name": "Casa de Nico" }
```

- Validation: `400 Bad Request` (Problem Details) when `name` is missing or invalid.

## Clean Architecture split

| Path | Responsibility |
|------|----------------|
| `src/Nido.Api` | HTTP exposure (controllers, contracts, startup wiring) |
| `src/Nido.Application` | Use cases and ports |
| `src/Nido.Domain` | Entities, value objects, pure business rules |
| `src/Nido.Infrastructure` | EF Core, repositories, persistence wiring |
| `tests/` | Domain, Application, and API integration tests |

## Configuration rules

- Do not hardcode integration values in code.
- Use configuration files/environment variables for runtime settings.
- CORS origins come from `Cors:AllowedOrigins` (default local: `http://localhost:4200`).
- Database connection comes from `ConnectionStrings__Nido`.

## Local environment (Phase 5 MVP)

`docker-compose.yml` lives in this repository by design for MVP simplicity.

1. Create local env file:

```bash
cp .env.example .env
```

2. Start SQL Server for local development:

```bash
docker compose up -d sqlserver
```

3. Apply migrations:

```bash
dotnet ef database update --project src/Nido.Infrastructure/Nido.Infrastructure.csproj --context NidoDbContext
```

4. Run the API from `src/Nido.Api`:

```bash
dotnet run
```

5. Verify backend:

```bash
curl http://localhost:8080/hello
```

## Frontend local connection

`nido-frontend` must call this backend through HTTP (no monorepo coupling):

- Backend base URL for local dev: `http://localhost:8080`
- Health/integration endpoint: `GET http://localhost:8080/hello`

In the frontend local environment config, point the API base URL to `http://localhost:8080`.
