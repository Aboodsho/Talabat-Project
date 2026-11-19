using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.CreatProduct
{
    public  class CreateProdcutCommandValidator:AbstractValidator<CreateProdcutCommand>
    {
        public CreateProdcutCommandValidator()
        {
            RuleFor(x => x.Name)  .NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        }
    }
}
