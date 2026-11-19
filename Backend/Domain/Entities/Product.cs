using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Domain.Entities
{
    public class Product:BaseEntity
    {
        public string Name { get; private set; }=default!;
        public string? Description { get; private set; }
        public  decimal Price { get; private set; }
        public int StockQuantity { get; private set; }
        public bool IsActive { get; private set; }=true;
        public DateTime? DeletedAt { get; private set; }

        public bool IsDeleted => DeletedAt is not null;

        //----------------------------------------------------------------
        public Guid? CategoryId { get; private set; }
        public Category? Category { get; private set; }

        //----------------------------------------------------------------

        private readonly List<ProductImage> _images = new();
        public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();


        //----------------------------------------------------------------
        

        private Product() { }

        public Product(string name, string? description, decimal price, int stockQuantity, Guid? categoryId)
        {
           SetName(name);
           SetPrice(price);
           
           Description = description;
           SetStock(stockQuantity); 
           CategoryId = categoryId;

        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("product.name_required", "Product name is required.");
            this.Name = name.Trim();
            MarkAsUpdated();
        }

        public void SetPrice(decimal price)
        {
            if (price <= 0)
                throw new DomainException("product.invalid_price", "Product price must be greater than zero.");
            this.Price = price;
            MarkAsUpdated();
        }
        public void SetStock(int quantity)
        {
            if (quantity < 0)
                throw new DomainException("product.negative_stock", "Stock quantity cannot be negative.");
            this.StockQuantity = quantity;
            MarkAsUpdated();
        }

        public void IncreaseStock(int amount)
        {
            if (amount <= 0)
                throw new DomainException("product.invalid_amount", "Amount must be greater than zero.");

            StockQuantity += amount;
            MarkAsUpdated();
        }
        public void DecreaseStock(int amount)
        {
            if (amount <= 0)
                throw new DomainException("product.invalid_amount", "Amount must be greater than zero.");

            if (amount > StockQuantity)
                throw new DomainException("product.insufficient_stock", $"Cannot decrease stock by {amount}; only {StockQuantity} available.");

            StockQuantity -= amount;
            MarkAsUpdated();
        }

        public void UpdateDescription(string? description)
        {
            this.Description = description;
            MarkAsUpdated();
        }

        public void AssignCategory(Guid categoryId)
        {
            this.CategoryId = categoryId;
            MarkAsUpdated();
        }

        public void RemoveCategory()
        {
            this.CategoryId = null;
            MarkAsUpdated();
        }

        public void Activate()
        {
            IsActive = true;
            MarkAsUpdated();
        }

        public void Deactivate()
        {
            IsActive = false;
            MarkAsUpdated();
        }

        public void Delete()
        {
            if (IsDeleted) return;
            DeletedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            DeletedAt = null;
            MarkAsUpdated();
        }

        public ProductImage AddImage(string imageUrl, bool isMain = false)
        {
            var image = new ProductImage(imageUrl, this.Id);
            if (isMain)
            {
                foreach (var img in _images)
                {
                    img.UnsetMain();
                }
                image.SetAsMain();
            }
            _images.Add(image);
            MarkAsUpdated();
            return image;
        }

        public void RemoveImage(Guid imageid)
        {
            var image =_images.FirstOrDefault(i => i.Id == imageid);
            if (image is null) return;
            _images.Remove(image);
            MarkAsUpdated();
        }
    }
}
