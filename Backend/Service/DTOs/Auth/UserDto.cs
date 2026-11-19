using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs.Auth
{
    public sealed record UserDto(
        Guid Id, string email, string Fullname, IReadOnlyList<string> Roles
        );
    
}
