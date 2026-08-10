using Fip.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fip.Persistence.Configurations;

public sealed class FlightTelemetryPointConfiguration : IEntityTypeConfiguration<FlightTelemetryPointEntity>
{
    public void Configure(EntityTypeBuilder<FlightTelemetryPointEntity> builder)
    {
        builder.ToTable("FlightTelemetryPoints");

        builder.HasKey(telemetryPoint => telemetryPoint.Id);

        builder.Property(telemetryPoint => telemetryPoint.Id)
            .ValueGeneratedNever();

        builder.Property(telemetryPoint => telemetryPoint.FlightId)
            .IsRequired();

        builder.Property(telemetryPoint => telemetryPoint.Timestamp)
            .IsRequired();

        builder.Property(telemetryPoint => telemetryPoint.Icao24)
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(telemetryPoint => telemetryPoint.Callsign)
            .HasMaxLength(8);

        builder.Property(telemetryPoint => telemetryPoint.Latitude);
        builder.Property(telemetryPoint => telemetryPoint.Longitude);
        builder.Property(telemetryPoint => telemetryPoint.AltitudeFeet);
        builder.Property(telemetryPoint => telemetryPoint.GroundSpeedKnots);
        builder.Property(telemetryPoint => telemetryPoint.TrackDegrees);
        builder.Property(telemetryPoint => telemetryPoint.VerticalRateFeetPerMinute);

        builder.HasIndex(telemetryPoint => new
        {
            telemetryPoint.FlightId,
            telemetryPoint.Timestamp
        })
        .HasDatabaseName("IX_FlightTelemetryPoints_FlightId_Timestamp");

        builder.HasOne(telemetryPoint => telemetryPoint.Flight)
            .WithMany(flight => flight.TelemetryPoints)
            .HasForeignKey(telemetryPoint => telemetryPoint.FlightId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
