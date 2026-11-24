using Core.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Profile.Commands.UpdateProfile
{
    public sealed class UpdateProfileCommandValidator:AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(50);

            // E.164: "+" + country code + number. Example: +201001234567
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+[1-9]\d{7,14}$")
                .WithMessage("Phone number must be in international format, e.g. +201001234567.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

            RuleFor(x => x.NotificationChannel).IsInEnum();

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("A phone number is required to receive SMS or WhatsApp notifications.")
                .When(x => x.NotificationChannel != NotificationChannel.None);
        }
    }
}
