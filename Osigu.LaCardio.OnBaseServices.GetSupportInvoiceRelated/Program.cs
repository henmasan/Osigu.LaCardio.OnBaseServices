using Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Registrar el servicio y el worker
                services.AddScoped<IMyService, MyService>();
                services.AddHostedService<MyWorker>();
            });
}