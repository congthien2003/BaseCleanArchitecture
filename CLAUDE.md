# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build                              # Build entire solution
dotnet run --project src/BaseCleanArchitecture.WebAPI  # Run the API
dotnet test                              # Run all tests (no test project exists yet)
```

The solution uses the `.slnx` (Solution Explorer) format. No `*.sln` file exists.

## Architecture

### Layer Dependencies (enforced direction)

```
WebAPI → Application → Domain
           ↓              ↑
     Infrastructure    (Domain has no external dependencies)
           ↓
     Persistence → Contract
```

Each layer references only the one directly below it. `Domain` is the innermost — no external dependencies.

### Entity Inheritance Chain

Entities inherit from `EntityAuditBase<TKey>`, which chains:
- `EntityBase<TKey>` — provides `Id` property (implements `IEntityBase<TKey>`)
- `IAuditable` — adds `CreatedAt/By`, `UpdatedAt/By`, `DeletedAt`, `IsDeleted`

Concrete entities must extend `EntityAuditBase<Guid>`. See `Category.cs` as the reference entity.

### Domain Events → Integration Events

Domain events (`DomainEventBase : INotification`) are dispatched **after** `SaveChangesAsync` succeeds. The flow:

1. Entity raises a domain event (stored in `Entity.DomainEvents`)
2. `ApplicationDbContext.SaveChangesAsync` clears events from entities and dispatches them
3. `DomainEventDispatcher` publishes each via **Rebus** (backed by RabbitMQ, configured via `RebusConfig`)
4. Rebus serializes and sends to a queue
5. External consumers receive integration events from `BaseCleanArchitecture.Contract/ExternalEvents`

### CQRS Pattern

Commands are MediatR `IRequest<TResponse>`. Handlers live in `Application/Features/<Feature>/Commands/`. The `BaseController` injects `IMediator` and dispatches requests. A `LoggingBehavior<TRequest, TResponse>` pipeline behavior logs all requests.

### Persistence Conventions

- `RepositoryBase<TEntity, TKey>` constrains `TEntity : EntityBase<TKey>` — entities must have an `Id`
- `ApplicationDbContext` uses SQL Server (`UseSqlServer`). Connection string comes from `appsettings.json` → `DefaultConnection`
- Audit fields (`CreatedAt`, `CreatedBy`, etc.) are auto-populated in `SaveChangesAsync` using `ICurrentUserService`
- Entity configurations live in `Persistence/Configurations/` and are auto-applied via `ApplyConfigurationsFromAssembly`

### Message Bus (Rebus/RabbitMQ)

- Connection string sourced from `RebusConfig.RabbitMqConnectionString` (appsettings section)
- `AddRebusServices` in `Infrastructure/DependencyInjection.cs` wires the bus
- Domain events implement `INotification` (MediatR) and are published through `IPublisher`
