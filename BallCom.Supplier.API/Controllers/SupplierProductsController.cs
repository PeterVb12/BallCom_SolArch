using Microsoft.AspNetCore.Mvc;

namespace BallCom.Supplier.API.Controllers
{
    [ApiController]
    [Route("api/supplier/products")]
    public class SupplierProductsController : ControllerBase
    {
        private readonly HttpClient _catalogClient;

        public SupplierProductsController(IHttpClientFactory httpClientFactory)
        {
            _catalogClient = httpClientFactory.CreateClient("CatalogService");
        }

        // Gateway: supplier voegt een product toe -> doorgezet naar Catalog microservice.
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] object productPayload)
        {
            var response = await _catalogClient.PostAsJsonAsync("api/products", productPayload);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorMessage);
            }

            var createdProduct = await response.Content.ReadFromJsonAsync<object>();
            return Ok(createdProduct);
        }

        // Gateway: supplier bekijkt de beschikbare producten.
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var response = await _catalogClient.GetAsync("api/products");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorMessage);
            }

            var products = await response.Content.ReadFromJsonAsync<object>();
            return Ok(products);
        }
    }
}
