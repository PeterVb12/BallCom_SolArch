using Microsoft.AspNetCore.Mvc;

namespace BallCom.CustomerServicePortal.API.Controllers
{
    /// <summary>BFF naar Logistics — leveringsstatus voor service-medewerkers (diagram: portal → Logistic).</summary>
    [ApiController]
    [Route("api/service/delivery")]
    public class ServiceDeliveryController : ControllerBase
    {
        private readonly HttpClient _logisticsClient;

        public ServiceDeliveryController(IHttpClientFactory httpClientFactory)
        {
            _logisticsClient = httpClientFactory.CreateClient("LogisticsService");
        }

        [HttpGet("orders/{orderId:int}/status")]
        public async Task<IActionResult> GetDeliveryStatus(int orderId)
        {
            var response = await _logisticsClient.GetAsync($"api/shipments/order/{orderId}/delivery-status");
            return await Relay(response);
        }

        [HttpGet("orders/{orderId:int}")]
        public async Task<IActionResult> GetShipment(int orderId)
        {
            var response = await _logisticsClient.GetAsync($"api/shipments/order/{orderId}");
            return await Relay(response);
        }

        private async Task<IActionResult> Relay(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, content);
            }

            return Content(content, "application/json");
        }
    }
}
