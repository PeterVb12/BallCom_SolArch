using BallCom.Ordering.API.Domain.Events;

namespace BallCom.Ordering.API.Domain
{
    public static class OrderEventTypeRegistry
    {
        private static readonly IReadOnlyDictionary<string, Type> _types = new Dictionary<string, Type>
        {
            [nameof(OrderPlacedDomainEvent)] = typeof(OrderPlacedDomainEvent),
            [nameof(OrderPaidDomainEvent)] = typeof(OrderPaidDomainEvent),
            [nameof(OrderProcessingStartedDomainEvent)] = typeof(OrderProcessingStartedDomainEvent),
            [nameof(OrderCancelledDomainEvent)] = typeof(OrderCancelledDomainEvent),
        };

        public static Type? Resolve(string eventType)
            => _types.TryGetValue(eventType, out var type) ? type : null;
    }
}
