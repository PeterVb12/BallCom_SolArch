using System.Text;
using System.Text.Json;
using BallCom.Logistics.API.Data;
using BallCom.Logistics.API.Models;
using BallCom.Logistics.API.Models.Events;
using BallCom.Logistics.API.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BallCom.Logistics.API.Messaging
{
    public class PackageReadyEventConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PackageReadyEventConsumer> _logger;
        private readonly string _hostname;
        private readonly string _exchangeName = "ballcom-exchange";
        private readonly string _queueName = "logistics.package-ready";

        private IConnection? _connection;
        private IModel? _channel;

        public PackageReadyEventConsumer(IServiceScopeFactory scopeFactory,
                                         IHttpClientFactory httpClientFactory,
                                         ILogger<PackageReadyEventConsumer> logger,
                                         IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _hostname = configuration["RabbitMQ:Host"] ?? "localhost";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await TryConnectAsync(stoppingToken);
            if (_channel is null) return;

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;
            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("[Logistics Service] Consumer luistert op queue '{Queue}'.", _queueName);
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
                    _logger.LogWarning(ex, "[Logistics Service] RabbitMQ nog niet bereikbaar (poging {Attempt}/10).", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }

        private void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
        {
            if (ea.RoutingKey != nameof(PackageReadyEvent))
            {
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var packageReady = JsonSerializer.Deserialize<PackageReadyEvent>(json);
                if (packageReady is not null)
                {
                    HandlePackageReady(packageReady);
                }

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Logistics Service] Fout bij verwerken PackageReadyEvent.");
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }

        private void HandlePackageReady(PackageReadyEvent packageReady)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
            var carrierSelection = scope.ServiceProvider.GetRequiredService<CarrierSelectionService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            if (context.Shipments.Any(s => s.OrderId == packageReady.OrderId))
            {
                _logger.LogInformation("[Logistics Service] Shipment voor order {OrderId} bestaat al; overslaan.", packageReady.OrderId);
                return;
            }

            var order = FetchOrder(packageReady.OrderId);
            if (order is null)
            {
                _logger.LogWarning("[Logistics Service] Order {OrderId} niet opgehaald; shipment niet aangemaakt.", packageReady.OrderId);
                return;
            }

            var quotes = carrierSelection.GetAllowedCarrierQuotes();
            var selected = carrierSelection.SelectLowestCostCarrier();
            var quotesAudit = JsonSerializer.Serialize(quotes);

            var shipmentId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;
            var trackingNumber = $"TRK-{packageReady.OrderId:D6}-{occurredAt:yyyyMMdd}";

            var createdEvent = new ShipmentCreatedEvent(
                shipmentId, packageReady.OrderId, selected.CarrierName, selected.Price, quotesAudit, trackingNumber, occurredAt);

            var eventStore = new EventStore(context);
            eventStore.Append(shipmentId, nameof(Shipment), createdEvent);

            var shipment = new Shipment
            {
                Id = shipmentId,
                OrderId = packageReady.OrderId,
                Status = ShipmentStatus.Created,
                SelectedCarrier = selected.CarrierName,
                SelectedPrice = selected.Price,
                TrackingNumber = trackingNumber,
                CustomerEmail = order.CustomerEmail,
                CustomerName = order.CustomerName,
                DeliveryAddress = $"{order.Street}, {order.PostalCode} {order.City}, {order.Country}",
                CarrierQuotesAudit = quotesAudit,
                CreatedAt = occurredAt,
                UpdatedAt = occurredAt
            };

            context.Shipments.Add(shipment);

            try
            {
                context.SaveChanges();
                eventPublisher.Publish(createdEvent);
                _logger.LogInformation("[Logistics Service] Shipment {ShipmentId} aangemaakt voor order {OrderId} via {Carrier} (€{Price}).",
                    shipmentId, packageReady.OrderId, selected.CarrierName, selected.Price);
            }
            catch (Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                _logger.LogInformation("[Logistics Service] Shipment voor order {OrderId} bestond al (unieke index).", packageReady.OrderId);
            }
        }

        private OrderDto? FetchOrder(int orderId)
        {
            var client = _httpClientFactory.CreateClient("OrderingService");
            var response = client.GetAsync($"api/orders/{orderId}").GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return null;

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<OrderDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
