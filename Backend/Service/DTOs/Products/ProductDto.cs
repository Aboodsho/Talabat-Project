using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs.Products
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }=default!;
        public string? Description { get; set; }
        public Decimal Price { get; set; }
        public string? CategoryName { get; set; }
        public List<string> Images { get; set; } = new();
        public bool IsAvailable { get; set; }
    }
}
