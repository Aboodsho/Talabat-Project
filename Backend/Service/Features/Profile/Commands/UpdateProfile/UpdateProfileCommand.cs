using Core.Enums;
using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Profile.Commands.UpdateProfile
{
    public sealed record UpdateProfileCommand(
        string FullName,
        string? PhoneNumber,
        NotificationChannel NotificationChannel) : IRequest<Result>;
}
