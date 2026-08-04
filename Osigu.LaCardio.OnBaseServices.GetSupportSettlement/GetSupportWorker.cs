using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Configuration;
using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Workers
{
    public class GetSupportWorker : BackgroundService
    {
        private readonly ILogger<GetSupportWorker> _logger;
        private readonly ISearchSupport _searchSupport;
        private Configuration _appsetting;

        public GetSupportWorker(ILogger<GetSupportWorker> logger, ISearchSupport searchSupport, Configuration appsetting)
        {
            _appsetting = appsetting;
            _searchSupport = searchSupport;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)


            while (!stoppingToken.IsCancellationRequested)
            {
                await _searchSupport.FindSupportInFolder();


                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(_appsetting.ExecutionFrecuency, stoppingToken);
            }
        }


    }
}