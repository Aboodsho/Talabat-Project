using Core.Results;
using MediatR;
using Service.DTOs.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Profile.Queries.GetProfile
{
    public sealed record GetProfileQuery : IRequest<Result<ProfileDto>>;

}
