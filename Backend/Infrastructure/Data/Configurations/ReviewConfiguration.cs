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
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ProductId).IsRequired();
            builder.Property(r => r.CustomerId).IsRequired();
            builder.Property(r => r.Rating).IsRequired();
            builder.Property(r => r.Comment) .HasMaxLength(1000).IsRequired(false);

            builder.HasIndex(r => r.ProductId);
            builder.HasIndex(r => r.CustomerId);
            builder.HasIndex(r => new { r.ProductId, r.CustomerId }).IsUnique();


            builder.HasOne<Product>()
                   .WithMany()
                   .HasForeignKey(r => r.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
