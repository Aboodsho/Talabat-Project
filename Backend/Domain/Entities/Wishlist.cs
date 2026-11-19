
using Domain.Exceptions;

namespace Domain.Entities
{
    public class Wishlist:BaseEntity
    {
        public const int MaxItems = 50;

        public Guid CustomerId { get; private set; }
        private readonly List<WishListItem> _wishListItems = new (); //Encapsulation 
        public IReadOnlyCollection<WishListItem> WishListItems => _wishListItems.AsReadOnly();

        private Wishlist() { }

        public Wishlist(Guid customerId)
        {
            if (customerId == Guid.Empty)
                throw new DomainException("wishlist.invalid_customer", "CustomerId is required.");
            CustomerId = customerId;
        }

        public void AddItem(Guid productId)
        {
            if (_wishListItems.Any(i => i.ProductId == productId)) return;

            if (_wishListItems.Count >= MaxItems)
                throw new DomainException("wishlist.max_items_exceeded",
                    $"Wishlist cannot contain more than {MaxItems} items.");

            _wishListItems.Add( new WishListItem(this.Id, productId));
          
            MarkAsUpdated();
        }

        public void RemoveItem(Guid productId)
        {
            var item = _wishListItems.FirstOrDefault(i => i.ProductId == productId);
            if (item is null) return;

            _wishListItems.Remove(item);
            MarkAsUpdated();
        }

        public void Clear()
        {
            _wishListItems.Clear();
            MarkAsUpdated();
        }
    }
}
