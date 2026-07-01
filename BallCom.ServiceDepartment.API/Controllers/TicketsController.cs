using BallCom.ServiceDepartment.API.Data;
using BallCom.ServiceDepartment.API.Models;
using BallCom.ServiceDepartment.API.Models.Commands;
using BallCom.ServiceDepartment.API.Models.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallCom.ServiceDepartment.API.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly ServiceDepartmentDbContext _context;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(ServiceDepartmentDbContext context, ILogger<TicketsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterTicketCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.CustomerEmail) ||
                string.IsNullOrWhiteSpace(command.Subject) ||
                string.IsNullOrWhiteSpace(command.Question))
            {
                return BadRequest("CustomerEmail, Subject en Question zijn verplicht.");
            }

            var ticketId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var registeredEvent = new TicketRegisteredEvent(
                ticketId, command.OrderId, command.CustomerEmail.Trim(), command.Subject.Trim(), command.Question.Trim(), occurredAt);

            var eventStore = new EventStore(_context);
            eventStore.Append(ticketId, nameof(Ticket), registeredEvent);

            var ticket = new Ticket
            {
                Id = ticketId,
                OrderId = command.OrderId,
                CustomerEmail = command.CustomerEmail.Trim(),
                Subject = command.Subject.Trim(),
                Question = command.Question.Trim(),
                Status = TicketStatus.Open,
                CreatedAt = occurredAt
            };
            _context.Tickets.Add(ticket);

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Service Department] Ticket {TicketId} geregistreerd voor {Email}.", ticketId, ticket.CustomerEmail);

            return CreatedAtAction(nameof(GetById), new { id = ticketId }, ticket);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _context.Tickets.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status.ToUpperInvariant());
            }

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return Ok(tickets);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ticket = await _context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (ticket is null)
            {
                return NotFound();
            }

            return Ok(ticket);
        }

        [HttpPost("{id:guid}/answer")]
        public async Task<IActionResult> Answer(Guid id, [FromBody] AnswerTicketCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Answer) || string.IsNullOrWhiteSpace(command.AnsweredBy))
            {
                return BadRequest("Answer en AnsweredBy zijn verplicht.");
            }

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket is null)
            {
                return NotFound();
            }

            if (ticket.Status == TicketStatus.Closed)
            {
                return Conflict("Dit ticket is gesloten en kan niet meer beantwoord worden.");
            }

            var occurredAt = DateTime.UtcNow;
            var answeredEvent = new TicketAnsweredEvent(id, command.Answer.Trim(), command.AnsweredBy.Trim(), occurredAt);

            var eventStore = new EventStore(_context);
            eventStore.Append(id, nameof(Ticket), answeredEvent);

            ticket.Answer = command.Answer.Trim();
            ticket.AnsweredBy = command.AnsweredBy.Trim();
            ticket.AnsweredAt = occurredAt;
            ticket.Status = TicketStatus.Answered;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Service Department] Ticket {TicketId} beantwoord door {Employee}.", id, command.AnsweredBy);

            return Ok(ticket);
        }

        [HttpPost("{id:guid}/close")]
        public async Task<IActionResult> Close(Guid id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket is null)
            {
                return NotFound();
            }

            ticket.Status = TicketStatus.Closed;
            await _context.SaveChangesAsync();

            return Ok(ticket);
        }
    }
}
