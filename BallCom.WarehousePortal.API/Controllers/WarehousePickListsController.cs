using Microsoft.AspNetCore.Mvc;

namespace BallCom.WarehousePortal.API.Controllers
{
    [ApiController]
    [Route("api/warehouse/picklists")]
    public class WarehousePickListsController : ControllerBase
    {
        private readonly HttpClient _warehouseClient;

        public WarehousePickListsController(IHttpClientFactory httpClientFactory)
        {
            _warehouseClient = httpClientFactory.CreateClient("WarehouseService");
        }

        [HttpGet]
        public async Task<IActionResult> GetPickLists()
        {
            var response = await _warehouseClient.GetAsync("api/picklists");
            return await Relay(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPickList(Guid id)
        {
            var response = await _warehouseClient.GetAsync($"api/picklists/{id}");
            return await Relay(response);
        }

        [HttpPost("{id}/start-picking")]
        public async Task<IActionResult> StartPicking(Guid id)
        {
            var response = await _warehouseClient.PostAsync($"api/picklists/{id}/start-picking", null);
            return await Relay(response);
        }

        [HttpPost("{id}/complete-picking")]
        public async Task<IActionResult> CompletePicking(Guid id)
        {
            var response = await _warehouseClient.PostAsync($"api/picklists/{id}/complete-picking", null);
            return await Relay(response);
        }

        [HttpPost("{id}/pack")]
        public async Task<IActionResult> Pack(Guid id)
        {
            var response = await _warehouseClient.PostAsync($"api/picklists/{id}/pack", null);
            return await Relay(response);
        }

        [HttpPost("{id}/ready")]
        public async Task<IActionResult> Ready(Guid id)
        {
            var response = await _warehouseClient.PostAsync($"api/picklists/{id}/ready", null);
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
