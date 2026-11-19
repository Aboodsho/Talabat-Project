using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Email
{
    public sealed class SmtpSettings
    {
        public const string SectionName = "Email:Smtp";

        [Required] public string Host { get; set; } = string.Empty;
        [Range(1, 65535)] public int Port { get; set; } = 587;
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Required, EmailAddress] public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "E-Commerce";
    }
}
