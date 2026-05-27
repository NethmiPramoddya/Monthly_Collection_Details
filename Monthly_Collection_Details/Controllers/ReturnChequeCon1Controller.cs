// ══════════════════════════════════════════════════════════════════════════════
// FILE: ReturnChequeCon1Controller.cs - UPDATED SaveChequeDetails METHOD
// 
// CHANGE: Instead of reading myAddCode from JWT claim, read it from request body
// This works for projects NOT using JWT authentication
// ══════════════════════════════════════════════════════════════════════════════

using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Models;
using Monthly_Collection_Details.Services;
using static Monthly_Collection_Details.Models.Model1;

namespace Monthly_Collection_Details.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnChequeCon1Controller : ControllerBase
    {
        private readonly Service1 _service;
        private readonly PdfService _pdfService;
        private readonly IWebHostEnvironment _env;

        public ReturnChequeCon1Controller(
            Service1 service,
            PdfService pdfService,
            IWebHostEnvironment env)
        {
            _service = service;
            _pdfService = pdfService;
            _env = env;
        }

        // ── All existing methods remain the same ──────────────────────────────

        [HttpGet("branches")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            if (data == null || data.Count == 0)
                return NotFound("No records found in cheqmy_no.");
            return Ok(data);
        }

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

        [HttpGet("notice-pdf")]
        public async Task<IActionResult> DownloadNoticePdf(
            [FromQuery] string noticeNo,
            [FromQuery] string myAddCode)
        {
            if (string.IsNullOrWhiteSpace(noticeNo) ||
                string.IsNullOrWhiteSpace(myAddCode))
                return BadRequest("noticeNo and myAddCode are required.");

            var detail = await _service.GetNoticeDetailAsync(noticeNo.Trim(), myAddCode.Trim());

            if (detail == null)
                return NotFound("Notice not found or access denied.");

            var pdfBytes = _pdfService.GenerateNotice(detail);

            return File(
                pdfBytes,
                "application/pdf",
                $"Notice_{noticeNo.Replace("/", "-")}.pdf"
            );
        }

        [HttpGet("pdf-grid")]
        public IActionResult GetPdfWithGrid()
        {
            var templatePath = Path.Combine(
                _env.WebRootPath, "templates", "cheque_notice_template.pdf");

            if (!System.IO.File.Exists(templatePath))
                return NotFound("Template file not found at wwwroot/templates/cheque_notice_template.pdf");

            using var outputStream = new MemoryStream();
            using var reader = new PdfReader(templatePath);
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(reader, writer);

            var page = pdfDoc.GetFirstPage();
            var canvas = new PdfCanvas(page);
            var pageHeight = page.GetPageSize().GetHeight();
            var pageWidth = page.GetPageSize().GetWidth();
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            canvas.SetStrokeColor(ColorConstants.RED).SetLineWidth(0.1f);

            for (float x = 0; x < pageWidth; x += 10)
            {
                if (x % 50 == 0)
                {
                    canvas.SetLineWidth(0.5f);
                    canvas.MoveTo(x, 0).LineTo(x, pageHeight).Stroke();
                    canvas.BeginText()
                          .SetFontAndSize(font, 6)
                          .SetColor(ColorConstants.RED, true)
                          .MoveText(x + 1, pageHeight - 10)
                          .ShowText($"{(int)x}")
                          .EndText();
                }
                else
                {
                    canvas.SetLineWidth(0.1f);
                    canvas.MoveTo(x, 0).LineTo(x, pageHeight).Stroke();
                }
            }

            for (float topY = 0; topY < pageHeight; topY += 10)
            {
                if (topY % 50 == 0)
                {
                    canvas.SetLineWidth(0.5f);
                    canvas.MoveTo(0, pageHeight - topY)
                          .LineTo(pageWidth, pageHeight - topY).Stroke();
                    canvas.BeginText()
                          .SetFontAndSize(font, 6)
                          .SetColor(ColorConstants.RED, true)
                          .MoveText(2, pageHeight - topY - 8)
                          .ShowText($"{(int)topY}")
                          .EndText();
                }
                else
                {
                    canvas.SetLineWidth(0.1f);
                    canvas.MoveTo(0, pageHeight - topY)
                          .LineTo(pageWidth, pageHeight - topY).Stroke();
                }
            }

            canvas.Release();
            pdfDoc.Close();

            return File(outputStream.ToArray(), "application/pdf", "grid.pdf");
        }

        [HttpGet("bill-cycles")]
        public async Task<IActionResult> GetBillCycles()
        {
            var data = await _service.GetBillCyclesAsync();
            if (data == null || data.Count == 0)
                return NotFound("No bill cycles found.");
            return Ok(data);
        }

        [HttpPost("search-by-account")]
        public async Task<IActionResult> SearchByAccount(
            [FromBody] Model1.SearchByAccountRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (!DateTime.TryParseExact(
                    request.ReceivedDate.Trim(),
                    "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime receivedDate))
            {
                return BadRequest("ReceivedDate must be in dd/MM/yyyy format.");
            }

            var data = await _service.SearchByAccountAsync(
                request.AccountNo.Trim(),
                receivedDate,
                request.BillCycle);

            if (data == null || data.Count == 0)
                return NotFound("No records found for the given account number.");

            return Ok(data);
        }

        [HttpPost("search-by-cheque")]
        public async Task<IActionResult> SearchByCheque(
            [FromBody] Model1.SearchByChequeRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            var data = await _service.SearchByChequeAsync(
                request.ChequeNo.Trim(),
                request.BankCode.Trim(),
                request.BranchCode.Trim(),
                request.BillCycle);

            if (data == null || data.Count == 0)
                return NotFound("No records found for the given cheque details.");

            return Ok(data);
        }

        [HttpGet("bill-cycles-raw")]
        public async Task<IActionResult> GetBillCyclesRaw()
        {
            var client = new HttpClient();
            var json = await client.GetStringAsync("http://10.128.1.227:5010/api/BulkEmail/billinfo");
            return Content(json, "application/json");
        }

        [HttpPost("search-90day-by-account")]
        public async Task<IActionResult> Search90DayByAccount(
            [FromBody] Model1.SearchByAccountRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            DateTime anchorDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(request.ReceivedDate))
            {
                if (!DateTime.TryParseExact(
                        request.ReceivedDate.Trim(),
                        "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out anchorDate))
                {
                    return BadRequest("ReceivedDate must be in dd/MM/yyyy format.");
                }
            }

            var result = await _service.Search90DayByAccountAsync(
                request.AccountNo.Trim(),
                anchorDate,
                request.BillCycle);

            return Ok(result);
        }

        [HttpPost("search-90day-by-cheque")]
        public async Task<IActionResult> Search90DayByCheque(
            [FromBody] Model1.SearchByChequeRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.Search90DayByChequeAsync(
                request.ChequeNo.Trim(),
                request.BankCode.Trim(),
                request.BranchCode.Trim(),
                request.BillCycle);

            return Ok(result);
        }

        [HttpGet("remarks")]
        public async Task<IActionResult> GetRemarks()
        {
            var data = await _service.GetRemarksAsync();
            if (data == null || data.Count == 0)
                return NotFound("No remarks found.");
            return Ok(data);
        }

        [HttpGet("charges-config")]
        public async Task<IActionResult> GetChargesConfig()
        {
            var config = await _service.GetChargesConfigAsync();
            if (config == null)
                return NotFound("No charges configuration found.");
            return Ok(config);
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // ✅ FIXED: SaveChequeDetails - Now reads myAddCode from REQUEST BODY, not JWT
        // ══════════════════════════════════════════════════════════════════════════════
        [HttpPost("save-cheque-details")]
        public async Task<IActionResult> SaveChequeDetails([FromBody] SaveChequeDetailsRequest req)
        {
            // ✅ FIX 1: No longer trying to read from JWT claims
            // Instead, read myAddCode directly from the request body (frontend sends it)

            if (req == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ FIX 2: Validate myAddCode is provided in request
            if (string.IsNullOrWhiteSpace(req.MyAddCode))
                return BadRequest("MyAddCode is required.");

            if (string.IsNullOrWhiteSpace(req.MyCode))
                return BadRequest("Notice number is required.");

            if (req.NoMonths < 3)
                return BadRequest("No of months must be at least 3.");

            if (!DateTime.TryParseExact(
                    req.CheqDate.Trim(),
                    "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out _))
            {
                return BadRequest("CheqDate must be in dd/MM/yyyy format.");
            }

            // CustLname: treat null/empty as empty string — it's valid
            req.CustLname ??= "";

            try
            {
                // Call the service to save
                var noticeNo = await _service.SaveChequeDetailsAsync(req);

                return Ok(new
                {
                    success = true,
                    noticeNo = noticeNo,
                    message = "Saved successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate cheque detected
                return Conflict(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                // Validation error (e.g., date format)
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Unexpected error
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
    }
}