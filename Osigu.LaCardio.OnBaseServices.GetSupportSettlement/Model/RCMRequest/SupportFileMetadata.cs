using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Osigu.OnBaseServices.GetSupportSettlement.Model.RCMRequest
{
    public class SupportFileMetadata
    {

        public string? ProcessId { get; set; }
        public string DocumentType { get; set; }
        public string AgreementDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDateTime { get; set; }
        public string DocumentTypeNumber { get; set; }
        public string InvoiceElectronicCode { get; set; }
        public string? UniqueVerificationCode { get; set; }

        [JsonIgnore]
        public string FilePath { get; set; }
    }

}
