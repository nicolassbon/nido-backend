---
name: dotnet-patterns
description: 'Pragmatic C#/.NET patterns and conventions for maintainable ASP.NET Core applications without overengineering.'
---

# Pragmatic .NET Development Patterns

## Use this skill when

- Implementing normal backend features in ASP.NET Core
- Deciding everyday code structure for services, DTOs, integrations, and persistence
- Refactoring backend code to keep it simple, readable, and maintainable
- Applying project conventions during routine coding

## Do not use this skill when

- Defining the base architecture of the whole system; use `dotnet-architect`
- Reviewing an implementation after the fact; use `dotnet-design-pattern-review`
- Handling framework-specific host/pipeline decisions where `aspnet-core` is the primary owner

Use idiomatic C# and ASP.NET Core patterns to build clear, maintainable backend code.

Prefer simplicity.  
Do **not** introduce patterns unless they solve a real problem.  
Do **not** force enterprise architecture into a small or medium project.

---

## Core Principles

### 1. Keep Architecture Simple

Prefer a pragmatic layered structure unless the project already adopted something else explicitly.

- Controllers: HTTP concerns only
- DTOs: request/response contracts
- Services: business logic
- Repositories/Data Access: persistence when useful
- Entities: domain/data model
- Integrations: external APIs and infrastructure concerns

Avoid putting business logic directly in controllers.

Do NOT reinterpret this section as permission to redesign the whole application architecture. This skill is for implementation-level structure, not architectural governance.

---

### 2. Use Dependency Injection Clearly

Use constructor injection for services and infrastructure dependencies.

```csharp
public class InventoryService(
    IInventoryRepository inventoryRepository,
    ILogger<InventoryService> logger)
{
}
```

Use interfaces when they add value:

- Multiple implementations
- External integrations
- Testing/mocking
- Clear service boundary

Do **not** create interfaces automatically for every class.

---

### 3. Use Async for I/O

Use async/await for:

- EF Core queries
- HTTP calls
- File operations
- Background jobs
- Notification sending

Avoid:

```csharp
.Result
.Wait()
.GetAwaiter().GetResult()
```

Pass `CancellationToken` when appropriate, especially in services and integrations.

---

### 4. Prefer Clear DTOs

Use request and response DTOs instead of exposing entities directly.

```csharp
public sealed record CreateInventoryItemRequest(
    string Name,
    decimal Quantity,
    string Unit,
    DateOnly? ExpirationDate
);
```

DTOs should represent API contracts, not database structure.

---

### 5. Use EF Core Pragmatically

Use EF Core directly when the query is simple.

Use repository abstractions only when they improve clarity, testability, or isolate complex persistence logic.

Use `AsNoTracking()` for read-only queries.

```csharp
var items = await _db.InventoryItems
    .AsNoTracking()
    .Where(x => x.HouseholdId == householdId)
    .ToListAsync(cancellationToken);
```

Avoid N+1 queries.

Keep queries readable.

---

### 6. Protect Household Boundaries

For multi-household applications, every query and command must respect the current household context.

Always validate that the current user belongs to the household before accessing or modifying data.

Do not trust IDs from the client without checking ownership/access.

---

### 7. Use Options Pattern for Configuration

Use strongly typed options for external integrations and configuration.

```csharp
public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public required string BotToken { get; init; }
    public string? DefaultChatId { get; init; }
}
```

Register with:

```csharp
builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection(TelegramOptions.SectionName));
```

---

### 8. Handle Errors Consistently

Use clear domain/application exceptions for meaningful business failures.

Examples:

- `ResourceNotFoundException`
- `HouseholdAccessDeniedException`
- `ValidationException`
- `ExternalServiceException`

Return consistent API error responses.

Prefer `ProblemDetails` or a project-wide error response format.

---

### 9. Use Guard Clauses

Prefer early validation over deep nesting.

```csharp
if (quantity <= 0)
{
    throw new ValidationException("Quantity must be greater than zero.");
}
```

Keep the happy path easy to read.

---

### 10. Keep Patterns Optional

Patterns are tools, not rules.

Use Factory only when object creation varies by type or provider.

Use Strategy when behavior changes by type, such as notification channels.

Use Result pattern only if the project already adopts it consistently.

Do not introduce CQRS, MediatR, Unit of Work, Repository Pattern, or Clean Architecture layers unless the project actually needs them.

---

## Recommended Patterns for Nido-Style Projects

Useful patterns:

- Layered architecture
- DTO mapping
- Service layer
- Options pattern
- Strategy pattern for notification channels
- Factory pattern only when selecting between providers
- Centralized exception handling
- Background job/service for scheduled notifications

Use carefully:

- Repository pattern
- Result pattern
- Domain events
- Generic base services

Avoid by default:

- CQRS
- MediatR
- Microservices
- Overly generic repositories
- Abstract factories everywhere
- “One interface per class” as a rule

---

## Anti-Patterns to Avoid

| Anti-Pattern | Better Approach |
|---|---|
| Fat controllers | Move business logic to services |
| Interfaces for every class | Use interfaces only where useful |
| Generic architecture before domain is clear | Start simple and evolve |
| `.Result` or `.Wait()` | Use `await` |
| Exposing EF entities in API responses | Use DTOs |
| Missing household access checks | Validate ownership/access in service layer |
| Huge services with many responsibilities | Split by feature/use case |
| Catching and swallowing exceptions | Handle or log with context |
| Hardcoded secrets | Use configuration/environment variables |

---

## Review Checklist

When writing or reviewing .NET code, check:

- Is the code simple enough?
- Is business logic outside controllers?
- Are DTOs separated from entities?
- Is async used correctly?
- Are household boundaries enforced?
- Are dependencies injected clearly?
- Is EF Core used efficiently?
- Are errors handled consistently?
- Are patterns solving a real problem?
- Would a teammate understand this code quickly?
