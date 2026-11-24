using Core.Results;
using MediatR;
using Service.DTOs.Profile;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Profile.Queries.GetProfile
{
    public sealed class GetProfileQueryHandler(IProfileService profile, ICurrentUserService currentUser)
        : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
    {
        public Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken ct)
        {
            var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
            return profile.GetAsync(userId, ct);
        }
    }
}
