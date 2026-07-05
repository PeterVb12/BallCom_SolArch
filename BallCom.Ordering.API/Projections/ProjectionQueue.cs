using System.Threading.Channels;
using BallCom.Ordering.API.Domain.Events;

namespace BallCom.Ordering.API.Projections
{
    // INTERNE async queue van de microservice (in-process, System.Threading.Channels).
    // De schrijfkant zet hier na een succesvolle append de nieuwe events op; de
    // OrderProjectionService leest ze en werkt daarmee de leeskant (Q) bij.
    // Zo is de leeskant EVENTUEEL consistent t.o.v. de schrijfkant, zonder dat
    // hiervoor RabbitMQ of een andere service nodig is.
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
