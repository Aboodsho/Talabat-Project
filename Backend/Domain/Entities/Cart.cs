using Domain.Exceptions;

namespace Domain.Entities
{
    public class Cart:BaseEntity
    {
        public Guid CustomerId { get; private set; }
        private readonly List<CartItem> _cartItems = new List<CartItem>();
        public IReadOnlyCollection<CartItem> CartItems => _cartItems.AsReadOnly();

        private Cart() { }
        public Cart(Guid customerId)
        {
            if (customerId == Guid.Empty)
                throw new DomainException("cart.invalid_customer", "CustomerId is required.");
            CustomerId = customerId;
        }

        public void AddCartItem(Guid ProductId,int quantity , int availableStock) 
        {
            var existingItem = _cartItems.FirstOrDefault(i => i.ProductId == ProductId);
            if(existingItem is not null) 
            {
                existingItem.IncreaseQuantity(quantity, availableStock);
            }
            else
            {
                _cartItems.Add(new CartItem(this.Id, ProductId, quantity, availableStock));
            }
            MarkAsUpdated();
        }

        //public void IncreaseItemQuantity(Guid productId,int quantity)
        //{
        //    var item=_cartItems.FirstOrDefault(i=>i.ProductId == productId)
        //    ?? throw new DomainException("Cart_Item.not_found","Item not found in cart.");

        //    item.IncreaseQuantity(quantity, availableStock);
        //    MarkAsUpdated();

        //}
        public void DecreaseItemQuantity(Guid productId, int amount = 1)
        {
            var item = _cartItems.FirstOrDefault(i => i.ProductId == productId)
                ?? throw new DomainException("Cart_Item.not_found", "Item not found in cart.");

            if (amount >= item.Quantity)
            {
                _cartItems.Remove(item); 
            }
            else
            {
                item.DecreaseQuantity(amount);
            }

            MarkAsUpdated();
        }

        public void RemoveItem(Guid productId)
        {
            var item = _cartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item is null) return;

            _cartItems.Remove(item);
            MarkAsUpdated();
        }

        public void Clear()
        {
            _cartItems.Clear();
            MarkAsUpdated();
        }
    }
}
