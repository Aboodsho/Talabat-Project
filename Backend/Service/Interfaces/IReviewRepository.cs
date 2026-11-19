using Core.Interfaces;
using Core.Pagination;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IReviewRepository: IRepository<Review>
    {
        Task<IReadOnlyList<Review>> GetByProductIdAsync(
            Guid productId, CancellationToken cancellationToken = default);
        Task<Review?> GetByProductAndCustomerAsync(
            Guid productId, Guid customerId, CancellationToken cancellationToken = default);

        Task<PagedResult<Review>> GetPagedByProductAsync(
            Guid productId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<double> GetAverageRatingAsync(
            Guid productId, CancellationToken cancellationToken = default);
    }
}
