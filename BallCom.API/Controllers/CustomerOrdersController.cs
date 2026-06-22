using Microsoft.AspNetCore.Mvc;

namespace BallCom.API.Controllers
{
    [ApiController]
    [Route("api/customer/orders")]
    public class CustomerOrdersController : ControllerBase
    {
        private readonly HttpClient _orderingClient;

        public CustomerOrdersController(IHttpClientFactory httpClientFactory)
        {
            _orderingClient = httpClientFactory.CreateClient("OrderingService");
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] object orderPayload)
        {
            // Het portaal ontvangt de klik van de klant en stuurt dit direct door naar de Ordering microservice
            var response = await _orderingClient.PostAsJsonAsync("api/orders", orderPayload);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorMessage);
            }

            var createdOrder = await response.Content.ReadFromJsonAsync<object>();
            return Ok(createdOrder);
        }
    }
}
