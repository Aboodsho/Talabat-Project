using Service.DTOs.Products;

namespace Service.DTOs.Category
{
    public  class CategoryWithProductsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }=default!;
        public string? Description { get; set; }
        public List<ProductDto> Products { get; set; }=new ();
    }
}
