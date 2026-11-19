using Core.Constants;
using Core.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Service.DTOs.Auth;
using Service.Interfaces;
using System.Data;
using System.Text;

namespace Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private const string InvalidCredentials = "Invalid email or password.";
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        private static UserDto ToDto(ApplicationUser user, IReadOnlyList<string> roles)
            => new(user.Id, user.Email!, user.FullName, roles);



        public async Task<Result<UserDto>> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default)
        {
            var user = new ApplicationUser
            {
                Id=Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow
            };

            var createdResult = await _userManager.CreateAsync(user, password);
            if (!createdResult.Succeeded)
            {
                return Result<UserDto>.Failure(
                    string.Join(", ", createdResult.Errors.Select(e => e.Description))
                    );
            }

            var roleResult = await _userManager.AddToRoleAsync(user, Role.User);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return Result<UserDto>.Failure(
                    string.Join(", ", roleResult.Errors.Select(e => e.Description))
                    );
            }

            return Result<UserDto>.Success(ToDto(user, new[] { Role.User }));


        }


        public async Task<Result<UserDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if(user is null)
                return Result<UserDto>.Failure(InvalidCredentials);

            if(await _userManager.IsLockedOutAsync(user))
                return Result<UserDto>.Failure("Account is locked. Try again later.");  

            if(! await _userManager.CheckPasswordAsync(user,password))
            {
                await _userManager.AccessFailedAsync(user);
                return Result<UserDto>.Failure(InvalidCredentials);
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            if (!user.EmailConfirmed)
                return Result<UserDto>.Failure(AuthErrors.EmailNotConfirmed);

            var roles = await _userManager.GetRolesAsync(user);
            return Result<UserDto>.Success(ToDto(user, roles.ToList()));

        }


        public async Task<EmailConfirmationTokenDto?> CreateEmailConfirmationTokenAsync(
             string email, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || user.EmailConfirmed)
                return null;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));   // URL-safe

            return new EmailConfirmationTokenDto(user.Id, user.Email!, user.UserName!, encoded);   // UserName = full name
        }

        public async Task<Result> ConfirmEmailAsync(
            Guid userId, string encodedToken, CancellationToken cancellationToken = default)
        {
            const string invalid = "Invalid or expired confirmation link.";

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Result.Failure(invalid);                 // same message: no user enumeration

            if (user.EmailConfirmed)
                return Result.Success();                        // idempotent: second click is fine

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            }
            catch (FormatException)
            {
                return Result.Failure(invalid);
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded ? Result.Success() : Result.Failure(invalid);
        }


        public async Task<PasswordResetTokenDto?> CreatePasswordResetTokenAsync(
    string email, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || !user.EmailConfirmed)
                return null;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            return new PasswordResetTokenDto(user.Id, user.Email!, user.UserName!, encoded);   // UserName = full name
        }

        public async Task<Result> ResetPasswordAsync(
            Guid userId, string encodedToken, string newPassword, CancellationToken cancellationToken = default)
        {
            const string invalid = "Invalid or expired reset link.";

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Result.Failure(invalid);

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            }
            catch (FormatException)
            {
                return Result.Failure(invalid);
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                    return Result.Failure(invalid);

                // Password policy errors (too short, needs digit...) are safe to show
                return Result.Failure(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            // A user locked out by forgotten-password attempts can log in right away
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            return Result.Success();
        }
    }
}
