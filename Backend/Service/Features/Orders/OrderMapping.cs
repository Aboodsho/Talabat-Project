using Domain.Entities;
using Service.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders
{
    internal static class OrderMapping
    {
        public static OrderSummaryDto ToSummaryDto(this Order order)
            => new(order.Id, order.Status.ToString(), order.CreatedAt, order.Items.Count, order.TotalAmount);


        public static OrderDto ToDto(this Order order, IReadOnlyDictionary<Guid, string> productNames)
        {
            var items = order.Items
               .Select(i => new OrderItemDto(
                   i.ProductId,
                   productNames.TryGetValue(i.ProductId, out var name) ? name : "Unknown product",
                   i.Quantity,
                   i.UnitPrice,
                   i.Quantity * i.UnitPrice))
               .ToList();

            var a = order.ShippingAddress;

            return new OrderDto(
                order.Id,
                order.Status.ToString(),
                order.CreatedAt,
                new ShippingAddressDto(a.Street, a.City, a.State, a.PostalCode, a.Country),
                items,
                order.TotalAmount);


        }
    }
}
