using BallCom.Catalog.API.Commands;
using BallCom.Catalog.API.Queries;
using BallCom.Catalog.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace BallCom.Catalog.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {        
        // Onze CQRS Handlers
        private readonly AddProductCommandHandler _addProductHandler;
        private readonly CatalogQueryHandler _catalogQueryHandler; 
        private readonly UpdateProductCommandHandler _updateProductHandler;
        private readonly ReplayProductsCommandHandler _replayProductsHandler;

        // Logger voor foutmeldingen en waarschuwingen
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ILogger<ProductsController> logger,
            CatalogQueryHandler catalogQueryHandler,
            AddProductCommandHandler addProductHandler,
            UpdateProductCommandHandler updateProductHandler,
            ReplayProductsCommandHandler replayProductsHandler
            ) 
        {
            _logger = logger;
            _catalogQueryHandler = catalogQueryHandler;
            _addProductHandler = addProductHandler;
            _updateProductHandler = updateProductHandler;
            _replayProductsHandler = replayProductsHandler;
        }

        // ---------------------------------------------------------------
        // Replay - Event Sourcing: Herstel de read model vanuit de Event Store
        // ---------------------------------------------------------------

        [HttpPost("replay")]
        public async Task<IActionResult> ReplayProducts()
        {
            try
            {
                await _replayProductsHandler.HandleAsync();
                return Ok("De producttabel is succesvol hersteld vanuit de Event Store.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout tijdens het replayeren van de catalogus.");
                return StatusCode(500, $"Replay mislukt: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------
        // CQRS - COMMAND zijde (schrijven)
        // ---------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] AddProductCommand command)
        {
            try
            {
                var product = await _addProductHandler.HandleAsync(command);
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
        {
            try
            {
                var updatedProduct = await _updateProductHandler.HandleAsync(id, command);
                
                if (updatedProduct is null)
                {
                    return NotFound();
                }

                return Ok(updatedProduct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ---------------------------------------------------------------
        // CQRS - QUERY zijde (lezen vanuit het geprojecteerde read model)
        // ---------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            // Maak de query aan en geef hem aan de query handler
            var query = new GetAllProductsQuery();
            var products = await _catalogQueryHandler.HandleAsync(query);
            
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            // Stop het ID in het query contract en geef hem aan de handler
            var query = new GetProductByIdQuery(id);
            var product = await _catalogQueryHandler.HandleAsync(query);
            
            if (product is null) 
            {
                return NotFound();
            }

            return Ok(product);
        }
    }
}