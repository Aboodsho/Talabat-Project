using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Commands.Admin.AdminCancelOrder
{
    public sealed class AdminCancelOrderCommandValidator:AbstractValidator<AdminCancelOrderCommand>
    {
        public AdminCancelOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }
}
