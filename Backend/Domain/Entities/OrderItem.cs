using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class OrderItem:BaseEntity
    {
        public Guid OrderId { get; private set; }
        public Guid ProductId { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }

        private OrderItem() { }

        internal OrderItem(Guid orderid, Guid productid, int quantity, decimal unitprice)
        {
            if (quantity <= 0)
                throw new DomainException("order_item.invalid_quantity", "Quantity must be greater than zero.");
            if (unitprice<=0)
                throw new DomainException("order_item.invalid_price", "Unit price must be greater than zero.");
            OrderId = orderid;
            ProductId = productid;
            Quantity = quantity;
            UnitPrice = unitprice;
        }




    }
}
