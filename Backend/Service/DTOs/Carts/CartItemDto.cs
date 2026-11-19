using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs.Carts
{
    public sealed record CartItemDto(
        Guid ProductId,
        string ProductName,
        string? ImageUrl,
        decimal UnitPrice,
        int Quantity,
        decimal LineTotal,
        int AvailableStock,
        bool IsAvailable
        );

    public sealed record CartDto(
        IReadOnlyList<CartItemDto> Items,
        decimal TotalPrice
        );


}
