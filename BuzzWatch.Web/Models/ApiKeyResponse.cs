using System;

namespace BuzzWatch.Web.Models
{
    public class ApiKeyResponse
    {
        public string Key { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
} 