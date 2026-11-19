using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Identity
{
    public sealed class UserLookupService(UserManager<ApplicationUser> userManager) : IUserLookupService
    {
        public async Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(
            IEnumerable<Guid> userIds, CancellationToken ct = default)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, string>();

            // Names only. Never email: reviews are public.
            return await userManager.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.UserName! })     // swap to u.FullName if you have it
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        }
    }
}
