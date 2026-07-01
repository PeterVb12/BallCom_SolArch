using System.Text;
using System.Text.Json;
using BallCom.Warehouse.API.Data;
using BallCom.Warehouse.API.Models;
using BallCom.Warehouse.API.Models.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BallCom.Warehouse.API.Messaging
{
    public class PaymentCompletedEventConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaymentCompletedEventConsumer> _logger;
        private readonly string _hostname = "localhost";
        private readonly string _exchangeName = "ballcom-exchange";
        private readonly string _queueName = "warehouse.payment-completed";

        private IConnection? _connection;
        private IModel? _channel;

        public PaymentCompletedEventConsumer(IServiceScopeFactory scopeFactory,
                                             IHttpClientFactory httpClientFactory,
                                             ILogger<PaymentCompletedEventConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await TryConnectAsync(stoppingToken);

            if (_channel is null)
            {
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("[Warehouse Service] Consumer luistert op queue '{Queue}'.", _queueName);
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
                    _logger.LogWarning(ex, "[Warehouse Service] RabbitMQ nog niet bereikbaar (poging {Attempt}/10). Opnieuw over 3s.", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }

            _logger.LogError("[Warehouse Service] Kon geen verbinding maken met RabbitMQ. Consumer stopt.");
        }

        private void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
        {
            if (ea.RoutingKey != nameof(PaymentCompletedEvent))
            {
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var paymentCompleted = JsonSerializer.Deserialize<PaymentCompletedEvent>(json);

                if (paymentCompleted is not null)
                {
                    HandlePaymentCompleted(paymentCompleted);
                }

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Warehouse Service] Fout bij verwerken PaymentCompletedEvent.");
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }

        private void HandlePaymentCompleted(PaymentCompletedEvent paymentCompleted)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

            var alreadyExists = context.PickLists.Any(p => p.OrderId == paymentCompleted.OrderId);
            if (alreadyExists)
            {
                _logger.LogInformation("[Warehouse Service] PaymentCompletedEvent voor order {OrderId} al verwerkt; overslaan.", paymentCompleted.OrderId);
                return;
            }

            var order = FetchOrder(paymentCompleted.OrderId);
            if (order is null)
            {
                _logger.LogWarning("[Warehouse Service] Order {OrderId} kon niet opgehaald worden bij Ordering; pick list niet aangemaakt.", paymentCompleted.OrderId);
                return;
            }

            var pickListId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var createdEvent = new PickListCreatedEvent(pickListId, paymentCompleted.OrderId, occurredAt);

            var eventStore = new EventStore(context);
            eventStore.Append(pickListId, nameof(PickList), createdEvent);

            var pickList = new PickList
            {
                Id = pickListId,
                OrderId = paymentCompleted.OrderId,
                Status = PickListStatus.Released,
                CreatedAt = occurredAt,
                UpdatedAt = occurredAt,
                Lines = order.Items.Select(i => new PickListLine
                {
                    Id = Guid.NewGuid(),
                    PickListId = pickListId,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };
            context.PickLists.Add(pickList);

            try
            {
                context.SaveChanges();
                _logger.LogInformation("[Warehouse Service] PickList {PickListId} (RELEASED) aangemaakt voor order {OrderId} met {LineCount} regels.", pickListId, paymentCompleted.OrderId, pickList.Lines.Count);
            }
            catch (Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                _logger.LogInformation("[Warehouse Service] PickList voor order {OrderId} bestond al (unieke index); overslaan.", paymentCompleted.OrderId);
            }
        }

        private OrderDto? FetchOrder(int orderId)
        {
            var client = _httpClientFactory.CreateClient("OrderingService");
            var response = client.GetAsync($"api/orders/{orderId}").GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<OrderDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
