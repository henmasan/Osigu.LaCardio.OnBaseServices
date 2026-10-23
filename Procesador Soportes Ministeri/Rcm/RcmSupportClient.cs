using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ProcesadorSoportesMinisterio.Model;

namespace ProcesadorSoportesMinisterio.Rcm
{
    public interface IRcmSupportClient
    {
        Task<RcmUploadResult> UploadSupportAsync(RequestData request, string accessToken, string filePath);
    }

    public class RcmUploadResult
    {
        public bool Success { get; set; }
        public string RcmId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RcmSupportClient : IRcmSupportClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly RcmApiSettings _settings;

        public RcmSupportClient(RcmApiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<RcmUploadResult> UploadSupportAsync(RequestData request, string accessToken, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new RcmUploadResult { Success = false, ErrorMessage = $"File not found: {filePath}" };
                }

                var fileBytes = File.ReadAllBytes(filePath);
                var fileName = Path.GetFileName(filePath);

                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    content.Add(fileContent, "file", fileName);

                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    var requestJson = JsonConvert.SerializeObject(request, settings);
                    var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
                    content.Add(requestContent, "request_data");

                    using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}{_settings.UploadPath}"))
                    {
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        httpRequest.Content = content;

                        using (var response = await _httpClient.SendAsync(httpRequest))
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();

                            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(responseContent))
                            {
                                return new RcmUploadResult { Success = true, RcmId = "" };
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                return new RcmUploadResult { Success = false, ErrorMessage = $"Upload failed: {response.StatusCode}" };
                            }

                            var rcmResponse = JsonConvert.DeserializeObject<RcmResponse>(responseContent);
                            return new RcmUploadResult { Success = true, RcmId = rcmResponse?.Id ?? rcmResponse?.RcmId ?? "" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RcmUploadResult { Success = false, ErrorMessage = $"Upload error: {ex.Message}" };
            }
        }
    }
}
