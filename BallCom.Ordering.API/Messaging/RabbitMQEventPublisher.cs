
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BallCom.Ordering.API.Messaging
{
    public class RabbitMQEventPublisher : IEventPublisher
    {
        private readonly string _hostname;

        public RabbitMQEventPublisher(IConfiguration configuration)
        {
            _hostname = configuration["RabbitMQ:Host"] ?? "localhost";
        }

        public void Publish<T>(T @event)
        {
            // 1. Maak verbinding met de RabbitMQ container
            var factory = new ConnectionFactory { HostName = _hostname };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // 2. Declareer een 'Exchange'. Dit is het verdeelstation van RabbitMQ.
            // We noemen hem 'ballcom-exchange' en het type is 'fanout' (schreeuw naar iedereen)
            string exchangeName = "ballcom-exchange";
            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);

            // 3. Vertaal het C# record naar JSON tekst en daarna naar Bytes
            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            // 4. Gooi het bericht op de Exchange!
            string routingKey = typeof(T).Name; // Bijv. "OrderPlacedEvent"
            channel.BasicPublish(exchange: exchangeName,
                                 routingKey: routingKey,
                                 basicProperties: null,
                                 body: body);
        }
    }
}
