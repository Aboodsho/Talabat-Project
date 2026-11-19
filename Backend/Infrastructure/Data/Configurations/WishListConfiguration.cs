using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Infrastructure.Data.Configurations
{
    public class WishListConfiguration : IEntityTypeConfiguration<Wishlist>
    {
  
        public void Configure(EntityTypeBuilder<Wishlist> builder)
        {
            builder.ToTable("WishLists");
            builder.HasKey(w => w.Id);
            builder.Property(w => w.CreatedAt).IsRequired();

            builder.Metadata.FindNavigation(nameof(Wishlist.WishListItems))!
                   .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(w=>w.WishListItems).WithOne()
                   .HasForeignKey(wli=>wli.WishListId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(x => x.Id).ValueGeneratedNever();

        }
    }
}

// builder.HasMany(w => w.WishListItems)
//      .WithOne()
//      .HasForeignKey(wli => wli.WishListId)
//      .OnDelete(DeleteBehavior.Cascade)
//      .HasField("_wishlistitems")                          
//      .UsePropertyAccessMode(PropertyAccessMode.Field);     

// if backing feild not conviention we should use this because we need returnINavigation
// and HasField return Inavigation  