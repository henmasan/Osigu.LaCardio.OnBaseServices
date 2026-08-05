using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure
{
    public class RcmAuthClient : IRcmAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly RcmApiSettings _rcmSettings;
        private readonly ILogger<RcmAuthClient> _logger;
        private RcmAuthToken _cachedToken;
        private DateTime _tokenExpiresAt = DateTime.MinValue;
        private readonly object _lockObject = new object();

        public RcmAuthClient(HttpClient httpClient, RcmApiSettings rcmSettings, ILogger<RcmAuthClient> logger)
        {
            _httpClient = httpClient;
            _rcmSettings = rcmSettings;
            _logger = logger;
        }

        public async Task<RcmAuthToken> GetTokenAsync()
        {
            lock (_lockObject)
            {
                if (_cachedToken != null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-30))
                {
                    _logger.LogInformation("Using cached RCM auth token");
                    return _cachedToken;
                }
            }

            try
            {
                var authUrl = $"{_rcmSettings.BaseUrl}{_rcmSettings.AuthPath}";
                var request = new HttpRequestMessage(HttpMethod.Post, authUrl);

                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_rcmSettings.ClientId}:{_rcmSettings.ClientSecret}")
                );
                request.Headers.Add("Authorization", $"Basic {credentials}");

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });
                request.Content = content;

                using (var response = await _httpClient.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"RCM auth failed with status {response.StatusCode}: {errorContent}");
                        throw new HttpRequestException($"RCM authentication failed: {response.StatusCode}");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    dynamic tokenResponse = JsonConvert.DeserializeObject(responseContent);

                    lock (_lockObject)
                    {
                        _cachedToken = new RcmAuthToken
                        {
                            AccessToken = tokenResponse.access_token,
                            ExpiresIn = tokenResponse.expires_in,
                            TokenType = tokenResponse.token_type
                        };

                        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(_cachedToken.ExpiresIn);
                    }

                    _logger.LogInformation($"RCM auth token obtained successfully, expires in {_cachedToken.ExpiresIn} seconds");
                    return _cachedToken;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining RCM auth token");
                throw;
            }
        }
    }
}
