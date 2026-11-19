using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities
{
    public class Order:BaseEntity
    {
        public Guid CustomerId { get; private set; }
        public OrderStatus Status { get; private set; }
        public OrderShippingAddress ShippingAddress { get; private set; }=default!;
        public decimal TotalAmount => _Items.Sum(i => i.Quantity * i.UnitPrice);

        private readonly List<OrderItem> _Items = new();
        public IReadOnlyCollection<OrderItem> Items => _Items.AsReadOnly();

        private Order() { }

        public Order(Guid cutomerid,
            IEnumerable<(Guid prodcutid,int quantity, decimal unitprice)> items,
            string street, string city, string? state, string? postalcode, string country)
        {

            if (cutomerid == Guid.Empty)
                throw new DomainException("order.invalid_customer", "CustomerId is required.");


            if (items is null || !items.Any())
                throw new DomainException("order.empty_items", "Order must contain at least one item.");
            CustomerId = cutomerid;
            Status = OrderStatus.Pending;

            foreach(var item in items)
                 _Items.Add(new OrderItem(Id, item.prodcutid, item.quantity, item.unitprice));

            ShippingAddress=new OrderShippingAddress(Id, street, city, state, postalcode, country);

        }
         public void Confirm()
        {
            if (Status!=OrderStatus.Pending)
                throw new DomainException("order.invalid_transition", $"Cannot confirm an order in '{Status}' status.");

            Status = OrderStatus.Confirmed;
            MarkAsUpdated();

        }
        public void Ship()
        {
            if (Status!=OrderStatus.Confirmed)
                throw new DomainException("order.invalid_transition", $"Cannot ship an order in '{Status}' status.");

            Status = OrderStatus.Shipped;
            MarkAsUpdated();

        }
        public void Deliver()
        {
            if (Status != OrderStatus.Shipped)
                throw new DomainException("order.invalid_transition", $"Cannot deliver an order in '{Status}' status.");

            Status = OrderStatus.Delivered;
            MarkAsUpdated();
        }

        public void Cancel()
        {
            if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
                throw new DomainException("order.invalid_transition", $"Cannot cancel an order in '{Status}' status.");

            Status = OrderStatus.Cancelled;
            MarkAsUpdated();
        }


        public void UpdateShippingAddress(string street, string city, string? state, string? postalCode, string country)
        {
            if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
                throw new DomainException("order.invalid_transition", $"Cannot update shipping address for an order in '{Status}' status.");

            ShippingAddress.Update(street, city, state, postalCode, country);
            MarkAsUpdated();
        }


    }
}
