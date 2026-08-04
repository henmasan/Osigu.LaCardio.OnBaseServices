using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model;
using Serilog;
using System;

public class Program
{
    static void Main(string[] args)
    {

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Cargar la configuración
        IConfigurationRoot configurationHost = configBuilder.Build();
        var configuration = configurationHost.Get<AppsettingConfiguration>();

        // Construir el host de la aplicación y pasar appSettings al método CreateHostBuilder
        var host = CreateHostBuilder(args, configurationHost, configuration).Build();

        host.Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot configurationHost, AppsettingConfiguration appSettings)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton(appSettings.Configuration);
                //services.AddRepositories(appSettings.ApplicationContext, true);
                services.AddSingleton<SearchSupport>();
                services.AddSingleton<DataQueries>();
                services.AddHostedService<GetSupportWorker>();

                services.AddLogging(builder => builder.AddSerilog(
                  new LoggerConfiguration()
                                          .ReadFrom.Configuration(configurationHost)
                                          .CreateLogger()))
                                  .BuildServiceProvider();
            }).UseWindowsService();
    }

}


