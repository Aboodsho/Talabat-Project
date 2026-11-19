using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Data.Configurations
{
    internal class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CreatedAt).IsRequired();
            builder.Property(o => o.CustomerId).IsRequired();
            builder.Property(o=>o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Ignore(o => o.TotalAmount);

            builder.HasIndex(o => o.CustomerId);                                                 // new
            builder.HasIndex(o => o.Status);                                                     // new


            builder.HasOne(o => o.ShippingAddress)
                   .WithOne()
                   .HasForeignKey<OrderShippingAddress>(sa => sa.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(Order.Items))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(o=>o.Items).WithOne()
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }
}


