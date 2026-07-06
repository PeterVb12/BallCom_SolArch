using BallCom.Ordering.API.Data;

namespace BallCom.Ordering.API.Projections
{
    public class OrderProjectionService : BackgroundService
    {
        private readonly ProjectionQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderProjectionService> _logger;

        public OrderProjectionService(
            ProjectionQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<OrderProjectionService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var events in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var read = scope.ServiceProvider.GetRequiredService<OrderingReadDbContext>();
                    var projector = new OrderReadModelProjector(read);

                    await projector.ProjectAsync(events, stoppingToken);

                    _logger.LogInformation("[Ordering ES] {Count} event(s) geprojecteerd naar de read models.", events.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Ordering ES] Fout bij projecteren van events naar de leeskant.");
                }
            }
        }
    }
}
