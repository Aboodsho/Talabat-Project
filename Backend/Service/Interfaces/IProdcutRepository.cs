using Core.Interfaces;
using Core.Pagination;
using Domain.Entities;
using Service.Filters;

namespace Service.Interfaces
{
    public interface IProdcutRepository : IRepository<Product>
    {
        Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
        Task<Product?> GetWithImagesAsync(Guid id, CancellationToken cancellationToken = default);

        Task<PagedResult<Product>> GetPagedAsunc(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<PagedResult<Product>> GetFilteredAsync(ProdcutFilter filter, CancellationToken cancellationToken = default);

        // Cart / Wishlist display: no tracking, hides deleted products
        Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        // Checkout: tracked (stock will be modified)
        Task<IReadOnlyList<Product>> GetByIdsTrackedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        // Order display: includes soft-deleted products (to show their names)
        Task<IReadOnlyList<Product>> GetByIdsIncludingDeletedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
