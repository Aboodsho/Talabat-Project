using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs.Auth
{
    public sealed record EmailConfirmationTokenDto(Guid UserId, string Email, string FullName, string Token);
}
