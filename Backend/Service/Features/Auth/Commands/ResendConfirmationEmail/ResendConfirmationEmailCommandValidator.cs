using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ResendConfirmationEmail
{
    public sealed class ResendConfirmationEmailCommandValidator : AbstractValidator<ResendConfirmationEmailCommand>
    {
        public ResendConfirmationEmailCommandValidator()
            => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
