using System.Text;
using System.Text.Json;
using BallCom.Payment.API.Data;
using BallCom.Payment.API.Models;
using BallCom.Payment.API.Models.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BallCom.Payment.API.Messaging
{
    public class OrderPlacedEventConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderPlacedEventConsumer> _logger;
        private readonly string _hostname = "localhost";
        private readonly string _exchangeName = "ballcom-exchange";
        private readonly string _queueName = "payment.order-placed";

        private IConnection? _connection;
        private IModel? _channel;

        public OrderPlacedEventConsumer(IServiceScopeFactory scopeFactory,
                                        ILogger<OrderPlacedEventConsumer> logger)
        {
            _scopeFactory = scopeFactory;
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
            _logger.LogInformation("[Payment Service] Consumer luistert op queue '{Queue}'.", _queueName);
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
                    _logger.LogWarning(ex, "[Payment Service] RabbitMQ nog niet bereikbaar (poging {Attempt}/10). Opnieuw over 3s.", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }

            _logger.LogError("[Payment Service] Kon geen verbinding maken met RabbitMQ. Consumer stopt.");
        }

        private void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
        {
            if (ea.RoutingKey != nameof(OrderPlacedEvent))
            {
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(json);

                if (orderPlaced is not null)
                {
                    HandleOrderPlaced(orderPlaced);
                }

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payment Service] Fout bij verwerken OrderPlacedEvent.");
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }

        private void HandleOrderPlaced(OrderPlacedEvent orderPlaced)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

            var alreadyExists = context.Transactions.Any(t => t.OrderId == orderPlaced.OrderId);
            if (alreadyExists)
            {
                _logger.LogInformation("[Payment Service] OrderPlacedEvent voor order {OrderId} al verwerkt; overslaan.", orderPlaced.OrderId);
                return;
            }

            var transactionId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var createdEvent = new TransactionCreatedEvent(
                transactionId, orderPlaced.OrderId, orderPlaced.TotalPrice, PaymentMethods.ForwardPay, occurredAt);

            var eventStore = new EventStore(context);
            eventStore.Append(transactionId, nameof(Transaction), createdEvent);

            var transaction = new Transaction
            {
                Id = transactionId,
                OrderId = orderPlaced.OrderId,
                Amount = orderPlaced.TotalPrice,
                PaymentMethod = string.Empty,
                Status = PaymentStatus.Pending,
                CreatedAt = occurredAt,
                UpdatedAt = occurredAt
            };
            context.Transactions.Add(transaction);

            try
            {
                context.SaveChanges();
                _logger.LogInformation("[Payment Service] Transactie {TransactionId} (PENDING) aangemaakt voor order {OrderId}.", transactionId, orderPlaced.OrderId);
            }
            catch (DbUpdateException)
            {
                _logger.LogInformation("[Payment Service] Transactie voor order {OrderId} bestond al (unieke index); overslaan.", orderPlaced.OrderId);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
