using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Models;
using Monthly_Collection_Details.Services;

namespace Monthly_Collection_Details.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnChequeCon1Controller : ControllerBase
    {
        // Only one service needed now instead of two
        private readonly Service1 _service;

        public ReturnChequeCon1Controller(Service1 service)
        {
            _service = service;
        }

        // GET api/returnchequecon1/branches
        [HttpGet("branches")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();

            if (data == null || data.Count == 0)
                return NotFound("No records found in cheqmy_no.");

            return Ok(data);
        }

        // POST api/returnchequecon1/report
        [HttpPost("report")]
        public async Task<IActionResult> GetReport([FromBody] Model1.ChequeReportRequest request)
        {
            if (request == null)
                return BadRequest("Request is null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (request.FromDate > today)
                return BadRequest("From date cannot be in the future.");

            request.ToDate = today;

            var data = await _service.GetReportAsync(
                request.MyAddCode,
                request.FromDate,
                request.ToDate);

            if (data == null || data.Count == 0)
                return NotFound("No records found for the given province and date range.");

            return Ok(data);
        }
    }
}