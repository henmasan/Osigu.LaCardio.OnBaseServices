using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Infrastructure;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Workers;
using Serilog;
using System;

public class Program
{
    static void Main(string[] args)
    {

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Cargar la configuraci�n
        IConfigurationRoot configurationHost = configBuilder.Build();
        var configuration = configurationHost.Get<AppsettingConfiguration>();

        // Construir el host de la aplicaci�n y pasar appSettings al m�todo CreateHostBuilder
        var host = CreateHostBuilder(args, configurationHost, configuration).Build();

        host.Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot configurationHost, AppsettingConfiguration appSettings)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton(appSettings.Configuration);
                services.AddSingleton<IDataQueries, DataQueries>();
                services.AddSingleton<ISupportDestination, FileSystemSupportDestination>();
                services.AddSingleton<ISearchSupport, SearchSupport>();
                services.AddHostedService<GetSupportWorker>();

                services.AddLogging(builder => builder.AddSerilog(
                  new LoggerConfiguration()
                                          .ReadFrom.Configuration(configurationHost)
                                          .CreateLogger()))
                                  .BuildServiceProvider();
            }).UseWindowsService();
    }

}


