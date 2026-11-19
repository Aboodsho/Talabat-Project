using Core.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ICategoryRepository:IRepository<Category>
    {

        Task<(Category? Category, IReadOnlyList<Product> Products)> GetWithProductsAsync(
            Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Category>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
    }
}
