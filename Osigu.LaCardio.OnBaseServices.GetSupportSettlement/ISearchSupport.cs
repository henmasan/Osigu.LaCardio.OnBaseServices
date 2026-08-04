using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application
{
    public interface ISearchSupport
    {
        Task FindSupportInFolder();
        Task FindPendingInvoice();
        Task<ProcessData> FindSupports(Invoice invoice);
    }
}
