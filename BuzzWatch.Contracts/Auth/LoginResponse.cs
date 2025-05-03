namespace BuzzWatch.Contracts.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? Role { get; set; }

        public LoginResponse() { }

        public LoginResponse(string token, string? role = null)
        {
            Token = token;
            Role = role;
        }
    }
} 