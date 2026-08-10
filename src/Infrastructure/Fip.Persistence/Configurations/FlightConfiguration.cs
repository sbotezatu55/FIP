using Fip.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fip.Persistence.Configurations;

public sealed class FlightConfiguration : IEntityTypeConfiguration<FlightEntity>
{
    public void Configure(EntityTypeBuilder<FlightEntity> builder)
    {
        builder.ToTable("Flights");

        builder.HasKey(flight => flight.Id);

        builder.Property(flight => flight.Id)
            .ValueGeneratedNever();

        builder.Property(flight => flight.Icao24)
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(flight => flight.Callsign)
            .HasMaxLength(8);

        builder.Property(flight => flight.StartTime)
            .IsRequired();

        builder.Property(flight => flight.EndTime)
            .IsRequired();

        builder.HasIndex(flight => flight.Icao24)
            .HasDatabaseName("IX_Flights_Icao24");

        builder.HasIndex(flight => flight.Callsign)
            .HasDatabaseName("IX_Flights_Callsign");

        builder.HasIndex(flight => flight.StartTime)
            .HasDatabaseName("IX_Flights_StartTime");

        builder.HasIndex(flight => flight.EndTime)
            .HasDatabaseName("IX_Flights_EndTime");

        builder.HasIndex(flight => new
        {
            flight.Icao24,
            flight.StartTime,
            flight.EndTime
        })
            .HasDatabaseName("IX_Flights_Icao24_StartTime_EndTime");

        builder.HasMany(flight => flight.TelemetryPoints)
            .WithOne(telemetryPoint => telemetryPoint.Flight)
            .HasForeignKey(telemetryPoint => telemetryPoint.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(flight => flight.Events)
            .WithOne(flightEvent => flightEvent.Flight)
            .HasForeignKey(flightEvent => flightEvent.FlightId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
