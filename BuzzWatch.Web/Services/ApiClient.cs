using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BuzzWatch.Web.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<T>(endpoint, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from {Endpoint}", endpoint);
                return default;
            }
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.PostAsJsonAsync(endpoint, data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting data to {Endpoint}", endpoint);
                throw;
            }
        }
    }

    public class JwtDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Get token from session
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
} 