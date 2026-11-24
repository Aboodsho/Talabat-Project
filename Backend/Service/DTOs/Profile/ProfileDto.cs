using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs.Profile
{
    public sealed record ProfileDto(
        Guid Id,
        string Email,
        string FullName,
        string? PhoneNumber,
        string NotificationChannel,   
        DateTime CreatedAt,
        IReadOnlyList<string> Roles);
}
