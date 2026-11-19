using Domain.Entities;
using Domain.Tests.Helpers;
using Xunit;

namespace Domain.Tests;

public class ReviewTests
{
    [Fact]
    public void Ctor_Valid_SetsProperties()
    {
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var review = new Review(productId, customerId, 4, "Great");

        Assert.Equal(productId, review.ProductId);
        Assert.Equal(customerId, review.CustomerId);
        Assert.Equal(4, review.Rating);
        Assert.Equal("Great", review.Comment);
    }

    [Fact]
    public void Ctor_WithoutComment_AllowsNullComment()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 5);

        Assert.Null(review.Comment);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Ctor_RatingInRange_IsAccepted(int rating)
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), rating);

        Assert.Equal(rating, review.Rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Ctor_RatingOutOfRange_Throws(int rating)
        => DomainAssert.Throws("review.invalid_rating", () => new Review(Guid.NewGuid(), Guid.NewGuid(), rating));

    [Fact]
    public void SetRating_Valid_UpdatesRatingAndMarksUpdated()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 2);
        var updatedAtBefore = review.UpdatedAt;

        review.SetRating(5);

        Assert.Equal(5, review.Rating);
        Assert.NotNull(review.UpdatedAt);
        Assert.NotEqual(updatedAtBefore, review.UpdatedAt);
    }

    [Fact]
    public void SetRating_OutOfRange_ThrowsAndKeepsRating()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 3);

        DomainAssert.Throws("review.invalid_rating", () => review.SetRating(9));

        Assert.Equal(3, review.Rating);
    }

    [Fact]
    public void UpdateComment_ChangesCommentAndAllowsNull()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 4, "old");

        review.UpdateComment("new");
        Assert.Equal("new", review.Comment);

        review.UpdateComment(null);
        Assert.Null(review.Comment);
    }

    [Fact]
    public void NewEntity_HasIdAndCreatedAt()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 4);

        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.InRange(review.CreatedAt, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
        Assert.Null(review.UpdatedAt);
    }
}
