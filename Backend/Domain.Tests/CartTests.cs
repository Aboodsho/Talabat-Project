using Domain.Entities;
using Domain.Tests.Helpers;
using Xunit;

namespace Domain.Tests;

public class CartTests
{
    private static Cart CreateCart() => new(Guid.NewGuid());

    // ---------- Constructor ----------

    [Fact]
    public void Ctor_EmptyCustomerId_Throws()
        => DomainAssert.Throws("cart.invalid_customer", () => new Cart(Guid.Empty));

    [Fact]
    public void Ctor_Valid_StartsEmpty()
    {
        var customerId = Guid.NewGuid();

        var cart = new Cart(customerId);

        Assert.Equal(customerId, cart.CustomerId);
        Assert.Empty(cart.CartItems);
    }

    // ---------- AddCartItem ----------

    [Fact]
    public void AddCartItem_NewProduct_AddsItem()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();

        cart.AddCartItem(productId, quantity: 2, availableStock: 10);

        var item = Assert.Single(cart.CartItems);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void AddCartItem_SameProductTwice_IncreasesQuantityWithoutDuplicate()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();

        cart.AddCartItem(productId, 2, 10);
        cart.AddCartItem(productId, 3, 10);

        var item = Assert.Single(cart.CartItems);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public void AddCartItem_DifferentProducts_AddsSeparateItems()
    {
        var cart = CreateCart();

        cart.AddCartItem(Guid.NewGuid(), 1, 10);
        cart.AddCartItem(Guid.NewGuid(), 1, 10);

        Assert.Equal(2, cart.CartItems.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddCartItem_NonPositiveQuantity_Throws(int quantity)
        => DomainAssert.Throws("cart_item.invalid_amount",
            () => CreateCart().AddCartItem(Guid.NewGuid(), quantity, 10));

    [Fact]
    public void AddCartItem_MoreThanStock_Throws()
        => DomainAssert.Throws("cart_item.insufficient_stock",
            () => CreateCart().AddCartItem(Guid.NewGuid(), quantity: 6, availableStock: 5));

    [Fact]
    public void AddCartItem_ExactlyStock_IsAllowed()
    {
        var cart = CreateCart();

        cart.AddCartItem(Guid.NewGuid(), quantity: 5, availableStock: 5);

        Assert.Equal(5, Assert.Single(cart.CartItems).Quantity);
    }

    [Fact]
    public void AddCartItem_ExactlyMaxQuantity_IsAllowed()
    {
        var cart = CreateCart();

        cart.AddCartItem(Guid.NewGuid(), quantity: CartItem.MaxQuantity, availableStock: 100);

        Assert.Equal(10, Assert.Single(cart.CartItems).Quantity);
    }

    [Fact]
    public void AddCartItem_AboveMaxQuantity_Throws()
        => DomainAssert.Throws("cart_item.max_quantity_exceeded",
            () => CreateCart().AddCartItem(Guid.NewGuid(), quantity: 11, availableStock: 100));

    [Fact]
    public void AddCartItem_ExistingPlusNewAboveMax_ThrowsAndKeepsQuantity()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddCartItem(productId, 6, 100);

        DomainAssert.Throws("cart_item.max_quantity_exceeded", () => cart.AddCartItem(productId, 5, 100));

        Assert.Equal(6, Assert.Single(cart.CartItems).Quantity);       // unchanged after failure
    }

    [Fact]
    public void AddCartItem_StockIsCheckedBeforeMax()
    {
        // 11 is above both the stock (5) and the max (10): the stock error wins
        DomainAssert.Throws("cart_item.insufficient_stock",
            () => CreateCart().AddCartItem(Guid.NewGuid(), quantity: 11, availableStock: 5));
    }

    [Fact]
    public void AddCartItem_ExistingPlusNewAboveStock_Throws()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddCartItem(productId, 3, availableStock: 5);

        DomainAssert.Throws("cart_item.insufficient_stock", () => cart.AddCartItem(productId, 3, availableStock: 5));
    }

    // ---------- DecreaseItemQuantity ----------

    [Fact]
    public void DecreaseItemQuantity_ReducesQuantity()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddCartItem(productId, 5, 10);

        cart.DecreaseItemQuantity(productId, 2);

        Assert.Equal(3, Assert.Single(cart.CartItems).Quantity);
    }

    [Fact]
    public void DecreaseItemQuantity_DefaultAmountIsOne()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddCartItem(productId, 5, 10);

        cart.DecreaseItemQuantity(productId);

        Assert.Equal(4, Assert.Single(cart.CartItems).Quantity);
    }

    [Theory]
    [InlineData(3)]     // equal to quantity
    [InlineData(10)]    // greater than quantity
    public void DecreaseItemQuantity_AmountReachesQuantity_RemovesItem(int amount)
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddCartItem(productId, 3, 10);

        cart.DecreaseItemQuantity(productId, amount);

        Assert.Empty(cart.CartItems);
    }

    [Fact]
    public void DecreaseItemQuantity_UnknownProduct_Throws()
        => DomainAssert.Throws("Cart_Item.not_found",
            () => CreateCart().DecreaseItemQuantity(Guid.NewGuid()));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecreaseItemQuantity_NonPositiveAmount_Throws(int amount)
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddCartItem(productId, 5, 10);

        DomainAssert.Throws("cart_item.invalid_amount", () => cart.DecreaseItemQuantity(productId, amount));
    }

    // ---------- RemoveItem / Clear ----------

    [Fact]
    public void RemoveItem_ExistingProduct_RemovesIt()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddCartItem(productId, 1, 10);
        cart.AddCartItem(Guid.NewGuid(), 1, 10);

        cart.RemoveItem(productId);

        Assert.DoesNotContain(cart.CartItems, i => i.ProductId == productId);
        Assert.Single(cart.CartItems);
    }

    [Fact]
    public void RemoveItem_UnknownProduct_DoesNothing()
    {
        var cart = CreateCart();
        cart.AddCartItem(Guid.NewGuid(), 1, 10);

        cart.RemoveItem(Guid.NewGuid());

        Assert.Single(cart.CartItems);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cart = CreateCart();
        cart.AddCartItem(Guid.NewGuid(), 1, 10);
        cart.AddCartItem(Guid.NewGuid(), 2, 10);

        cart.Clear();

        Assert.Empty(cart.CartItems);
    }
}