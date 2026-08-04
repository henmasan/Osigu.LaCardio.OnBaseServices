namespace Osigu.LaCardio.OnBaseServices.GetSupportInvoiceRelated
{
    public class Worker : BackgroundService
    {
        private readonly ISearchSupport _myService;
        private readonly ILogger<Worker> _logger;

        public Worker(ISearchSupport myService, ILogger<Worker> logger)
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
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}