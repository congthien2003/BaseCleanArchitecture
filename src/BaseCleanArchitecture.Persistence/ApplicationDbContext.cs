using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using BaseCleanArchitecture.Domain.Abtractions;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;

        private readonly IDomainEventDispatcher _domainEventDispatcher;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService, IDomainEventDispatcher domainEventDispatcher) : base(options)
        {
            _currentUserService = currentUserService;
            _domainEventDispatcher = domainEventDispatcher;
        }

        public DbSet<Category> Category { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {

            var entries = ChangeTracker.Entries<EntityAuditBase<Guid>>();

            var entitiesWithEvents = ChangeTracker
                                .Entries<EntityAuditBase<Guid>>()
                                .Where(e => e.Entity.DomainEvents.Any())
                                .Select(e => e.Entity)
                                .ToList();

            // Get all domain events
            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            // Clear domain events from entities
            entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy ??= _currentUserService.CurrentUser.Id;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy ??= _currentUserService.CurrentUser.Id;
                }
            }

            var result = await base.SaveChangesAsync(cancellationToken);

            // Dispatch domain events after successful save
            if (result > 0 && domainEvents.Any())
            {
                await _domainEventDispatcher.PublishEventsAsync(
                    domainEvents,
                    cancellationToken);
            }

            return result;
        }

    }
}
