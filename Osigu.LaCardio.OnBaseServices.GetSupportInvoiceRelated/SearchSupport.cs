using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated
{
    public interface ISearchSupport
    {
        Task DoWorkAsync();
    }

    public class SearchSupport : ISearchSupport
    {
        public async Task DoWorkAsync()
        {
            // Simula una tarea asíncrona
            await Task.Delay(1000);
            Console.WriteLine("Trabajo realizado en MyService.");
        }

    }
}
