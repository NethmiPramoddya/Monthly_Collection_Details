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

        // ── Existing endpoint (keep as is) ───────────────────────────────
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

        // ── NEW: GET chq_mnyord by acno_pivno ────────────────────────────
        // POST: api/ReturnChequeCon2/ViewChqMnyord
        [HttpPost("ViewChqMnyord")]
        public async Task<IActionResult> ViewChqMnyord([FromBody] ChqMnyordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.AcnoPivno))
                return BadRequest(new { message = "Account/Pivno number is required." });

            var data = await _service2.GetChqMnyordByAccountAsync(request.AcnoPivno);

            if (data == null || data.Count == 0)
                return NotFound(new { message = "No records found for the given account/pivno number." });

            return Ok(data);
        }

        // ── NEW: GET cheqmy_remarks by prov_code ─────────────────────────
        // POST: api/ReturnChequeCon2/ViewCheqmyRemarks
        [HttpPost("ViewCheqmyRemarks")]
        public async Task<IActionResult> ViewCheqmyRemarks([FromBody] CheqmyRemarkRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.ProvCode))
                return BadRequest(new { message = "Province code is required." });

            var data = await _service2.GetCheqmyRemarksByProvCodeAsync(request.ProvCode);

            if (data == null || data.Count == 0)
                return NotFound(new { message = "No remarks found for the given province code." });

            return Ok(data);
        }

        // ── NEW: GET all cheqmy_chargers ─────────────────────────────────
        // GET: api/ReturnChequeCon2/ViewCheqmyChargers
        [HttpGet("ViewCheqmyChargers")]
        public async Task<IActionResult> ViewCheqmyChargers()
        {
            var data = await _service2.GetAllCheqmyChargersAsync();

            if (data == null || data.Count == 0)
                return NotFound(new { message = "No charger records found." });

            return Ok(data);
        }

        // ── NEW: GET provinces by prov_code ──────────────────────────────
        // POST: api/ReturnChequeCon2/ViewProvince
        [HttpPost("ViewProvince")]
        public async Task<IActionResult> ViewProvince([FromBody] Province2Request request)
        {
            if (string.IsNullOrWhiteSpace(request?.ProvCode))
                return BadRequest(new { message = "Province code is required." });

            var data = await _service2.GetProvinceByCodeAsync(request.ProvCode);

            if (data == null || data.Count == 0)
                return NotFound(new { message = "No province found for the given province code." });

            return Ok(data);
        }
    }
}