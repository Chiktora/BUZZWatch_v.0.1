namespace BuzzWatch.Contracts.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public LoginResponse() { }

        public LoginResponse(string token)
        {
            Token = token;
        }
    }
} 