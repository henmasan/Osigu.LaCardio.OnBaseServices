using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    public interface ISendSupportToRcm
    {
        Task SendPendingSupportAsync(SupportTraceRecord trace);
        Task SendBatchAsync(List<SupportTraceRecord> traces);
        Task ProcessQueueAsync();
    }
}
