using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Commands.CancelMyOrder
{
    public sealed class CancelMyOrderCommandValidator:AbstractValidator<CancelMyOrderCommand>
    {
        public CancelMyOrderCommandValidator()
        {
            RuleFor(o => o.OrderId).NotEmpty();
        }
    }
}
