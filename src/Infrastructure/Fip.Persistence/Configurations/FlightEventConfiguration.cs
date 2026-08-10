using Fip.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fip.Persistence.Configurations;

public sealed class FlightEventConfiguration : IEntityTypeConfiguration<FlightEventEntity>
{
    public void Configure(EntityTypeBuilder<FlightEventEntity> builder)
    {
        builder.ToTable("FlightEvents");

        builder.HasKey(flightEvent => flightEvent.Id);

        builder.Property(flightEvent => flightEvent.Id)
            .ValueGeneratedNever();

        builder.Property(flightEvent => flightEvent.FlightId)
            .IsRequired();

        builder.Property(flightEvent => flightEvent.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(flightEvent => flightEvent.Timestamp)
            .IsRequired();

        builder.Property(flightEvent => flightEvent.Latitude);
        builder.Property(flightEvent => flightEvent.Longitude);
        builder.Property(flightEvent => flightEvent.AltitudeFeet);

        builder.Property(flightEvent => flightEvent.Description)
            .HasMaxLength(500);

        builder.HasIndex(flightEvent => new
        {
            flightEvent.FlightId,
            flightEvent.Timestamp
        })
        .HasDatabaseName("IX_FlightEvents_FlightId_Timestamp");

        builder.HasOne(flightEvent => flightEvent.Flight)
            .WithMany(flight => flight.Events)
            .HasForeignKey(flightEvent => flightEvent.FlightId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
