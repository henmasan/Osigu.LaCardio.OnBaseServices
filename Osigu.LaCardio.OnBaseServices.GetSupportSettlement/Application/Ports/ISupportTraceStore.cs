using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public interface ISupportTraceStore
    {
        Task<SupportTraceRecord> AddTraceAsync(SupportTraceRecord record);
        Task<SupportTraceRecord> GetTraceAsync(int id);
        Task<SupportTraceRecord> GetPendingTraceAsync(string invoiceNumber, string supportType);
        Task<List<SupportTraceRecord>> GetPendingTracesAsync();
        Task UpdateTraceAsync(SupportTraceRecord record);
        Task<bool> ExistsAsync(string invoiceNumber, string supportType);
    }
}
