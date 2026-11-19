using Domain.Exceptions;

namespace Domain.Entities
{
    public class CartItem:BaseEntity
    {
        public const int MaxQuantity = 10;

        public Guid CartId { get; private set; }
        public Guid ProductId { get; private set; }
        public int Quantity { get; private set; }
        private CartItem() { }
        internal CartItem(Guid cartId, Guid productId, int quantity, int availableStock)
        {
            CartId = cartId;
            ProductId = productId;
            IncreaseQuantity(quantity, availableStock);
        }
       
        internal void IncreaseQuantity(int amount, int availableStock)
        {
            if (amount <= 0)
                throw new DomainException("cart_item.invalid_amount", "Amount must be greater than zero.");

            var newQuantity = Quantity + amount;

            if (newQuantity > availableStock)
                throw new DomainException("cart_item.insufficient_stock",
                    $"Only {availableStock} unit(s) available, You already have {Quantity} in your cart.");

            if (newQuantity > MaxQuantity)
                throw new DomainException("cart_item.max_quantity_exceeded",
                    $"Cannot exceed {MaxQuantity} units of the same product.");

            Quantity = newQuantity;
            MarkAsUpdated();
        }

        internal void DecreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new DomainException("cart_item.invalid_amount", "Amount must be greater than zero.");
            if (amount>=Quantity )
                throw new DomainException("cart_item.quantity_below_one", "Quantity cannot go below one this way — remove the item instead.");
            Quantity -= amount;
            MarkAsUpdated();
        }
    }
}
