using Core.Results;
using Service.DTOs.Auth;

namespace Service.Interfaces
{
    public interface IIdentityService
    {
        Task<Result<UserDto>> RegisterAsync(
        string email, string password, string fullName,
        CancellationToken cancellationToken = default);

        Task<Result<UserDto>> LoginAsync(
            string email, string password,
            CancellationToken cancellationToken = default);

        // Returns null if the user doesn't exist OR is already confirmed
        Task<EmailConfirmationTokenDto?> CreateEmailConfirmationTokenAsync(
            string email, CancellationToken cancellationToken = default);

        Task<Result> ConfirmEmailAsync(
            Guid userId, string encodedToken, CancellationToken cancellationToken = default);


        // Returns null if the user doesn't exist OR the email is not confirmed
        Task<PasswordResetTokenDto?> CreatePasswordResetTokenAsync(
            string email, CancellationToken cancellationToken = default);

        Task<Result> ResetPasswordAsync(
            Guid userId, string encodedToken, string newPassword, CancellationToken cancellationToken = default);



    }
}
