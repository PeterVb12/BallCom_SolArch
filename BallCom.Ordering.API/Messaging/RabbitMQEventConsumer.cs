using System.Text;
using System.Text.Json;
using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BallCom.Ordering.API.Messaging
{
    public class RabbitMQEventConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMQEventConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
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
                
                _logger.LogInformation("[Ordering Service] Bericht ontvangen via RabbitMQ: {Json} met RoutingKey: {Key}", json, ea.RoutingKey);

                try
                {
                    // CASE 1: Nieuw product toegevoegd in Catalogus
                    if (ea.RoutingKey == "ProductAddedEvent")
                    {
                        var productEvent = JsonSerializer.Deserialize<ProductAddedIntegrationEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (productEvent != null)
                        {
                            var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

                            var exists = dbContext.Products.Any(p => p.Id == productEvent.ProductId);
                            if (!exists)
                            {
                                var newProduct = new Product
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
                    // CASE 2: Product bijgewerkt in Catalogus (Nu gekoppeld aan jouw nieuwe ProductUpdatedEvent)
                    else if (ea.RoutingKey == "ProductUpdatedEvent")
                    {
                        var productEvent = JsonSerializer.Deserialize<ProductUpdatedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (productEvent != null)
                        {
                            var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

                            // Zoek het product lokaal op in de Ordering database
                            var product = dbContext.Products.FirstOrDefault(p => p.Id == productEvent.ProductId);
                            if (product != null)
                            {
                                // Synchroniseer de lokale gegevens
                                product.Name = productEvent.Name;
                                product.Price = productEvent.Price;
                                
                                await dbContext.SaveChangesAsync();
                                _logger.LogInformation("[Ordering Service] Product {Name} succesvol BIJGEWERKT in ordering_db!", product.Name);
                            }
                            else
                            {
                                _logger.LogWarning("[Ordering Service] Product {ProductId} kon niet worden geüpdatet omdat het hier onbekend is.", productEvent.ProductId);
                            }
                        }
                    }
                    // Onbekende of irrelevante events negeren
                    else
                    {
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
            _channel?.Close(); // Sluit de RabbitMQ channel
            _connection?.Close(); // Sluit de netwerkverbinding
            base.Dispose(); // Laat de achtergrond service de rest opruimen
        }
    }
}