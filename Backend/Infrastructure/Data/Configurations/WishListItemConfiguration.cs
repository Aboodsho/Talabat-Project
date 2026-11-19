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
    public class WishListItemConfiguration : IEntityTypeConfiguration<WishListItem>
    {
        public void Configure(EntityTypeBuilder<WishListItem> builder)
        {
            builder.ToTable("WishListItems");
            builder.HasKey(wli => wli.Id);
            builder.Property(wli => wli.WishListId).IsRequired();
            builder.Property(wli => wli.ProductId).IsRequired();
            builder.Property(wli => wli.CreatedAt).IsRequired();

            builder.HasIndex(wli => new { wli.WishListId, wli.ProductId }).IsUnique();

            builder.HasIndex(wli => wli.ProductId);

            builder.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(wli => wli.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Id).ValueGeneratedNever();
        }
    }
}
