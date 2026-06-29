using System.Text;
using System.Text.Json;
using BallCom.Ordering.API.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BallCom.Ordering.API.Messaging
{
    // Dit klasse-ontwerp komt overeen met het ProductAddedEvent uit de Catalog.API
    public record ProductAddedIntegrationEvent(Guid ProductId, string Name, string Description, decimal Price, int Stock, Guid SupplierId, DateTime OccurredAt);

    public class RabbitMQEventConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMQEventConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory; // Nodig om met de database te praten
        private IConnection _connection;
        private IModel _channel;
        private string _queueName;

        public RabbitMQEventConsumer(ILogger<RabbitMQEventConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            string exchangeName = "ballcom-exchange";
            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);

            _queueName = _channel.QueueDeclare(queue: "ordering-service-queue", 
                                             durable: true, 
                                             exclusive: false, 
                                             autoDelete: false).QueueName;

            _channel.QueueBind(queue: _queueName, exchange: exchangeName, routingKey: "");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("[Ordering Service] Bericht ontvangen via RabbitMQ: {Json}", json);

                try
                {
                    // Probeer te kijken of het bericht een ProductAddedEvent is
                    // (Omdat we Fanout gebruiken, checken we of de routingKey of de inhoud overeenkomt)
                    if (ea.RoutingKey == "ProductAddedEvent")
                    {
                        var productEvent = JsonSerializer.Deserialize<ProductAddedIntegrationEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (productEvent != null)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

                            var exists = dbContext.Products.Any(p => p.Id == productEvent.ProductId);
                            if (!exists)
                            {
                                _logger.LogInformation("HIER MOET JE KIJKEN: {ProductId}", productEvent.ProductId.ToString());
                                var newProduct = new Models.Product
                                {
                                    Id = productEvent.ProductId,
                                    Name = productEvent.Name,
                                    Price = productEvent.Price
                                };

                                dbContext.Products.Add(newProduct);
                                await dbContext.SaveChangesAsync();
                                _logger.LogInformation("[Ordering Service] Product {Name} succesvol opgeslagen in ordering_db!", newProduct.Name);
                            }
                        }
                    }
                    else
                    {
                        // Log dat we het bericht negeren omdat het niet voor ons (of deze tabel) bedoeld is
                        _logger.LogInformation("[Ordering Service] Event genegeerd (RoutingKey: {Key}). Niet relevant voor de producttabel.", ea.RoutingKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Ordering Service] Fout bij verwerken van event.");
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}