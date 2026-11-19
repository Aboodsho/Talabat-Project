using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Data.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");
            builder.HasKey(ci => ci.Id);
            builder.Property(ci => ci.CartId).IsRequired();

            builder.Property(ci => ci.CartId).IsRequired();
            builder.Property(ci => ci.ProductId).IsRequired();
            builder.Property(ci => ci.Quantity).IsRequired();

            builder.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();
            builder.HasIndex(ci => ci.ProductId);

            builder.HasOne<Product>()
                   .WithMany()
                   .HasForeignKey(ci => ci.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(ci => ci.Id).ValueGeneratedNever();

        }
    }
}
