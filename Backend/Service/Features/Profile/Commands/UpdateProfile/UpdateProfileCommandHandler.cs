using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Profile.Commands.UpdateProfile
{
    internal class UpdateProfileCommandHandler(IProfileService profile, ICurrentUserService currentUser)
        : IRequestHandler<UpdateProfileCommand, Result>
    {
        public Task<Result> Handle(UpdateProfileCommand request, CancellationToken ct)
        {
            var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
            return profile.UpdateAsync(userId, request.FullName, request.PhoneNumber, request.NotificationChannel, ct);

        }
    }
}
