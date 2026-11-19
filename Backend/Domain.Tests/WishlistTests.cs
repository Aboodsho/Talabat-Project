using Domain.Entities;
using Domain.Tests.Helpers;
using Xunit;

namespace Domain.Tests;

public class WishlistTests
{
    private static Wishlist CreateWishlist() => new(Guid.NewGuid());

    [Fact]
    public void Ctor_EmptyCustomerId_Throws()
        => DomainAssert.Throws("wishlist.invalid_customer", () => new Wishlist(Guid.Empty));

    [Fact]
    public void Ctor_Valid_StartsEmpty()
    {
        var customerId = Guid.NewGuid();

        var wishlist = new Wishlist(customerId);

        Assert.Equal(customerId, wishlist.CustomerId);
        Assert.Empty(wishlist.WishListItems);
    }

    [Fact]
    public void AddItem_NewProduct_AddsIt()
    {
        var wishlist = CreateWishlist();
        var productId = Guid.NewGuid();

        wishlist.AddItem(productId);

        Assert.Equal(productId, Assert.Single(wishlist.WishListItems).ProductId);
    }

    [Fact]
    public void AddItem_SameProductTwice_IsIdempotent()
    {
        var wishlist = CreateWishlist();
        var productId = Guid.NewGuid();

        wishlist.AddItem(productId);
        wishlist.AddItem(productId);

        Assert.Single(wishlist.WishListItems);
    }

    [Fact]
    public void AddItem_AtMaxItems_Throws()
    {
        var wishlist = CreateWishlist();
        for (var i = 0; i < Wishlist.MaxItems; i++)
            wishlist.AddItem(Guid.NewGuid());

        DomainAssert.Throws("wishlist.max_items_exceeded", () => wishlist.AddItem(Guid.NewGuid()));

        Assert.Equal(Wishlist.MaxItems, wishlist.WishListItems.Count);
    }

    [Fact]
    public void AddItem_ExistingProductWhenFull_DoesNotThrow()
    {
        var wishlist = CreateWishlist();
        var existing = Guid.NewGuid();
        wishlist.AddItem(existing);
        for (var i = 1; i < Wishlist.MaxItems; i++)
            wishlist.AddItem(Guid.NewGuid());

        var ex = Record.Exception(() => wishlist.AddItem(existing));    // idempotent check comes first

        Assert.Null(ex);
        Assert.Equal(Wishlist.MaxItems, wishlist.WishListItems.Count);
    }

    [Fact]
    public void RemoveItem_ExistingProduct_RemovesIt()
    {
        var wishlist = CreateWishlist();
        var productId = Guid.NewGuid();
        wishlist.AddItem(productId);
        wishlist.AddItem(Guid.NewGuid());

        wishlist.RemoveItem(productId);

        Assert.DoesNotContain(wishlist.WishListItems, i => i.ProductId == productId);
        Assert.Single(wishlist.WishListItems);
    }

    [Fact]
    public void RemoveItem_UnknownProduct_DoesNothing()
    {
        var wishlist = CreateWishlist();
        wishlist.AddItem(Guid.NewGuid());

        wishlist.RemoveItem(Guid.NewGuid());

        Assert.Single(wishlist.WishListItems);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var wishlist = CreateWishlist();
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        wishlist.Clear();

        Assert.Empty(wishlist.WishListItems);
    }
}