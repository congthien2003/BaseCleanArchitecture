# BaseCleanArchitecture — Copilot Instructions

.NET 10 Clean Architecture template with MediatR, EF Core, Serilog, Rebus, and Scalar.
See [README.md](../README.md) for full project overview and example flow.

## Architecture

Six layers with strict dependency direction (inner layers have no knowledge of outer):

| Layer          | Project                                | Responsibility                                               |
| -------------- | -------------------------------------- | ------------------------------------------------------------ |
| Domain         | `BaseCleanArchitecture.Domain`         | Entities, domain events, repository interfaces               |
| Application    | `BaseCleanArchitecture.Application`    | CQRS handlers, MediatR behaviors, service interfaces         |
| Infrastructure | `BaseCleanArchitecture.Infrastructure` | Rebus messaging, domain/integration event dispatchers        |
| Persistence    | `BaseCleanArchitecture.Persistence`    | EF Core DbContext, repository implementations, migrations    |
| Contract       | `BaseCleanArchitecture.Contract`       | External integration event DTOs (consumed by other services) |
| WebAPI         | `BaseCleanArchitecture.WebAPI`         | Controllers, Program.cs, DI composition root                 |

## Build & Run

```bash
dotnet build
dotnet run --project src/BaseCleanArchitecture.WebAPI/
```

- API docs (dev): `/scalar/v1` | OpenAPI spec: `/openapi/v1.json`
- **Prerequisites**: SQL Server (Windows Auth, `localhost`) and RabbitMQ (`amqp://guest:guest@localhost:5672`) must be running. Domain event publishing is skipped if Rebus/RabbitMQ is unavailable.
- No EF migrations are committed yet — run `dotnet ef migrations add Init` before first run.

## Conventions

### Typo in Domain folder — do NOT correct

The folder and namespace is `Abtractions` (missing the 's'), not `Abstractions`. This is intentional throughout the codebase.

```
src/BaseCleanArchitecture.Domain/Abtractions/   ← use as-is
namespace BaseCleanArchitecture.Domain.Abtractions
```

### Adding a new feature — follow the Category example

1. **Domain entity** — extend `EntityAuditBase<TKey>` (auto-audit fields + soft delete):
   - See [Category.cs](../src/BaseCleanArchitecture.Domain/Entities/Category.cs)
2. **Domain event** — extend `DomainEventBase`, call `AddDomainEvent()` from entity:
   - See [Events/Category/](../src/BaseCleanArchitecture.Domain/Events/Category/)
3. **CQRS command/query** — `IRequest<TResponse>` + `IRequestHandler<,>` in one file:
   - See [CreateCategoryCommandRequest.cs](../src/BaseCleanArchitecture.Application/Features/Categories/Commands/CreateCategoryCommandRequest.cs)
4. **Input/Output DTOs** — place in `Features/<Name>/Models/`
5. **EF configuration** — implement `IEntityTypeConfiguration<TEntity>` in `Persistence/Configurations/`:
   - See [CategoryConfiguration.cs](../src/BaseCleanArchitecture.Persistence/Configurations/CategoryConfiguration.cs)
6. **Controller** — inherit `BaseController`, dispatch via `Mediator.Send()`:
   - See [CategoriesController.cs](../src/BaseCleanArchitecture.WebAPI/Controllers/Features/CategoriesController.cs)
7. **Integration event** — extend `BaseExternalEvent` in `Contract/ExternalEvents/Features/<Name>/`
8. **DI registration** — add `IRepository<T,K>` bindings in the relevant `DependencyInjection.cs`

### DI pattern — method per layer

Each layer exposes an extension method on `IServiceCollection`:

```csharp
// Application — AddApplication()
// Infrastructure — AddInfrastructure()
// Persistence — AddPersistenceServices()
```

`Program.cs` composes all three. Register new services in the correct layer's `DependencyInjection.cs`.

### Repository + Unit of Work

- Always depend on `IRepositoryBase<TEntity, TKey>` (Domain interface), not the concrete class.
- Always call `IUnitOfWork.SaveChangesAsync()` — do NOT call `DbContext.SaveChangesAsync()` directly. The DbContext override auto-sets audit fields and dispatches domain events.

### Auto-audit (handled by `ApplicationDbContext`)

`SaveChangesAsync` automatically sets `CreatedAt/By` on Added and `UpdatedAt/By` on Modified entities. Do not set these fields manually.

## Key Placeholders (starter kit)

These exist in the template but are not yet implemented — add real logic when building features:

- `CategoryService` / `ICategoryService` — currently empty
- `CurrentUserService` — returns empty defaults (no auth wired up)
- No FluentValidation, no global error-handling middleware, no domain event handlers, no test projects
