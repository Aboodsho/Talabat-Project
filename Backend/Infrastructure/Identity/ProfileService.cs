using Core.Enums;
using Core.Results;
using Microsoft.AspNetCore.Identity;
using Service.DTOs.Profile;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Identity
{
    public sealed class ProfileService(UserManager<ApplicationUser> userManager) : IProfileService
    {
        private const string UserNotFound = "User not found.";

        public async Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Result<ProfileDto>.Failure(UserNotFound);

            var roles = await userManager.GetRolesAsync(user);

            return Result<ProfileDto>.Success(new ProfileDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.PhoneNumber,
                user.NotificationChannel.ToString(),
                user.CreatedAt,
                roles.ToList()));
        }

        public async Task<Result> UpdateAsync(
                        Guid userId,
                        string fullName,
                        string? phoneNumber,
                        NotificationChannel channel,
                        CancellationToken cancellationToken = default)
        {
            // 1. Normalize
            fullName = fullName?.Trim() ?? string.Empty;
            phoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

            // 2. Validate (no DB needed)
            if (fullName.Length == 0)
                return Result.Failure("Full name is required.");

            if (!Enum.IsDefined(channel))
                return Result.Failure("Invalid notification channel.");

            // 3. Load
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Result.Failure(UserNotFound);

            // 4. Apply
            user.FullName = fullName;

            if (!string.Equals(user.PhoneNumber, phoneNumber, StringComparison.Ordinal))
            {
                user.PhoneNumber = phoneNumber;
                user.PhoneNumberConfirmed = false;
            }

            user.NotificationChannel = phoneNumber is null ? NotificationChannel.None : channel;

            // 5. Save
            var result = await userManager.UpdateAsync(user);

            return result.Succeeded
                ? Result.Success()
                : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
