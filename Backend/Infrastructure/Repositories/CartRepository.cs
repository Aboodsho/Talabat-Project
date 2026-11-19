using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;

namespace Infrastructure.Repositories
{
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context) { }
        

        public async Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Include(c => c.CartItems)
                               .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
                
        }
    }
}
