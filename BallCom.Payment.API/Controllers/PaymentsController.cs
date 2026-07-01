using BallCom.Payment.API.Data;
using BallCom.Payment.API.Messaging;
using BallCom.Payment.API.Models;
using BallCom.Payment.API.Models.Commands;
using BallCom.Payment.API.Models.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Payment.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentDbContext _context;
        private readonly ILogger<PaymentsController> _logger;
        private readonly IEventPublisher _eventPublisher;

        public PaymentsController(PaymentDbContext context,
                                  ILogger<PaymentsController> logger,
                                  IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        [HttpPost]
        public async Task<IActionResult> StartPayment([FromBody] StartPaymentCommand command)
        {
            if (command.PaymentMethod != PaymentMethods.ForwardPay && command.PaymentMethod != PaymentMethods.AfterPay)
            {
                return BadRequest("PaymentMethod moet 'ForwardPay' of 'AfterPay' zijn.");
            }

            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.OrderId == command.OrderId);
            if (transaction is null)
            {
                return BadRequest($"Geen betaling bekend voor order {command.OrderId}. Wacht tot het OrderPlacedEvent verwerkt is.");
            }

            if (transaction.Status == PaymentStatus.Paid)
            {
                return Conflict($"Order {command.OrderId} is al betaald.");
            }

            transaction.PaymentMethod = command.PaymentMethod;
            transaction.UpdatedAt = DateTime.UtcNow;
            var eventStore = new EventStore(_context);

            if (command.PaymentMethod == PaymentMethods.AfterPay)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("[Payment Service] AfterPay gestart voor order {OrderId}; status blijft PENDING.", command.OrderId);
                return Ok(transaction);
            }

            if (command.SimulateFailure)
            {
                return Fail(transaction, eventStore, "Gesimuleerde betaalfout (ForwardPay).");
            }

            return await Complete(transaction, eventStore);
        }

        [HttpPost("{orderId:int}/complete")]
        public async Task<IActionResult> CompletePayment(int orderId, [FromQuery] bool simulateFailure = false)
        {
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.OrderId == orderId);
            if (transaction is null)
            {
                return NotFound($"Geen betaling bekend voor order {orderId}.");
            }

            if (transaction.Status == PaymentStatus.Paid)
            {
                return Conflict($"Order {orderId} is al betaald.");
            }

            var eventStore = new EventStore(_context);

            if (simulateFailure)
            {
                return Fail(transaction, eventStore, "Gesimuleerde betaalfout (AfterPay).");
            }

            return await Complete(transaction, eventStore);
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            var transactions = await _context.Transactions.AsNoTracking().ToListAsync();
            return Ok(transactions);
        }

        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetTransactionByOrderId(int orderId)
        {
            var transaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.OrderId == orderId);
            if (transaction is null)
            {
                return NotFound();
            }

            return Ok(transaction);
        }

        private async Task<IActionResult> Complete(Transaction transaction, EventStore eventStore)
        {
            var completedAt = DateTime.UtcNow;
            var completedEvent = new PaymentCompletedEvent(
                transaction.OrderId, transaction.Id, transaction.Amount, transaction.PaymentMethod, completedAt);

            eventStore.Append(transaction.Id, nameof(Transaction), completedEvent);

            transaction.Status = PaymentStatus.Paid;
            transaction.UpdatedAt = completedAt;

            await _context.SaveChangesAsync();

            _eventPublisher.Publish(completedEvent);
            _logger.LogInformation("[Payment Service] Order {OrderId} betaald ({Method}); PaymentCompletedEvent gepubliceerd.", transaction.OrderId, transaction.PaymentMethod);

            return Ok(transaction);
        }

        private IActionResult Fail(Transaction transaction, EventStore eventStore, string reason)
        {
            var failedAt = DateTime.UtcNow;
            var failedEvent = new PaymentFailedEvent(
                transaction.OrderId, transaction.Id, transaction.Amount, transaction.PaymentMethod, reason, failedAt);

            eventStore.Append(transaction.Id, nameof(Transaction), failedEvent);

            transaction.Status = PaymentStatus.Failed;
            transaction.UpdatedAt = failedAt;

            _context.SaveChanges();

            _eventPublisher.Publish(failedEvent);
            _logger.LogInformation("[Payment Service] Betaling voor order {OrderId} mislukt; PaymentFailedEvent gepubliceerd.", transaction.OrderId);

            return StatusCode(402, transaction);
        }
    }
}
