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

## Local Development Setup

`docker-compose.yml` lives in this repository to provide local infrastructure (SQL Server).

### 1. Start the Database
Create your local environment file (if you haven't already) and start the SQL Server container:

```bash
cp .env.example .env
docker compose up -d
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
