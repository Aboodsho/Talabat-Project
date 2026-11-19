using Core.Pagination;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;

namespace Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context) { }
   
        public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                               .Where(o => o.CustomerId == customerId)
                               .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasCustomerPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                               .AnyAsync(o => o.CustomerId == customerId
                               && o.Status == OrderStatus.Delivered
                               && o.Items.Any(i => i.ProductId == productId), cancellationToken);
        }


        public async Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.Items)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);   // tracked: used for Cancel/status changes
                
        }

        // admin
        public async Task<PagedResult<Order>> GetPagedAsync(OrderStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {

            var query = _dbSet.AsNoTracking().AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Order>(items, pageNumber, pageSize, totalCount);


        }

        public async Task<PagedResult<Order>> GetPagedByCustomerAsync(Guid customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(o => o.CustomerId == customerId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(o => o.Items)                    // needed for TotalAmount
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Order>(items, pageNumber, pageSize, totalCount);
        }

       
    }
}
