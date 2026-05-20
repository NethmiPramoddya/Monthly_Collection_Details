using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Models;
using Monthly_Collection_Details.Services;

namespace Monthly_Collection_Details.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnChequeCon1Controller : ControllerBase
    {
        private readonly Service1 _service;
        private readonly PdfService _pdfService;
        private readonly IWebHostEnvironment _env; 

        // ONE constructor — injects all three dependencies
        public ReturnChequeCon1Controller(
            Service1 service,
            PdfService pdfService,
            IWebHostEnvironment env)  // ← added
        {
            _service = service;
            _pdfService = pdfService;
            _env = env;        // ← added
        }

        // ── GET api/returnchequecon1/branches ─────────────────────────────────
        // Returns all rows from cheqmy_no — used for province dropdown on login
        [HttpGet("branches")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();

            if (data == null || data.Count == 0)
                return NotFound("No records found in cheqmy_no.");

            return Ok(data);
        }

        // ── POST api/returnchequecon1/report ──────────────────────────────────
        // Body: { "myAddCode": "WP", "fromDate": "2024-01-01", "toDate": "2024-05-05" }
        // toDate is always overridden to today server-side
        [HttpPost("report")]
        public async Task<IActionResult> GetReport(
            [FromBody] Model1.ChequeReportRequest request)
        {
            if (request == null)
                return BadRequest("Request is null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (request.FromDate > today)
                return BadRequest("From date cannot be in the future.");

            // Always lock toDate to today — user cannot see future records
            request.ToDate = today;

            var data = await _service.GetReportAsync(
                request.MyAddCode,
                request.FromDate,
                request.ToDate);

            if (data == null || data.Count == 0)
                return NotFound("No records found for the given province and date range.");

            return Ok(data);
        }

        // ── GET api/returnchequecon1/notice-pdf ───────────────────────────────
        // Query params: noticeNo, myAddCode
        // Returns a generated PDF file as a download
        // myAddCode is locked from login — prevents cross-province access
        [HttpGet("notice-pdf")]
        public async Task<IActionResult> DownloadNoticePdf(
            [FromQuery] string noticeNo,
            [FromQuery] string myAddCode)
        {
            if (string.IsNullOrWhiteSpace(noticeNo) ||
                string.IsNullOrWhiteSpace(myAddCode))
                return BadRequest("noticeNo and myAddCode are required.");

            // Fetch record — myAddCode enforces province restriction
            // Returns null if noticeNo belongs to a different province
            var detail = await _service.GetNoticeDetailAsync(noticeNo.Trim(), myAddCode.Trim());

            if (detail == null)
                return NotFound("Notice not found or access denied.");

            // Generate the filled PDF from the template
            var pdfBytes = _pdfService.GenerateNotice(detail);

            // Return as a downloadable PDF file
            return File(
                pdfBytes,
                "application/pdf",
                $"Notice_{noticeNo.Replace("/", "-")}.pdf"
            );
        }

        // ── GET api/returnchequecon1/pdf-grid ─────────────────────────────────
        // TEMPORARY DEBUG ENDPOINT — remove after coordinates are confirmed
        // Downloads a copy of the template with a red coordinate grid overlay
        // Use this to find the exact X,Y positions for each field
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

            canvas.SetStrokeColor(ColorConstants.RED).SetLineWidth(0.1f); // thinner line for 10pt grid

            // Draw vertical lines every 10 points
            for (float x = 0; x < pageWidth; x += 10)
            {
                // Draw thicker line and label only every 50 points for readability
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
                    // Thinner line for every 10pt mark — no label to avoid clutter
                    canvas.SetLineWidth(0.1f);
                    canvas.MoveTo(x, 0).LineTo(x, pageHeight).Stroke();
                }
            }

            // Draw horizontal lines every 10 points
            for (float topY = 0; topY < pageHeight; topY += 10)
            {
                // Draw thicker line and label only every 50 points for readability
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
                    // Thinner line for every 10pt mark — no label to avoid clutter
                    canvas.SetLineWidth(0.1f);
                    canvas.MoveTo(0, pageHeight - topY)
                          .LineTo(pageWidth, pageHeight - topY).Stroke();
                }
            }

            canvas.Release();
            pdfDoc.Close();

            return File(outputStream.ToArray(), "application/pdf", "grid.pdf");
        }

        // ── GET api/returnchequecon1/bill-cycles ──────────────────────────
        // Returns distinct bill cycles from prn_dat_1 for the dropdown
        [HttpGet("bill-cycles")]
        public async Task<IActionResult> GetBillCycles()
        {
            var data = await _service.GetBillCyclesAsync();

            if (data == null || data.Count == 0)
                return NotFound("No bill cycles found.");

            return Ok(data);
        }

        // ── POST api/returnchequecon1/search-by-account ───────────────────
        // Body: { "accountNo": "...", "receivedDate": "dd/MM/yyyy", "billCycle": 3 }
        [HttpPost("search-by-account")]
        public async Task<IActionResult> SearchByAccount(
            [FromBody] Model1.SearchByAccountRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            // Parse dd/MM/yyyy safely — reject malformed dates
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

        // ── POST api/returnchequecon1/search-by-cheque ────────────────────
        // Body: { "chequeNo": "...", "bankCode": "...", "branchCode": "...", "billCycle": 3 }
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

        // ── TEMPORARY DEBUG — remove after confirming raw API response ────
        [HttpGet("bill-cycles-raw")]
        public async Task<IActionResult> GetBillCyclesRaw()
        {
            var client = new HttpClient();
            var json = await client.GetStringAsync("http://10.128.1.227:5010/api/BulkEmail/billinfo");

            // Returns the raw string so you can see exact field names
            return Content(json, "application/json");
        }

        // ── POST api/returnchequecon1/search-90day-by-account ─────────────
        // Body: { "accountNo": "...", "receivedDate": "dd/MM/yyyy", "billCycle": 448 }
        // Returns all cheques for that account in the last 90 days
        // grouped by ChequeNo → dropdown + cards
        [HttpPost("search-90day-by-account")]
        public async Task<IActionResult> Search90DayByAccount(
            [FromBody] Model1.SearchByAccountRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            // Parse the POS received date — if blank/invalid, anchor to today
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

            // Return 200 with empty lists — frontend handles the empty state
            return Ok(result);
        }

        // ── POST api/returnchequecon1/search-90day-by-cheque ──────────────
        // Body: { "chequeNo": "...", "bankCode": "...", "branchCode": "...", "billCycle": 448 }
        // Returns all accounts paid by this cheque in the last 90 days
        // grouped by AccountNo → dropdown + cards
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

        //insertion
        // ── GET api/returnchequecon1/remarks ──────────────────────────────
        // ▼▼▼ NEW ▼▼▼
        // Returns all remark options for the Remark dropdown in the insert form
        [HttpGet("remarks")]
        public async Task<IActionResult> GetRemarks()
        {
            var data = await _service.GetRemarksAsync();

            if (data == null || data.Count == 0)
                return NotFound("No remarks found.");

            return Ok(data);
        }

        // ── GET api/returnchequecon1/charges-config ───────────────────────
        // ▼▼▼ NEW ▼▼▼
        // Returns postage, bank charges, surcharge %, and no_months from
        // cheqmy_chargers so the insert form can pre-fill those fields
        [HttpGet("charges-config")]
        public async Task<IActionResult> GetChargesConfig()
        {
            var config = await _service.GetChargesConfigAsync();

            if (config == null)
                return NotFound("No charges configuration found.");

            return Ok(config);
        }

        // ── POST api/returnchequecon1/save-cheque-details ─────────────────
        // ▼▼▼ NEW ▼▼▼
        // Saves a new defaulter record into cheqmy_details.
        // Returns the generated notice number (my_code) on success.
        [HttpPost("save-cheque-details")]
        public async Task<IActionResult> SaveChequeDetails(
            [FromBody] Model1.SaveChequeDetailsRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate no_months minimum (same rule as legacy system)
            if (request.NoMonths < 3)
                return BadRequest("No of months must be at least 3.");

            // Validate cheq_date format
            if (!DateTime.TryParseExact(
                    request.CheqDate.Trim(),
                    "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out _))
            {
                return BadRequest("CheqDate must be in dd/MM/yyyy format.");
            }

            try
            {
                string noticeNo = await _service.SaveChequeDetailsAsync(request);
                return Ok(new { noticeNo, message = "Cheque details saved successfully." });
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate cheque detected in service layer
                return Conflict(ex.Message);
            }
        }

    }
}