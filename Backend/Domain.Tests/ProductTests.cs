using Domain.Entities;
using Domain.Tests.Helpers;
using Xunit;

namespace Domain.Tests;

public class ProductTests
{
    private static Product CreateProduct(int stock = 10, decimal price = 100m)
        => new("Laptop", "Gaming laptop", price, stock, null);

    // ---------- Constructor ----------

    [Fact]
    public void Ctor_ValidArguments_SetsProperties()
    {
        var categoryId = Guid.NewGuid();

        var product = new Product("  Laptop  ", "desc", 99.5m, 7, categoryId);

        Assert.Equal("Laptop", product.Name);                 // trimmed
        Assert.Equal("desc", product.Description);
        Assert.Equal(99.5m, product.Price);
        Assert.Equal(7, product.StockQuantity);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.True(product.IsActive);
        Assert.False(product.IsDeleted);
        Assert.NotEqual(Guid.Empty, product.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Ctor_EmptyName_Throws(string? name)
        => DomainAssert.Throws("product.name_required", () => new Product(name!, null, 10m, 1, null));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ctor_NonPositivePrice_Throws(decimal price)
        => DomainAssert.Throws("product.invalid_price", () => new Product("Laptop", null, price, 1, null));

    [Fact]
    public void Ctor_NegativeStock_Throws()      // EXPECTED RED until the ctor uses SetStock
        => DomainAssert.Throws("product.negative_stock", () => new Product("Laptop", null, 10m, -5, null));

    // ---------- SetName / SetPrice / SetStock ----------

    [Fact]
    public void SetName_Whitespace_Throws()      // EXPECTED RED until IsNullOrWhiteSpace is used
        => DomainAssert.Throws("product.name_required", () => CreateProduct().SetName("   "));

    [Fact]
    public void SetName_Valid_TrimsAndMarksUpdated()
    {
        var product = CreateProduct();

        product.SetName("  Phone ");

        Assert.Equal("Phone", product.Name);
        Assert.NotNull(product.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SetPrice_NonPositive_Throws(decimal price)
        => DomainAssert.Throws("product.invalid_price", () => CreateProduct().SetPrice(price));

    [Fact]
    public void SetStock_Negative_Throws()
        => DomainAssert.Throws("product.negative_stock", () => CreateProduct().SetStock(-1));

    [Fact]
    public void SetStock_Zero_IsAllowed()
    {
        var product = CreateProduct();

        product.SetStock(0);

        Assert.Equal(0, product.StockQuantity);
    }

    // ---------- IncreaseStock ----------

    [Fact]
    public void IncreaseStock_Valid_AddsToStock()
    {
        var product = CreateProduct(stock: 10);

        product.IncreaseStock(5);

        Assert.Equal(15, product.StockQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void IncreaseStock_NonPositive_Throws(int amount)
        => DomainAssert.Throws("product.invalid_amount", () => CreateProduct().IncreaseStock(amount));

    // ---------- DecreaseStock ----------

    [Fact]
    public void DecreaseStock_Valid_SubtractsFromStock()
    {
        var product = CreateProduct(stock: 10);

        product.DecreaseStock(4);

        Assert.Equal(6, product.StockQuantity);
    }

    [Fact]
    public void DecreaseStock_ExactlyAvailable_GoesToZero()
    {
        var product = CreateProduct(stock: 3);

        product.DecreaseStock(3);

        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void DecreaseStock_MoreThanAvailable_ThrowsAndKeepsStock()
    {
        var product = CreateProduct(stock: 3);

        DomainAssert.Throws("product.insufficient_stock", () => product.DecreaseStock(4));

        Assert.Equal(3, product.StockQuantity);              // unchanged after failure
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecreaseStock_NonPositive_Throws(int amount)
        => DomainAssert.Throws("product.invalid_amount", () => CreateProduct().DecreaseStock(amount));

    // ---------- Activate / Deactivate ----------

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var product = CreateProduct();

        product.Deactivate();
        Assert.False(product.IsActive);

        product.Activate();
        Assert.True(product.IsActive);
    }

    // ---------- Soft delete ----------

    [Fact]
    public void Delete_SetsDeletedAt()
    {
        var product = CreateProduct();

        product.Delete();

        Assert.True(product.IsDeleted);
        Assert.NotNull(product.DeletedAt);
    }

    [Fact]
    public void Delete_Twice_KeepsFirstDeletedAt()
    {
        var product = CreateProduct();
        product.Delete();
        var first = product.DeletedAt;

        product.Delete();

        Assert.Equal(first, product.DeletedAt);
    }

    [Fact]
    public void Restore_ClearsDeletedAt()
    {
        var product = CreateProduct();
        product.Delete();

        product.Restore();

        Assert.False(product.IsDeleted);
        Assert.Null(product.DeletedAt);
    }

    // ---------- Category ----------

    [Fact]
    public void AssignCategory_ThenRemove_UpdatesCategoryId()
    {
        var product = CreateProduct();
        var categoryId = Guid.NewGuid();

        product.AssignCategory(categoryId);
        Assert.Equal(categoryId, product.CategoryId);

        product.RemoveCategory();
        Assert.Null(product.CategoryId);
    }
}