using Core.Enums;
using Core.Results;
using Service.DTOs.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IProfileService
    {
        Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> UpdateAsync(
            Guid userId, string fullName, string? phoneNumber, NotificationChannel channel,
            CancellationToken cancellationToken = default);
    }
}
