# BaseCleanArchitecture — .NET 10 Clean Architecture Starter Kit

A production-ready Clean Architecture template built on **.NET 10**, implementing **MediatR**, **Entity Framework Core**, **Serilog**, **Rebus** (message bus), and **Scalar** (API docs). It follows a 6-layer separation:

## Project Structure

```
src/
├── BaseCleanArchitecture.Domain/          # Core business logic
│   ├── Abtractions/Entities/               # IEntityBase, IAuditable, ISoftDelete, etc.
│   ├── Abtractions/Repositories/            # IRepositoryBase, IUnitOfWork
│   ├── Abtractions/DomainEventBase.cs      # Domain event base
│   ├── Entities/                           # Domain entities (e.g., Category)
│   ├── Events/                             # Domain events (e.g., CategoryCreatedEvent)
│   └── Services/                           # Domain services interfaces
│
├── BaseCleanArchitecture.Application/      # Application layer
│   ├── Behaviors/                          # MediatR pipeline behaviors (LoggingBehavior)
│   ├── Common/Interfaces/                  # ICurrentUserService, IDomainEventDispatcher
│   ├── Common/Models/                      # CurrentUser, DTOs
│   └── Features/                           # CQRS commands/queries (e.g., CreateCategoryCommand)
│
├── BaseCleanArchitecture.Infrastructure/   # Infrastructure layer
│   ├── Extensions/Rebus/                   # Rebus configuration (message bus)
│   └── Services/Events/                    # DomainEventDispatcher, IntegrationEventPublisher
│
├── BaseCleanArchitecture.Persistence/      # Data access layer
│   ├── ApplicationDbContext.cs             # EF Core DbContext with auto-audit
│   ├── Repositories/                       # RepositoryBase implementation
│   ├── Configurations/                     # EF entity configurations
│   └── CurrentUserService.cs               # Request-scoped user service
│
├── BaseCleanArchitecture.WebAPI/           # API layer
│   ├── Controllers/                        # API controllers
│   ├── Program.cs                          # App startup + Serilog config
│   └── appsettings.json                    # Configuration
│
└── BaseCleanArchitecture.Contract/         # Shared contract layer (DTOs/events for messaging)
    └── ExternalEvents/                     # Integration events consumed externally
```

## Key Architecture Decisions

| Feature | Implementation |
|---|---|
| **CQRS** | MediatR with `IRequest<TResponse>` pipeline |
| **Audit Fields** | `EntityAuditBase<TKey>` auto-populates `CreatedAt/By`, `UpdatedAt/By` on save |
| **Domain Events** | Dispatched after successful `SaveChangesAsync` via `IDomainEventDispatcher` |
| **Soft Delete** | `ISoftDelete` interface + `IsDeleted` field on `EntityAuditBase` |
| **Unit of Work** | `IUnitOfWork.SaveChangesAsync()` for transactional consistency |
| **Repository Pattern** | `RepositoryBase<TEntity, TKey>` generic base |
| **Logging** | Serilog with console + file sinks, rolling daily |
| **API Docs** | Scalar (Swagger alternative) mapped at `/scalar/v1` in dev |
| **Message Bus** | Rebus for domain event to integration event publishing |
| **Current User** | Scoped `ICurrentUserService` injected via DI |

## Example Flow (Create Category)

1. `POST /api/categories` hits `CategoriesController`
2. `BaseController` dispatches `CreateCategoryCommandRequest` via **MediatR**
3. `LoggingBehavior` logs request handling
4. `CreateCategoryCommandRequestHandler` processes the command
5. `CategoryCreatedEvent` domain event is queued and dispatched via **Rebus**
6. `ApplicationDbContext.SaveChangesAsync` auto-sets audit fields (`CreatedAt`, `CreatedBy`)
7. Integration event (`ExternalEvents.Features.Categories.CategoryCreatedEvent`) is published via **Rebus** for external consumers

## Configuration

- **Database**: SQL Server via EF Core (configured in `appsettings.json`)
- **Serilog**: Console + file (`logs/log-.txt`), structured output
- **Dev Tools**: Scalar API reference at `/scalar/v1`, OpenAPI at `/openapi/v1.json`
