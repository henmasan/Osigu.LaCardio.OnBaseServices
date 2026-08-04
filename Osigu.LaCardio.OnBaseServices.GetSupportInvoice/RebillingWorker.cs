using SCSE.RCM.Integration.Domain;
using SCSE.RCM.Integration.Ports.Input.Rebilling;


namespace SCSE.RCM.Integration.ServiceWorkers.RebillingWorker
{
    public class RebillingWorker : BackgroundService
    {
        private readonly ILogger<RebillingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ApplicationContext _context;

        public RebillingWorker(ILogger<RebillingWorker> logger,
          IServiceScopeFactory scopeFactory, ApplicationContext context)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RebillingWorker -------------------start");
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var rebillingInputPort = scope.ServiceProvider.GetRequiredService<IRebillingInputPort>();
                var applicationContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                try
                {
                    await rebillingInputPort.Rebilling();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(" RebillingWorker in worker: {exceptionMessage}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(applicationContext.DelayTimeBetweenExecution), stoppingToken); // Esperar antes de la siguiente iteración
            }
        }
    }
}
