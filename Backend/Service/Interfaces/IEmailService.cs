using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(
            string toEmail, string fullName, Guid userId, string token,
            CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(
            string toEmail, string fullName, Guid userId, string token,
            CancellationToken cancellationToken = default);
    }
}
