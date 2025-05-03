using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BuzzWatch.Web.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<T>(endpoint, _jsonOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from {Endpoint}", endpoint);
                return default;
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.GetAsync(endpoint, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting data to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting JSON data to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error putting JSON data to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                return await _httpClient.PutAsync(endpoint, content, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error putting data to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClient.DeleteAsync(endpoint, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting data from {Endpoint}", endpoint);
                throw;
            }
        }
    }

    public class JwtDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<JwtDelegatingHandler> _logger;

        public JwtDelegatingHandler(IHttpContextAccessor httpContextAccessor, ILogger<JwtDelegatingHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Log the request URL
            _logger.LogInformation("Preparing request to {RequestUri}", request.RequestUri);
            
            // Get token from session
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Found JWT token in session, adding to request");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("No JWT token found in session for request to {RequestUri}", request.RequestUri);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
} 