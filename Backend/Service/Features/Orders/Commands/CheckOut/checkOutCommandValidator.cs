using Core.Results;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Commands.CheckOut
{
    public sealed class checkOutCommandValidator:AbstractValidator<checkOutCommand>
    {
        public checkOutCommandValidator()
        {
            RuleFor(x => x.street).NotEmpty().MaximumLength(200);
            RuleFor(x => x.City).NotEmpty().MaximumLength(200);
            RuleFor(x => x.state).MaximumLength(200);
            RuleFor(x => x.PostalCode).MaximumLength(200);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(200);
        }
    }
}
