using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.OnBaseServices.GetSupportSettlement.Model.RCMResponse;
using Osigu.OnBaseServices.GetSupportSettlement.Model.Request;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure
{
    public class RcmSupportClient : IRcmSupportClient
    {
        private readonly HttpClient _httpClient;
        private readonly RcmApiSettings _rcmSettings;
        private readonly ILogger<RcmSupportClient> _logger;

        public RcmSupportClient(HttpClient httpClient, RcmApiSettings rcmSettings, ILogger<RcmSupportClient> logger)
        {
            _httpClient = httpClient;
            _rcmSettings = rcmSettings;
            _logger = logger;
        }

        public async Task<RCMUploadResponse> UploadSupportAsync(RequestData request, string accessToken)
        {
            try
            {
                if (!File.Exists(request.SupportFileMetadata.FilePath))
                {
                    _logger.LogError($"File not found: {request.SupportFileMetadata.FilePath}");
                    //return new RCMUploadResponse
                    //{
                    //    Success = false,
                    //    Message = "File not found"
                    //};
                }

                var uploadUrl = $"{_rcmSettings.BaseUrl}{_rcmSettings.UploadPath}";
                using (var multipartContent = new MultipartFormDataContent())
                {
                    // Add file
                    var fileBytes = File.ReadAllBytes(request.SupportFileMetadata.FilePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.Add("Content-Type", "application/octet-stream");
                    multipartContent.Add(fileContent, "file", Path.GetFileName(request.SupportFileMetadata.FilePath));

                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new SnakeCaseNamingStrategy()
                        }
                    };

                    // Serializar
                    string requestDataJson = JsonConvert.SerializeObject(request, settings);

                    var jsonContent = new StringContent(requestDataJson, Encoding.UTF8, "application/json");
                    multipartContent.Add(jsonContent, "request_data");


                    using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl))
                    {
                        httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                        httpRequest.Content = multipartContent;

                        using (var response = await _httpClient.SendAsync(httpRequest))
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();

                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogError($"RCM upload failed with status {response.StatusCode}: {responseContent}");
                                return new RCMUploadResponse
                                {
                                    Success = false,
                                    Message = $"Upload failed: {response.StatusCode}"
                                };
                            }

                            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(responseContent))
                            {
                                _logger.LogInformation($"RCM upload successful (HTTP {(int)response.StatusCode})");
                                return new RCMUploadResponse
                                {
                                    Success = true,
                                    Message = "File uploaded successfully",
                                    RcmId = string.Empty
                                };
                            }

                            dynamic rcmResponse = JsonConvert.DeserializeObject(responseContent);
                            return new RCMUploadResponse
                            {
                                Success = true,
                                Message = "File uploaded successfully",
                                RcmId = rcmResponse?.id ?? rcmResponse?.rcm_id ?? ""
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading support to RCM");
                return new RCMUploadResponse
                {
                    Success = false,
                    Message = $"Upload error: {ex.Message}"
                };
            }
        }
    }
}
