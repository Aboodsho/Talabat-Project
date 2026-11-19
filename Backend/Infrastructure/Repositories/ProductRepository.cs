using Core.Pagination;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Service.Filters;
using Service.Interfaces;

namespace Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProdcutRepository
    {

        public ProductRepository(AppDbContext context) : base(context) { }
     
        public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                               .Where(p=>p.CategoryId== categoryId)
                               .ToListAsync(cancellationToken);
        }


        public async Task<PagedResult<Product>> GetPagedAsunc(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await _dbSet.CountAsync(cancellationToken);


           var items = await _dbSet.AsNoTracking()
                                .Include(p => p.Category)
                                .OrderBy(p => p.Name)
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync(cancellationToken);


            return new PagedResult<Product>(items, pageNumber, pageSize, totalCount);


        }

        public Task<Product?> GetWithImagesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbSet.Include(p => p.Images).Include(p=>p.Category).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }


        public async Task<PagedResult<Product>> GetFilteredAsync(ProdcutFilter filter, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Include(p => p.Category).Include(p => p.Images).AsQueryable();

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(p => p.Name.Contains(filter.SearchTerm));

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Product>(items, filter.PageNumber, filter.PageSize, totalCount);

        }


        public async Task<IReadOnlyList<Product>> GetByIdsAsync( IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();

            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => idList.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Product>> GetByIdsTrackedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();

            return await _dbSet
                .Where(p => idList.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Product>> GetByIdsIncludingDeletedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();

            return await _dbSet
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => idList.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
