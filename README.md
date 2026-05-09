# Nido Backend

.NET 10 backend for the Nido MVP 1. This folder contains the API, domain, use cases, infrastructure, and tests for the first real `Household` flow.

## Quick path

1. Create a repository-root `.env` file with `ConnectionStrings__Nido=...`.
2. Start MySQL by whatever local method you are using in this migration stage.
3. Apply EF migrations from repository root with `dotnet ef database update --project src/Nido.Infrastructure/Nido.Infrastructure.csproj --context NidoDbContext`.
4. From `src/Nido.Api`, run `dotnet run`.
5. Smoke-check API with `curl http://localhost:8080/hello`.
6. Run backend tests with `dotnet test Nido.slnx --configuration Release`.

## Structure

| Path | Responsibility |
|------|----------------|
| `src/Nido.Api` | HTTP exposure, controllers, contracts, Program wiring |
| `src/Nido.Application` | Use cases and ports |
| `src/Nido.Domain` | Entities, value objects, pure business rules |
| `src/Nido.Infrastructure` | EF Core, repositories, persistence wiring |
| `tests/` | Domain, Application, and API integration tests |

## Notes

- The backend follows the Clean Architecture split defined in `Arquitectura-MVP1.md`.
- Database schema changes must go through **EF Core migrations**.
- Generated outputs such as `bin/`, `obj/`, and `TestResults/` should not be tracked in git.

## EF Core migrations (standard workflow)

The API startup does **not** run `Database.Migrate()` automatically. Apply migrations explicitly before using flows that depend on the schema (for example, `POST /household`).

From repository root:

```bash
dotnet ef database update --project src/Nido.Infrastructure/Nido.Infrastructure.csproj --context NidoDbContext
```

`ConnectionStrings__Nido` is the source of truth for the .NET runtime and EF tooling.
`MYSQL_*` variables are for Docker/MySQL only.

## Run API locally (host)

From `src/Nido.Api/`:

```bash
dotnet run
```

## NuGet packages (standard workflow)

Add or remove NuGet packages through the **.NET CLI first**, not by manually editing `.csproj` files.

Recommended commands on .NET 10:

```bash
dotnet package add <PackageName> --project <ProjectPath>
dotnet package remove <PackageName> --project <ProjectPath>
```

Use manual `.csproj` edits only for follow-up metadata that the CLI does not express well, such as `PrivateAssets` or `IncludeAssets`.
