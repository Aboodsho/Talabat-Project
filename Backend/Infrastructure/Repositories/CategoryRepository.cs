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
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context) { }
        
        public async Task<(Category? Category, IReadOnlyList<Product> Products)> GetWithProductsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var category = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (category is null)
                return (null, Array.Empty<Product>());

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => p.CategoryId == id)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return (category, products);
        }

        public async Task<IReadOnlyList<Category>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
