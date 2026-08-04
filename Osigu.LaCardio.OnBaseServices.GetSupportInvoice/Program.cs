using SCSE.RCM.Integration.Application;
using SCSE.RCM.Integration.Integrations;
using SCSE.RCM.Integration.Presenters;
using SCSE.RCM.Integration.Repository.ORM;
using SCSE.RCM.Integration.ServiceWorkers.RebillingWorker;
using SCSE.RCM.Integration.ServiceWorkers.RebillingWorker.Context;
using Serilog;

class Program
{
    static void Main(string[] args)
    {

        // Crear una instancia de ConfigurationBuilder
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Cargar la configuración
        IConfigurationRoot configurationHost = configBuilder.Build();
        var configuration = configurationHost.Get<RCMIntegrationWebServicesContextConfiguration>();



        // Construir el host de la aplicación y pasar appSettings al método CreateHostBuilder
        var host = CreateHostBuilder(args, configurationHost, configuration).Build();

        // Ejecutar el Worker Service
        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot configurationHost, RCMIntegrationWebServicesContextConfiguration appSettings)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton(appSettings.ApplicationContext);
                services.AddRepositories(appSettings.ApplicationContext, true);
                services.AddServices(true);
                services.AddPresenters(true);
                services.AddIntegrations(true);
                services.AddHostedService<RebillingWorker>();
                services.AddLogging(builder => builder.AddSerilog(
                    new LoggerConfiguration()
                        .ReadFrom.Configuration(configurationHost)
                        .CreateLogger()))
                .BuildServiceProvider();

            }).UseWindowsService();
    }
}