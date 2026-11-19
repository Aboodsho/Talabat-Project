using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ProductImage:BaseEntity
    {
        public string ImageUrl { get; private set; }=default!;
        public bool IsMain { get; private set; }
        public Guid ProductId { get; private set; }
        private ProductImage() { }
        internal ProductImage(string imageUrl, Guid productId, bool isMain=false)
        {
            SetUrl(imageUrl);
            ProductId = productId;
            if(isMain) SetAsMain();


        }
        public void SetUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                throw new DomainException("Product_Image.URL.not_found", "Image URL is required.");
            ImageUrl = imageUrl;
            MarkAsUpdated();
        }
        internal void SetAsMain() => IsMain = true;
        internal void UnsetMain() => IsMain = false;

    }
}
