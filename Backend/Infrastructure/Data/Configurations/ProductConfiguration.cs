using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => p.Name);
            builder.Property(p=>p.CreatedAt).IsRequired();
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).IsRequired(false).HasMaxLength(1000);
            builder.Property(p => p.Price).IsRequired().HasPrecision(18, 2);
            builder.HasQueryFilter(p => p.DeletedAt == null);
            builder.Ignore(p => p.IsDeleted);

            builder.HasOne(p => p.Category).WithMany()
                   .HasForeignKey(p => p.CategoryId).IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.Metadata.FindNavigation(nameof(Product.Images))
                   ?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(p=>p.Images).WithOne()
                   .HasForeignKey(pi => pi.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property<byte[]>("RowVersion").IsRowVersion(); // for making order and solve race condition


        }
    }
}
