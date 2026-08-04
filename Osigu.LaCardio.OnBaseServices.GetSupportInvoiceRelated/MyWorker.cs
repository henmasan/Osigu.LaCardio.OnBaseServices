
using Microsoft.Extensions.Hosting;
using Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated;
using System;
using System.Threading;
using System.Threading.Tasks;

public class MyWorker : BackgroundService
{
    private readonly IMyService _myService;
    private readonly ILogger<MyWorker> _logger;

    public MyWorker(IMyService myService, ILogger<MyWorker> logger)
    {
        _myService = myService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await _myService.DoWorkAsync();
            await Task.Delay(5000, stoppingToken); // Esperar 5 segundos
        }
    }
}