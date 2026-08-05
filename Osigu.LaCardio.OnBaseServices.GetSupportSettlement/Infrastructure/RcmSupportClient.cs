using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
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

        public async Task<RcmUploadResponse> UploadSupportAsync(RcmUploadRequest request, string accessToken)
        {
            try
            {
                if (!File.Exists(request.FilePath))
                {
                    _logger.LogError($"File not found: {request.FilePath}");
                    return new RcmUploadResponse
                    {
                        Success = false,
                        Message = "File not found"
                    };
                }

                var uploadUrl = $"{_rcmSettings.BaseUrl}{_rcmSettings.UploadPath}";
                using (var multipartContent = new MultipartFormDataContent())
                {
                    // Add file
                    var fileBytes = File.ReadAllBytes(request.FilePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.Add("Content-Type", "application/octet-stream");
                    multipartContent.Add(fileContent, "file", Path.GetFileName(request.FilePath));

                    // Add metadata
                    multipartContent.Add(new StringContent(request.InvoiceNumber), "invoice_number");
                    multipartContent.Add(new StringContent(request.SupportFileCode), "support_file_code");
                    multipartContent.Add(new StringContent(request.AgreementCode), "agreement_code");
                    multipartContent.Add(new StringContent(request.DocumentType), "document_type");

                    if (!string.IsNullOrEmpty(request.OriginEventId))
                        multipartContent.Add(new StringContent(request.OriginEventId), "origin_event_id");

                    if (!string.IsNullOrEmpty(request.ProcessId))
                        multipartContent.Add(new StringContent(request.ProcessId), "process_id");

                    if (!string.IsNullOrEmpty(request.InvoiceElectronicCode))
                        multipartContent.Add(new StringContent(request.InvoiceElectronicCode), "invoice_electronic_code");

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
                                return new RcmUploadResponse
                                {
                                    Success = false,
                                    Message = $"Upload failed: {response.StatusCode}"
                                };
                            }

                            dynamic rcmResponse = JsonConvert.DeserializeObject(responseContent);
                            return new RcmUploadResponse
                            {
                                Success = true,
                                Message = "File uploaded successfully",
                                RcmId = rcmResponse.id ?? rcmResponse.rcm_id ?? ""
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading support to RCM");
                return new RcmUploadResponse
                {
                    Success = false,
                    Message = $"Upload error: {ex.Message}"
                };
            }
        }
    }
}
