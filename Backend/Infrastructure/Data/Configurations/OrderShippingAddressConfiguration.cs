using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public class OrderShippingAddressConfiguration : IEntityTypeConfiguration<OrderShippingAddress>
{
    public void Configure(EntityTypeBuilder<OrderShippingAddress> builder)
    {
        builder.ToTable("OrderShippingAddresses");

        builder.HasKey(osa => osa.Id);

        builder.Property(osa => osa.CreatedAt)
            .IsRequired();

        builder.Property(osa => osa.Street)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(osa => osa.City)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(osa => osa.PostalCode)
            .HasMaxLength(200);

        builder.Property(osa => osa.Country)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(osa => osa.State)
            .HasMaxLength(200);
    }
}