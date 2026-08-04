using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement
{
    public class GetSupportWorker : BackgroundService
    {
        private readonly ILogger<GetSupportWorker> _logger;
        //private readonly ISearchSupport _searchSupport;
        private readonly ISearchSupport _searchSupport;
        private Configuration _appsetting;

        private readonly IServiceScopeFactory _scopeFactory;
        public GetSupportWorker(ILogger<GetSupportWorker> logger, SearchSupport searchSupport, Configuration appsetting, IServiceScopeFactory scopeFactory)
        {
            {
                _appsetting = appsetting;
               _searchSupport = searchSupport;
                _logger = logger;
                _scopeFactory = scopeFactory;

            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            //var searchSupport = scope.ServiceProvider.GetRequiredService<ISearchSupport>();
            //var appsetting = scope.ServiceProvider.GetRequiredService<Configuration>();


            while (!stoppingToken.IsCancellationRequested)
            {
                await _searchSupport.FindSupportInFolder();


                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(_appsetting.ExecutionFrecuency, stoppingToken);
            }
        }


    }
}