using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BallCom.Ordering.API.Application.Queries
{
    public class OrderQueryHandler
    {
        private readonly OrderingReadDbContext _read;

        public OrderQueryHandler(OrderingReadDbContext read)
        {
            _read = read;
        }

        public async Task<OrderView?> GetByIdAsync(int orderId)
        {
            var summary = await _read.OrderSummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (summary is null)
            {
                return null;
            }

            var lines = await _read.OrderLineViews
                .AsNoTracking()
                .Where(l => l.OrderId == orderId)
                .ToListAsync();

            return new OrderView(summary, lines);
        }

        public async Task<List<OrderSummary>> GetAllAsync()
            => await _read.OrderSummaries.AsNoTracking().OrderByDescending(o => o.OrderId).ToListAsync();

        public async Task<List<CustomerOrderStat>> GetCustomerStatsAsync()
            => await _read.CustomerOrderStats.AsNoTracking().OrderByDescending(c => c.TotalSpent).ToListAsync();
    }

    public record OrderView(OrderSummary Summary, List<OrderLineView> Lines);
}
