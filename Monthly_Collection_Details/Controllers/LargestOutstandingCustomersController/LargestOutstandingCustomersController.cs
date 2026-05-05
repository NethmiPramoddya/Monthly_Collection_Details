using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Models;
using Monthly_Collection_Details.Services;
using System.Threading.Tasks;

namespace Monthly_Collection_Details.Controllers.LargestOutstandingCustomersController
{
    [ApiController]
    [Route("api/[controller]")]
    public class LargestOutstandingCustomersController : ControllerBase
    {
        private readonly OutstandingCustomerService _service;

        public LargestOutstandingCustomersController(OutstandingCustomerService service)
        {
            _service = service;
        }

        [HttpPost("province")]
        public async Task<IActionResult> GetProvince([FromBody] ProvinceRequest request)
        {
            if (string.IsNullOrEmpty(request.ProvCode))
                return BadRequest("ProvCode is required");

            var data = await _service.GetTopProvinceCustomersAsync(request.ProvCode, request.BillCycle);
            return Ok(data);
        }

        [HttpPost("region")]
        public async Task<IActionResult> GetRegion([FromBody] RegionRequest request)
        {
            if (string.IsNullOrEmpty(request.Region))
                return BadRequest("Region is required");

            var data = await _service.GetTopRegionCustomersAsync(request.Region, request.BillCycle);
            return Ok(data);
        }

        [HttpPost("area")]
        public async Task<IActionResult> GetArea([FromBody] AreaRequest request)
        {
            if (string.IsNullOrEmpty(request.AreaCode))
                return BadRequest("AreaCode is required");

            var data = await _service.GetTopAreaCustomersAsync(request.AreaCode, request.BillCycle);
            return Ok(data);
        }

        [HttpPost("ceb")]
        public async Task<IActionResult> GetCEB([FromBody] AreaRequest request)
        {
            // Only bill cycle needed
            var data = await _service.GetTopCEBCustomersAsync(request.BillCycle);
            return Ok(data);
        }
    }
}