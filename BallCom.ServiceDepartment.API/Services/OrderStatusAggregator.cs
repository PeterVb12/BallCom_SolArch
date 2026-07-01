using System.Text.Json;
using BallCom.ServiceDepartment.API.Models;

namespace BallCom.ServiceDepartment.API.Services
{
    public class OrderStatusAggregator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrderStatusAggregator> _logger;

        public OrderStatusAggregator(IHttpClientFactory httpClientFactory, ILogger<OrderStatusAggregator> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<OrderStatusView?> GetOrderStatusAsync(int orderId)
        {
            var view = new OrderStatusView { OrderId = orderId };

            var orderingClient = _httpClientFactory.CreateClient("OrderingService");
            var orderResponse = await orderingClient.GetAsync($"api/orders/{orderId}");
            if (!orderResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var orderJson = await orderResponse.Content.ReadAsStringAsync();
            using var orderDoc = JsonDocument.Parse(orderJson);
            var orderRoot = orderDoc.RootElement;

            view.OrderStatus = orderRoot.GetProperty("status").GetString();
            view.TotalPrice = orderRoot.GetProperty("totalPrice").GetDecimal();

            if (orderRoot.TryGetProperty("items", out var itemsElement))
            {
                foreach (var item in itemsElement.EnumerateArray())
                {
                    view.Items.Add(new OrderItemView
                    {
                        ProductId = item.GetProperty("productId").GetString() ?? string.Empty,
                        Quantity = item.GetProperty("quantity").GetInt32(),
                        Price = item.GetProperty("price").GetDecimal()
                    });
                }
            }

            var paymentClient = _httpClientFactory.CreateClient("PaymentService");
            var paymentResponse = await paymentClient.GetAsync($"api/payments/{orderId}");
            if (paymentResponse.IsSuccessStatusCode)
            {
                var paymentJson = await paymentResponse.Content.ReadAsStringAsync();
                using var paymentDoc = JsonDocument.Parse(paymentJson);
                var paymentRoot = paymentDoc.RootElement;
                view.PaymentStatus = paymentRoot.GetProperty("status").GetString();
                if (paymentRoot.TryGetProperty("paymentMethod", out var method) && method.ValueKind != JsonValueKind.Null)
                {
                    view.PaymentMethod = method.GetString();
                }
            }

            var warehouseClient = _httpClientFactory.CreateClient("WarehouseService");
            var pickListResponse = await warehouseClient.GetAsync($"api/picklists/order/{orderId}");
            if (pickListResponse.IsSuccessStatusCode)
            {
                var pickListJson = await pickListResponse.Content.ReadAsStringAsync();
                using var pickListDoc = JsonDocument.Parse(pickListJson);
                var pickListRoot = pickListDoc.RootElement;
                view.PickListStatus = pickListRoot.GetProperty("status").GetString();
                if (pickListRoot.TryGetProperty("id", out var idElement))
                {
                    view.PickListId = idElement.GetGuid();
                }
            }

            _logger.LogInformation("[Service Department] Orderstatus samengesteld voor order {OrderId}.", orderId);
            return view;
        }

        public async Task<bool> CanCancelOrModifyAsync(int orderId)
        {
            var warehouseClient = _httpClientFactory.CreateClient("WarehouseService");
            var pickListResponse = await warehouseClient.GetAsync($"api/picklists/order/{orderId}");
            if (!pickListResponse.IsSuccessStatusCode)
            {
                return true;
            }

            var pickListJson = await pickListResponse.Content.ReadAsStringAsync();
            using var pickListDoc = JsonDocument.Parse(pickListJson);
            var status = pickListDoc.RootElement.GetProperty("status").GetString();

            return status is not "READY_FOR_SHIPMENT" and not "CANCELLED";
        }
    }
}
