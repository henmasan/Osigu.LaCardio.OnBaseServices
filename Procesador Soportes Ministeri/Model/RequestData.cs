using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProcesadorSoportesMinisterio.Model
{
    public class RequestData
    {
        [JsonProperty("support_file_code")]
        public string SupportFileCode { get; set; }

        [JsonProperty("events")]
        public List<EventInfo> Events { get; set; }

        [JsonProperty("support_file_metadata")]
        public SupportFileMetadata SupportFileMetadata { get; set; }

        [JsonProperty("ruta_fisica")]
        public string RutaFisica { get; set; }
    }

    public class EventInfo
    {
        [JsonProperty("origin_event_id")]
        public string OriginEventId { get; set; }

        [JsonProperty("agreement_code")]
        public string AgreementCode { get; set; }
    }

    public class SupportFileMetadata
    {
        [JsonProperty("process_id")]
        public string ProcessId { get; set; }

        [JsonProperty("document_type")]
        public string DocumentType { get; set; }

        [JsonProperty("agreement_date")]
        public string AgreementDate { get; set; }

        [JsonProperty("invoice_amount")]
        public decimal InvoiceAmount { get; set; }

        [JsonProperty("invoice_number")]
        public string InvoiceNumber { get; set; }

        [JsonProperty("invoice_date_time")]
        public DateTime? InvoiceDateTime { get; set; }

        [JsonProperty("document_type_number")]
        public string DocumentTypeNumber { get; set; }

        [JsonProperty("invoice_electronic_code")]
        public string InvoiceElectronicCode { get; set; }

        [JsonProperty("unique_verification_code")]
        public string UniqueVerificationCode { get; set; }
    }

    public class RcmResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("rcm_id")]
        public string RcmId { get; set; }
    }

    public class RcmAuthToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
}
