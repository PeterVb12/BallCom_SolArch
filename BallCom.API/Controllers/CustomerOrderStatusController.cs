using Microsoft.AspNetCore.Mvc;

namespace BallCom.API.Controllers
{
    /// <summary>
    /// F12: klant bekijkt orderstatus via Ball.com portal.
    /// Orderstatus uit Ordering (F13), leveringsstatus uit Logistics (F13).
    /// </summary>
    [ApiController]
    [Route("api/customer/orders")]
    public class CustomerOrderStatusController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CustomerOrderStatusController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{orderId:int}/status")]
        public async Task<IActionResult> GetOrderStatus(int orderId)
        {
            var orderingClient = _httpClientFactory.CreateClient("OrderingService");
            var orderResponse = await orderingClient.GetAsync($"api/orders/{orderId}/status");
            if (!orderResponse.IsSuccessStatusCode)
            {
                return NotFound($"Geen order gevonden met id {orderId}.");
            }

            var orderJson = await orderResponse.Content.ReadAsStringAsync();

            var logisticsClient = _httpClientFactory.CreateClient("LogisticsService");
            var deliveryResponse = await logisticsClient.GetAsync($"api/shipments/order/{orderId}/delivery-status");

            if (!deliveryResponse.IsSuccessStatusCode)
            {
                return Content(orderJson, "application/json");
            }

            var deliveryJson = await deliveryResponse.Content.ReadAsStringAsync();

            var combined = $$"""
                {
                  "order": {{orderJson}},
                  "delivery": {{deliveryJson}}
                }
                """;

            return Content(combined, "application/json");
        }
    }
}
