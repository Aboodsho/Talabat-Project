global using Core.Results;
global using Core.Pagination;

global using Service.Interfaces;
global using Service.DTOs.Products;
global using Service.Filters;

global using Service.Features.Products.Commands.CreatProduct;
global using Service.Features.Products.Commands.UpdateProduct;
global using Service.Features.Products.Commands.UpdateProductCategory;
global using Service.Features.Products.Commands.UpdateProductStock;
global using Service.Features.Products.Commands.SetProductStatus;
global using Service.Features.Products.Commands.DeleteProduct;

global using Service.Features.Products.Queries.User.GetProductById;
global using Service.Features.Products.Queries.User.GetProducts;
global using Service.Features.Products.Queries.Admin.GetAdminProductById;
global using Service.Features.Products.Queries.Admin.GetAdminProducts;

global using Infrastructure.Data;