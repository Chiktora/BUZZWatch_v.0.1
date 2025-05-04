using System;

namespace BuzzWatch.Contracts.Admin
{
    /// <summary>
    /// Response model for API key generation
    /// </summary>
    public class ApiKeyResponse
    {
        /// <summary>
        /// The API key for device authentication
        /// </summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// When the API key expires
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }
    }
} 