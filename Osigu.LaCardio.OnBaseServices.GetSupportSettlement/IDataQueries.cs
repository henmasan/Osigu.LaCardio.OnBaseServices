using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement
{
    public interface IDataQueries
    {
        Task<List<Invoice>> GetPendingInvoices(string status);

    }
}
