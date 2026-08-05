using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public class RcmUploadRequest
    {
        public string InvoiceNumber { get; set; }
        public string SupportFileCode { get; set; }
        public string AgreementCode { get; set; }
        public string DocumentType { get; set; }
        public string OriginEventId { get; set; }
        public string ProcessId { get; set; }
        public string InvoiceElectronicCode { get; set; }
        public string FilePath { get; set; }
    }

    public class RcmUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RcmId { get; set; }
    }

    public interface IRcmSupportClient
    {
        Task<RcmUploadResponse> UploadSupportAsync(RcmUploadRequest request, string accessToken);
    }
}
