using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Projections;

namespace BallCom.Ordering.API.Application.Commands
{
    public record MarkOrderPaidCommand(int OrderId, decimal Amount);
    public record CancelOrderCommand(int OrderId, string Reason);

    public class MarkOrderPaidCommandHandler
    {
        private readonly OrderEventStore _eventStore;
        private readonly ProjectionQueue _projectionQueue;
        private readonly ILogger<MarkOrderPaidCommandHandler> _logger;

        public MarkOrderPaidCommandHandler(
            OrderEventStore eventStore,
            ProjectionQueue projectionQueue,
            ILogger<MarkOrderPaidCommandHandler> logger)
        {
            _eventStore = eventStore;
            _projectionQueue = projectionQueue;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(MarkOrderPaidCommand command)
        {
            var aggregate = await _eventStore.LoadAsync(command.OrderId);
            if (aggregate is null)
            {
                _logger.LogWarning("[Ordering ES] MarkPaid: order {OrderId} onbekend.", command.OrderId);
                return false;
            }

            aggregate.MarkPaid(command.Amount);

            var appended = await _eventStore.SaveAsync(aggregate);
            if (appended.Count > 0)
            {
                await _projectionQueue.EnqueueAsync(appended);
                _logger.LogInformation("[Ordering ES] Order {OrderId} op PAID gezet via OrderPaidEvent.", command.OrderId);
            }

            return true;
        }
    }

    public class CancelOrderCommandHandler
    {
        private readonly OrderEventStore _eventStore;
        private readonly ProjectionQueue _projectionQueue;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(
            OrderEventStore eventStore,
            ProjectionQueue projectionQueue,
            ILogger<CancelOrderCommandHandler> logger)
        {
            _eventStore = eventStore;
            _projectionQueue = projectionQueue;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(CancelOrderCommand command)
        {
            var aggregate = await _eventStore.LoadAsync(command.OrderId);
            if (aggregate is null)
            {
                return false;
            }

            aggregate.Cancel(string.IsNullOrWhiteSpace(command.Reason) ? "Geannuleerd" : command.Reason);

            var appended = await _eventStore.SaveAsync(aggregate);
            if (appended.Count > 0)
            {
                await _projectionQueue.EnqueueAsync(appended);
                _logger.LogInformation("[Ordering ES] Order {OrderId} geannuleerd via OrderCancelledEvent.", command.OrderId);
            }

            return true;
        }
    }
}
