using Core.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IWishListRepository:IRepository<Wishlist>
    {
        Task<Wishlist?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    }
}
