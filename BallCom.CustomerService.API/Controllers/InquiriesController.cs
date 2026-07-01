using BallCom.CustomerService.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BallCom.CustomerService.API.Controllers
{
    [ApiController]
    [Route("api/inquiries")]
    public class InquiriesController : ControllerBase
    {
        private readonly InquiryStatusAggregator _statusAggregator;

        public InquiriesController(InquiryStatusAggregator statusAggregator)
        {
            _statusAggregator = statusAggregator;
        }

        /// <summary>Leest order- en leveringsstatus om klantvragen te beantwoorden (F15).</summary>
        [HttpGet("orders/{orderId:int}/status")]
        public async Task<IActionResult> GetOrderInquiryStatus(int orderId)
        {
            var status = await _statusAggregator.GetInquiryStatusAsync(orderId);
            if (status is null)
            {
                return NotFound($"Geen order gevonden met id {orderId}.");
            }

            return Ok(status);
        }
    }
}
