using Infrastructure.Data;
using Infrastructure.Repositories;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        private IProdcutRepository    _products;
        private ICategoryRepository   _category;
        private ICartRepository       _cart;
        private IOrderRepository      _order;
        private IWishListRepository   _wishLists;
        private IReviewRepository     _review;

        public UnitOfWork(AppDbContext context) => _context = context;


        public IProdcutRepository Products =>      _products ??= new ProductRepository(_context);
                                                   
        public ICategoryRepository Categories =>   _category ??= new CategoryRepository(_context);
                                                   
        public ICartRepository Carts =>            _cart ??= new CartRepository(_context);
                                                   
        public IOrderRepository Orders =>          _order??= new OrderRepository(_context);
                                                   
        public IWishListRepository Wishlists =>     _wishLists??= new WishListRepository(_context);
                                                   
        public IReviewRepository Reviews =>         _review??= new ReviewRepository(_context);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
       
           => _context.SaveChangesAsync(cancellationToken);
        
    }
}
