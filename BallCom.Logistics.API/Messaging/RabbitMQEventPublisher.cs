using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BallCom.Logistics.API.Messaging
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
            var factory = new ConnectionFactory { HostName = _hostname };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            const string exchangeName = "ballcom-exchange";
            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);
            var routingKey = typeof(T).Name;

            channel.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: null, body: body);
        }
    }
}
