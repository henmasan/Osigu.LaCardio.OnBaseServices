
namespace Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated
{
    public interface IMyService
    {
        Task DoWorkAsync();
    }

    public class MyService : IMyService
    {
        public async Task DoWorkAsync()
        {
            // Simula una tarea asíncrona
            await Task.Delay(1000);
            Console.WriteLine("Trabajo realizado en MyService.");
        }
    }
}
