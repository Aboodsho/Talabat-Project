using Domain.Entities;
using Service.Interfaces;

namespace Service.Features.Orders
{
    internal static class OrderStockRestorer
    {
        public static async Task RestoreAsync(IUnitOfWork uow, Order order, CancellationToken ct) 
        {
            var productIds= order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = (await uow.Products.GetByIdsTrackedAsync(productIds, ct)).ToDictionary(p=>p.Id);

            foreach (var item in order.Items)
            { 
                if(products.TryGetValue(item.ProductId,out var product))
                    product.IncreaseStock(item.Quantity);
            }


        }
    }
}
