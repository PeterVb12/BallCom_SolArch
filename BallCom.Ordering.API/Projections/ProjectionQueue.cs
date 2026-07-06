using System.Threading.Channels;
using BallCom.Ordering.API.Domain.Events;

namespace BallCom.Ordering.API.Projections
{
    public class ProjectionQueue
    {
        private readonly Channel<IReadOnlyList<IOrderEvent>> _channel =
            Channel.CreateUnbounded<IReadOnlyList<IOrderEvent>>();

        public ValueTask EnqueueAsync(IReadOnlyList<IOrderEvent> events)
            => _channel.Writer.WriteAsync(events);

        public IAsyncEnumerable<IReadOnlyList<IOrderEvent>> ReadAllAsync(CancellationToken ct)
            => _channel.Reader.ReadAllAsync(ct);
    }
}
