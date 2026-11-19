using Domain.Entities;
using Domain.Enums;
using Domain.Tests.Helpers;
using Xunit;

namespace Domain.Tests;

public class OrderTests
{
    private static Order CreateOrder(params (Guid, int, decimal)[] items)
    {
        if (items.Length == 0)
            items = new[] { (Guid.NewGuid(), 2, 10m) };

        return new Order(Guid.NewGuid(), items, "1 Main St", "Cairo", null, null, "Egypt");
    }

    /// Drives a new order to the requested status using only public transitions.
    private static Order OrderInStatus(OrderStatus status)
    {
        var order = CreateOrder();

        switch (status)
        {
            case OrderStatus.Pending:
                break;
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Shipped:
                order.Confirm(); order.Ship();
                break;
            case OrderStatus.Delivered:
                order.Confirm(); order.Ship(); order.Deliver();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        Assert.Equal(status, order.Status);        // guard: the helper reached the requested status
        return order;
    }

    // ---------- Constructor ----------

    [Fact]
    public void Ctor_Valid_CreatesPendingOrderWithItemsAndAddress()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var order = new Order(customerId, new[] { (productId, 3, 20m) }, "1 Main St", "Cairo", null, null, "Egypt");

        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        var item = Assert.Single(order.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(20m, item.UnitPrice);
        Assert.NotNull(order.ShippingAddress);
    }

    [Fact]
    public void Ctor_EmptyCustomerId_Throws()
        => DomainAssert.Throws("order.invalid_customer",
            () => new Order(Guid.Empty, new[] { (Guid.NewGuid(), 1, 10m) }, "St", "City", null, null, "Country"));

    [Fact]
    public void Ctor_NoItems_Throws()
        => DomainAssert.Throws("order.empty_items",
            () => new Order(Guid.NewGuid(), Array.Empty<(Guid, int, decimal)>(), "St", "City", null, null, "Country"));

    [Fact]
    public void Ctor_NullItems_Throws()
        => DomainAssert.Throws("order.empty_items",
            () => new Order(Guid.NewGuid(), null!, "St", "City", null, null, "Country"));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ctor_ItemWithNonPositiveQuantity_Throws(int quantity)
        => DomainAssert.Throws("order_item.invalid_quantity",
            () => new Order(Guid.NewGuid(), new[] { (Guid.NewGuid(), quantity, 10m) }, "St", "City", null, null, "Country"));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Ctor_ItemWithNonPositivePrice_Throws(decimal price)
        => DomainAssert.Throws("order_item.invalid_price",
            () => new Order(Guid.NewGuid(), new[] { (Guid.NewGuid(), 1, price) }, "St", "City", null, null, "Country"));

    // ---------- TotalAmount ----------

    [Fact]
    public void TotalAmount_SumsQuantityTimesUnitPrice()
    {
        var order = CreateOrder((Guid.NewGuid(), 2, 10m), (Guid.NewGuid(), 1, 5.5m));

        Assert.Equal(25.5m, order.TotalAmount);          // (2 x 10) + (1 x 5.5)
    }

    [Fact]
    public void TotalAmount_UsesFrozenUnitPrice()
    {
        var order = CreateOrder((Guid.NewGuid(), 3, 100m));

        Assert.Equal(300m, order.TotalAmount);
    }

    // ---------- Happy path ----------

    [Fact]
    public void FullLifecycle_Confirm_Ship_Deliver_Succeeds()
    {
        var order = CreateOrder();

        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        order.Ship();
        Assert.Equal(OrderStatus.Shipped, order.Status);

        order.Deliver();
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.UpdatedAt);
    }

    // ---------- Invalid transitions ----------

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Confirm_WhenNotPending_Throws(OrderStatus status)
    {
        var order = OrderInStatus(status);

        DomainAssert.Throws("order.invalid_transition", () => order.Confirm());
        Assert.Equal(status, order.Status);              // unchanged
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Ship_WhenNotConfirmed_Throws(OrderStatus status)
    {
        var order = OrderInStatus(status);

        DomainAssert.Throws("order.invalid_transition", () => order.Ship());
        Assert.Equal(status, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Deliver_WhenNotShipped_Throws(OrderStatus status)
    {
        var order = OrderInStatus(status);

        DomainAssert.Throws("order.invalid_transition", () => order.Deliver());
        Assert.Equal(status, order.Status);
    }

    // ---------- Cancel ----------

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public void Cancel_WhenPendingOrConfirmed_Succeeds(OrderStatus status)
    {
        var order = OrderInStatus(status);

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Cancel_WhenShippedDeliveredOrCancelled_Throws(OrderStatus status)
    {
        var order = OrderInStatus(status);

        DomainAssert.Throws("order.invalid_transition", () => order.Cancel());
        Assert.Equal(status, order.Status);
    }

    // ---------- UpdateShippingAddress ----------

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public void UpdateShippingAddress_BeforeShipping_Succeeds(OrderStatus status)
    {
        var order = OrderInStatus(status);

        var ex = Record.Exception(() => order.UpdateShippingAddress("2 New St", "Giza", null, null, "Egypt"));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void UpdateShippingAddress_AfterShipping_Throws(OrderStatus status)
    {
        var order = OrderInStatus(status);

        DomainAssert.Throws("order.invalid_transition",
            () => order.UpdateShippingAddress("2 New St", "Giza", null, null, "Egypt"));
    }

    [Fact]
    public void UpdateShippingAddress_MapsEachArgumentToTheRightField()
    {
        // Regression test for the old argument-order bug.
        // Property names assumed: Street, City, State, PostalCode, Country. Adjust if yours differ.
        var order = CreateOrder();

        order.UpdateShippingAddress("2 New St", "Giza", "Giza Gov", "12345", "Egypt");

        Assert.Equal("2 New St", order.ShippingAddress.Street);
        Assert.Equal("Giza", order.ShippingAddress.City);
        Assert.Equal("Giza Gov", order.ShippingAddress.State);
        Assert.Equal("12345", order.ShippingAddress.PostalCode);
        Assert.Equal("Egypt", order.ShippingAddress.Country);
    }
}