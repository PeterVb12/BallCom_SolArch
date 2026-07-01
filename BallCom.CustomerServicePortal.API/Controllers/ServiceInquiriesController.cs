using Microsoft.AspNetCore.Mvc;

namespace BallCom.CustomerServicePortal.API.Controllers
{
    /// <summary>BFF naar Customer Service — klantvragen over orderstatus (F15).</summary>
    [ApiController]
    [Route("api/service/inquiries")]
    public class ServiceInquiriesController : ControllerBase
    {
        private readonly HttpClient _customerServiceClient;

        public ServiceInquiriesController(IHttpClientFactory httpClientFactory)
        {
            _customerServiceClient = httpClientFactory.CreateClient("CustomerService");
        }

        [HttpGet("orders/{orderId:int}/status")]
        public async Task<IActionResult> GetOrderInquiryStatus(int orderId)
        {
            var response = await _customerServiceClient.GetAsync($"api/inquiries/orders/{orderId}/status");
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
