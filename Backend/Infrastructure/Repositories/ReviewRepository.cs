using Core.Pagination;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return   await _dbSet .AsNoTracking()
                .Where(r => r.ProductId==productId)
                .ToListAsync(cancellationToken);
        }


        // Tracked: used for duplicate check, update, delete
        public async Task<Review?> GetByProductAndCustomerAsync(Guid productId, Guid customerId, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customerId, ct);
        }

        public async Task<PagedResult<Review>> GetPagedByProductAsync(
            Guid productId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking().Where(r => r.ProductId == productId);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<Review>(items, pageNumber, pageSize, totalCount);
        }

        public async Task<double> GetAverageRatingAsync(Guid productId, CancellationToken ct = default)
        {
            // (double?) so an empty set returns null instead of throwing
            var avg = await _dbSet.AsNoTracking()
                .Where(r => r.ProductId == productId)
                .AverageAsync(r => (double?)r.Rating, ct);

            return avg.HasValue ? Math.Round(avg.Value, 1) : 0;
        }




    }
}
