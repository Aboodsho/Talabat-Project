using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IUnitOfWork
    {
        IProdcutRepository Products { get; }
        ICategoryRepository Categories { get; }
        ICartRepository Carts { get; }
        IOrderRepository Orders { get; }
        IWishListRepository Wishlists { get; }
        IReviewRepository Reviews { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
