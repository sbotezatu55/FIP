using Fip.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fip.Persistence.Context;

public sealed class FipDbContext(DbContextOptions<FipDbContext> options) : DbContext(options)
{
    public DbSet<FlightEntity> Flights => Set<FlightEntity>();

    public DbSet<FlightTelemetryPointEntity> FlightTelemetryPoints => Set<FlightTelemetryPointEntity>();

    public DbSet<FlightEventEntity> FlightEvents => Set<FlightEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FipDbContext).Assembly);
    }
}
