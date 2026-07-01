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
    public class OrderCancelledEventConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderCancelledEventConsumer> _logger;
        private readonly string _hostname = "localhost";
        private readonly string _exchangeName = "ballcom-exchange";
        private readonly string _queueName = "payment.order-cancelled";

        private IConnection? _connection;
        private IModel? _channel;

        public OrderCancelledEventConsumer(IServiceScopeFactory scopeFactory,
                                           ILogger<OrderCancelledEventConsumer> logger)
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
            _logger.LogInformation("[Payment Service] OrderCancelled consumer luistert op queue '{Queue}'.", _queueName);
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
                    _logger.LogWarning(ex, "[Payment Service] RabbitMQ nog niet bereikbaar (poging {Attempt}/10).", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }

        private void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
        {
            if (ea.RoutingKey != nameof(Models.Events.OrderCancelledEvent))
            {
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var cancelled = JsonSerializer.Deserialize<Models.Events.OrderCancelledEvent>(json);

                if (cancelled is not null)
                {
                    HandleOrderCancelled(cancelled);
                }

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payment Service] Fout bij verwerken OrderCancelledEvent.");
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }

        private void HandleOrderCancelled(Models.Events.OrderCancelledEvent cancelled)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

            var transaction = context.Transactions.FirstOrDefault(t => t.OrderId == cancelled.OrderId);
            if (transaction is null)
            {
                _logger.LogInformation("[Payment Service] Geen transactie voor geannuleerde order {OrderId}.", cancelled.OrderId);
                return;
            }

            if (transaction.Status == PaymentStatus.Cancelled)
            {
                return;
            }

            transaction.Status = PaymentStatus.Cancelled;
            transaction.UpdatedAt = DateTime.UtcNow;
            context.SaveChanges();

            _logger.LogInformation("[Payment Service] Transactie voor order {OrderId} geannuleerd.", cancelled.OrderId);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
