using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs.Products
{
    public class AdminProdcutDto
    {
        public Guid Id { get; set; }
        public string Name  { get; set; }=default!;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public int  StockQuantity { get; set; }
        public bool  IsActive { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<string> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


    }
}
