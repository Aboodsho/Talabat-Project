using Core.Interfaces;
using Core.Pagination;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IOrderRepository:IRepository<Order>
    {
        Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<bool> HasCustomerPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);


        Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<PagedResult<Order>> GetPagedByCustomerAsync(
            Guid customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<PagedResult<Order>> GetPagedAsync(
            OrderStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);



    }
}
