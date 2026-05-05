using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Models;
using System.Threading.Tasks;
using Monthly_Collection_Details.Services;

namespace Monthly_Collection_Details.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LargestCustomersController : ControllerBase
    {
        private readonly CustomerService _service;

        public LargestCustomersController(CustomerService service)
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
    }
}