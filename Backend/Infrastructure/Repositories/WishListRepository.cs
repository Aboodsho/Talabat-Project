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
    public class WishListRepository : GenericRepository<Wishlist>, IWishListRepository
    {
        public WishListRepository(AppDbContext context) : base(context) { }

        public async Task<Wishlist?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
           return await _dbSet.Include(w=>w.WishListItems)
                              .FirstOrDefaultAsync(w => w.CustomerId == customerId, cancellationToken);
        }
    }
}
