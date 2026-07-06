using Microsoft.AspNetCore.Mvc;

namespace BallCom.Supplier.API.Controllers
{
    [ApiController]
    [Route("api/supplier/register")]
    public class SupplierRegistrationController : ControllerBase
    {
        private readonly HttpClient _catalogClient;

        public SupplierRegistrationController(IHttpClientFactory httpClientFactory)
        {
            _catalogClient = httpClientFactory.CreateClient("CatalogService");
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] object supplierPayload)
        {
            var response = await _catalogClient.PostAsJsonAsync("api/suppliers", supplierPayload);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorMessage);
            }

            var registeredSupplier = await response.Content.ReadFromJsonAsync<object>();
            return Ok(registeredSupplier);
        }
    }
}
