using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement
{
    public interface ISearchSupport
    {
        Task FindSupportInFolder();
        Task FindPendingInvoice();
        Task<ProcessData> FindSupports(Invoice invoice);
    }
}
