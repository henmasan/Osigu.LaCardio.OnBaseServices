using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public interface IRcmAuthClient
    {
        Task<RcmAuthToken> GetTokenAsync();
    }

    public class RcmAuthClient : IRcmAuthClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly RcmApiSettings _settings;
        private RcmAuthToken _cachedToken;
        private DateTime _tokenExpiresAt = DateTime.MinValue.AddDays(1);
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public RcmAuthClient(RcmApiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<RcmAuthToken> GetTokenAsync()
        {
            if (DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-30))
            {
                return _cachedToken;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-30))
                {
                    return _cachedToken;
                }

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
                using (var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}{_settings.AuthPath}"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    var content = new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("grant_type", "client_credentials")
                    });
                    request.Content = content;

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"RCM authentication failed: {response.StatusCode}");
                        }

                        var responseContent = await response.Content.ReadAsStringAsync();
                        _cachedToken = JsonConvert.DeserializeObject<RcmAuthToken>(responseContent);

                        var expiresIn = _settings.TokenExpiration > 0 ? _settings.TokenExpiration : _cachedToken.ExpiresIn;
                        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

                        return _cachedToken;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
