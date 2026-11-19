namespace Service.DTOs.Wishlists
{
    public sealed record WishlistItemDto(
         Guid ProductId,
         string ProductName,
         string? ImageUrl,
         decimal Price,
         bool IsAvailable,
         DateTime AddedAt);

    public sealed record WishlistDto(
        IReadOnlyList<WishlistItemDto> Items,
        int Count);
}
