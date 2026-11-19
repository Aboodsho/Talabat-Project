using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class WishListItem:BaseEntity
    {
        public Guid WishListId { get; private set; }
        public Guid ProductId { get; private set; }
        private WishListItem() { }
        internal WishListItem(Guid wishListId, Guid productId)
        {
            WishListId = wishListId;
            ProductId = productId;
        }
    }
}
