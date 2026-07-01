using Microsoft.AspNetCore.Mvc;

namespace BallCom.API.Controllers
{
    [ApiController]
    [Route("api/customer/payments")]
    public class CustomerPaymentsController : ControllerBase
    {
        private readonly HttpClient _paymentClient;

        public CustomerPaymentsController(IHttpClientFactory httpClientFactory)
        {
            _paymentClient = httpClientFactory.CreateClient("PaymentService");
        }

        [HttpPost]
        public async Task<IActionResult> StartPayment([FromBody] object paymentPayload)
        {
            var response = await _paymentClient.PostAsJsonAsync("api/payments", paymentPayload);
            return await Relay(response);
        }

        [HttpPost("{orderId}/complete")]
        public async Task<IActionResult> CompletePayment(int orderId)
        {
            var response = await _paymentClient.PostAsync($"api/payments/{orderId}/complete", null);
            return await Relay(response);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetPayment(int orderId)
        {
            var response = await _paymentClient.GetAsync($"api/payments/{orderId}");
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
