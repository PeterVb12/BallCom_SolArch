using System.Text.Json;
using BallCom.CustomerService.API.Models;

namespace BallCom.CustomerService.API.Services
{
    /// <summary>
    /// Leest orderstatus bij Ordering (F13) en leveringsstatus bij Logistics (F13) voor klantvragen.
    /// </summary>
    public class InquiryStatusAggregator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InquiryStatusAggregator> _logger;

        public InquiryStatusAggregator(IHttpClientFactory httpClientFactory, ILogger<InquiryStatusAggregator> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<OrderInquiryView?> GetInquiryStatusAsync(int orderId)
        {
            var view = new OrderInquiryView { OrderId = orderId };

            var orderingClient = _httpClientFactory.CreateClient("OrderingService");
            var orderResponse = await orderingClient.GetAsync($"api/orders/{orderId}/status");
            if (!orderResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var orderJson = await orderResponse.Content.ReadAsStringAsync();
            using var orderDoc = JsonDocument.Parse(orderJson);
            var orderRoot = orderDoc.RootElement;

            view.OrderStatus = orderRoot.GetProperty("orderStatus").GetString();
            view.TotalPrice = orderRoot.GetProperty("totalPrice").GetDecimal();
            view.CustomerEmail = orderRoot.GetProperty("customerEmail").GetString();
            view.CustomerName = orderRoot.GetProperty("customerName").GetString();

            var logisticsClient = _httpClientFactory.CreateClient("LogisticsService");
            var deliveryResponse = await logisticsClient.GetAsync($"api/shipments/order/{orderId}/delivery-status");
            if (deliveryResponse.IsSuccessStatusCode)
            {
                var deliveryJson = await deliveryResponse.Content.ReadAsStringAsync();
                using var deliveryDoc = JsonDocument.Parse(deliveryJson);
                var deliveryRoot = deliveryDoc.RootElement;
                view.DeliveryStatus = deliveryRoot.GetProperty("deliveryStatus").GetString();
                view.Carrier = deliveryRoot.GetProperty("carrier").GetString();
                view.TrackingNumber = deliveryRoot.GetProperty("trackingNumber").GetString();
            }

            _logger.LogInformation("[Customer Service] Inquiry status samengesteld voor order {OrderId}.", orderId);
            return view;
        }
    }
}
