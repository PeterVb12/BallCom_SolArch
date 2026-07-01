using Microsoft.AspNetCore.Mvc;

namespace BallCom.CustomerServicePortal.API.Controllers
{
    [ApiController]
    [Route("api/service/tickets")]
    public class ServiceTicketsController : ControllerBase
    {
        private readonly HttpClient _customerServiceClient;

        public ServiceTicketsController(IHttpClientFactory httpClientFactory)
        {
            _customerServiceClient = httpClientFactory.CreateClient("CustomerService");
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] object payload)
        {
            var response = await _customerServiceClient.PostAsJsonAsync("api/tickets", payload);
            return await Relay(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var url = string.IsNullOrWhiteSpace(status)
                ? "api/tickets"
                : $"api/tickets?status={Uri.EscapeDataString(status)}";
            var response = await _customerServiceClient.GetAsync(url);
            return await Relay(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _customerServiceClient.GetAsync($"api/tickets/{id}");
            return await Relay(response);
        }

        [HttpPost("{id:guid}/answer")]
        public async Task<IActionResult> Answer(Guid id, [FromBody] object payload)
        {
            var response = await _customerServiceClient.PostAsJsonAsync($"api/tickets/{id}/answer", payload);
            return await Relay(response);
        }

        [HttpPost("{id:guid}/close")]
        public async Task<IActionResult> Close(Guid id)
        {
            var response = await _customerServiceClient.PostAsync($"api/tickets/{id}/close", null);
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
