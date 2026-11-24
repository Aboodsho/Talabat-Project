using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationChannel
    {
        None = 0,
        Sms = 1,
        WhatsApp = 2
    }
}
