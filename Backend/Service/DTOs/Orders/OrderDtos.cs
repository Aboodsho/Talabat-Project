namespace Service.DTOs.Orders
{
   public sealed record ShippingAddressDto(
       string Street, string City, string? State, string? PostalCode, string Country);

    public sealed record OrderItemDto(
        Guid ProductId, string ProdcutName, int Quantity, decimal UnitPrice, 
        decimal LineTotal);

    public sealed record OrderSummaryDto(
       Guid Id, string Status, DateTime CreatedAt,int ItemsCount, decimal TotalAmount);

    public sealed record OrderDto(
       Guid Id, string Status, DateTime CreatedAt, ShippingAddressDto ShippingAddress,
       IReadOnlyList<OrderItemDto> Items,
       decimal TotalAmount);

}
