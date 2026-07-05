using System.Text;
using System.Text.Json;
using BallCom.Ordering.API.Application.Commands;
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
        private readonly string _hostname;
        private readonly string _exchangeName = "ballcom-exchange";
        private readonly string _queueName = "ordering-service-queue";

        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQEventConsumer(
            ILogger<RabbitMQEventConsumer> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hostname = configuration["RabbitMQ:Host"] ?? "localhost";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await TryConnectAsync(stoppingToken);

            if (_channel is null)
            {
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("[Ordering Service] Bericht ontvangen: {Key}", ea.RoutingKey);

                try
                {
                    switch (ea.RoutingKey)
                    {
                        case "ProductAddedEvent":
                            await HandleProductAddedAsync(json);
                            break;
                        case "ProductUpdatedEvent":
                            await HandleProductUpdatedAsync(json);
                            break;
                        case "PaymentCompletedEvent":
                            await HandlePaymentCompletedAsync(json);
                            break;
                        default:
                            _logger.LogInformation("[Ordering Service] Event genegeerd (RoutingKey: {Key}).", ea.RoutingKey);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Ordering Service] Fout bij verwerken van event {Key}.", ea.RoutingKey);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
            _logger.LogInformation("[Ordering Service] Consumer luistert op queue '{Queue}'.", _queueName);
        }

        // Integratie-event -> COMMAND: markeer de order (event-sourced) als betaald.
        private async Task HandlePaymentCompletedAsync(string json)
        {
            var paid = JsonSerializer.Deserialize<PaymentCompletedIntegrationEvent>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paid is null)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<MarkOrderPaidCommandHandler>();
            await handler.HandleAsync(new MarkOrderPaidCommand(paid.OrderId, paid.Amount));
        }

        private async Task HandleProductAddedAsync(string json)
        {
            var productEvent = JsonSerializer.Deserialize<ProductAddedIntegrationEvent>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (productEvent is null)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingWriteDbContext>();

            if (!dbContext.Products.Any(p => p.Id == productEvent.ProductId))
            {
                dbContext.Products.Add(new Product
                {
                    Id = productEvent.ProductId,
                    Name = productEvent.Name,
                    Price = productEvent.Price
                });
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[Ordering Service] Product {Name} opgeslagen in referentietabel.", productEvent.Name);
            }
        }

        private async Task HandleProductUpdatedAsync(string json)
        {
            var productEvent = JsonSerializer.Deserialize<ProductUpdatedEvent>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (productEvent is null)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingWriteDbContext>();

            var product = dbContext.Products.FirstOrDefault(p => p.Id == productEvent.ProductId);
            if (product is not null)
            {
                product.Name = productEvent.Name;
                product.Price = productEvent.Price;
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[Ordering Service] Product {Name} bijgewerkt in referentietabel.", product.Name);
            }
        }

        private async Task TryConnectAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = _hostname, DispatchConsumersAsync = false };

            for (var attempt = 1; attempt <= 10 && !stoppingToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Fanout);
                    _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
                    _channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: string.Empty);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Ordering Service] RabbitMQ nog niet bereikbaar (poging {Attempt}/10). Opnieuw over 3s.", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }

            _logger.LogError("[Ordering Service] Kon geen verbinding maken met RabbitMQ. Consumer stopt.");
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
