using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Email
{
    public sealed class FrontendSettings
    {
        public const string SectionName = "Frontend";
        public string BaseUrl { get; set; } = string.Empty;
    }

}
