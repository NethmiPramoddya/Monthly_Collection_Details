using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Models;
using Monthly_Collection_Details.Services;
using System.Threading.Tasks;

namespace Monthly_Collection_Details.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReturnChequeCon2Controller : ControllerBase
    {
        private readonly Service2 _service2;

        public ReturnChequeCon2Controller(Service2 service2)
        {
            _service2 = service2;
        }

        // POST: api/ReturnChequeCon2/ViewDefaulterDetails
        [HttpPost("ViewDefaulterDetails")]
        public async Task<IActionResult> ViewDefaulterDetails([FromBody] ReturnChequeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.AccountNo))
                return BadRequest(new { message = "Account number is required." });

            var data = await _service2.GetReturnChequeDetailsByAccountAsync(request.AccountNo);

            if (data == null || data.Count == 0)
                return NotFound(new { message = "No defaulter details found for the given account number." });

            return Ok(data);
        }
    }
}