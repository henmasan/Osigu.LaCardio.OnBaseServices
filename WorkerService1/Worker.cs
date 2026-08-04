public class Worker : BackgroundService
{
    private readonly IService _myService;
    private readonly ILogger<Worker> _logger;

    public Worker(Service myService, ILogger<Worker> logger)
    {
        _myService = myService ?? throw new ArgumentNullException(nameof(myService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await _myService.DoWorkAsync();
            await Task.Delay(5000, stoppingToken);
        }
    }
}