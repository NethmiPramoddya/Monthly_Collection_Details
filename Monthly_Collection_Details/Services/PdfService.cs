using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.AspNetCore.Hosting;
using Monthly_Collection_Details.Models;
using iText.IO.Font;

namespace Monthly_Collection_Details.Services
{
    // ══════════════════════════════════════════════════════════════════════════
    // PROVINCE DRAWER INTERFACE
    // ──────────────────────────────────────────────────────────────────────────
    // Each province template that differs in its charges paragraph layout
    // implements this interface.  PdfService calls DrawChargesParagraph() at
    // the exact point where those values are written — every other field is
    // shared across all templates.
    //
    // To add a new province with a unique layout:
    //   1. Create a class that implements IProvinceDrawer below.
    //   2. Add it to PdfService.ProvinceDrawers dictionary.
    //   Done — no other code changes needed.
    // ══════════════════════════════════════════════════════════════════════════
    public interface IProvinceDrawer
    {
        /// <summary>
        /// Writes the dynamic values (Total, Postage, BankCharges, Percentage)
        /// into the charges paragraph of the notice.
        /// </summary>
        /// <param name="d">Full notice detail from the database.</param>
        /// <param name="writeText">
        ///   Delegate to PdfService's inner WriteText helper.
        ///   Signature: (text, x, topY, font, fontSize)
        ///   topY is measured from the TOP of the page (same as pdfplumber output).
        /// </param>
        /// <param name="fontRegular">Helvetica regular — matches template body font.</param>
        void DrawChargesParagraph(
            Model1.ChequeNoticeDetail d,
            Action<string, float, float, PdfFont, float> writeText,
            PdfFont fontRegular);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // COLOMBO CITY DRAWER
    // ──────────────────────────────────────────────────────────────────────────
    // Used by ALL provinces except NWP (code "8").
    //
    // Coordinates derived from pdfplumber analysis of the live template PDF.
    // Page height = 842.25pt, all topY values are from TOP of page (pdfplumber
    // "top" / "bottom" fields map directly to iText topY).
    //
    // Template paragraph blank positions:
    //   Line 258 — "...pay immediately a sum of Rs. [TOTAL] being..."
    //              Blank x=387–465, baseline bottom≈272 → topY=272
    //
    //   Line 270 — "dishonoured cheque, Rs. [POSTAGE]/- for postage,
    //               Rs. [BANKCHARGES]/- for dishonoured cheque..."
    //              Postage blank  x=179–257, BankCharges blank x=350–428
    //              Baseline bottom≈280 → topY=280
    //
    //   Line 281 — "charges charged by the bank and surcharge of [PERCENTAGE]%..."
    //              Blank x=278–311, baseline bottom≈291 → topY=291
    // ══════════════════════════════════════════════════════════════════════════
    public class ColomboCityDrawer : IProvinceDrawer
    {
        public void DrawChargesParagraph(
            Model1.ChequeNoticeDetail d,
            Action<string, float, float, PdfFont, float> writeText,
            PdfFont fontRegular)
        {
            // Line 1 — "...pay immediately a sum of Rs. ___ being"
            // Blank starts at x=387, baseline topY≈272
            writeText($"{d.Total:F2}", 400, 268, fontRegular, 10);

            // Line 2 — "...Rs. ___/- for postage, Rs. ___/- for dishonoured cheque charges..."
            // Postage blank starts at x=179, BankCharges blank starts at x=350, baseline topY≈280
            writeText($"{d.Postage:F0}", 179, 279, fontRegular, 10);
            writeText($"{d.BankCharges:F0}", 300, 279, fontRegular, 10);

            // Line 3 — "...surcharge of ___% on the value..."
            // Blank starts at x=278, baseline topY≈291
            writeText($"{d.Percentage:F0}", 170, 290, fontRegular, 10);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // NWP DRAWER  (province code "8" only)
    // ──────────────────────────────────────────────────────────────────────────
    // Template paragraph (gaps shown as ___):
    //   "...pay immediately a sum of Rs. _TOTAL_ being
    //    the value of dishonoured cheque, Rs _POSTAGE_ for postage,
    //    Rs. _BANKCHARGES_ for dishonoured cheque charges
    //    charged by the bank and surcharge of Rs._SURCHARGE_ on the value..."
    //
    // KEY DIFFERENCES vs all other provinces:
    //   - No "/- " suffix after postage or bank charges
    //   - BankCharges blank is wider / positioned differently (x=344 vs x=350)
    //   - Line 3 writes surcharge amount (Rs.X) instead of percentage
    //
    // NOTE: If the NWP template PDF blank positions differ from the Colombo City
    // template, measure them with pdfplumber against the NWP template file and
    // update the x / topY values below accordingly.
    // ══════════════════════════════════════════════════════════════════════════
    public class NwpDrawer : IProvinceDrawer
    {
        public void DrawChargesParagraph(
            Model1.ChequeNoticeDetail d,
            Action<string, float, float, PdfFont, float> writeText,
            PdfFont fontRegular)
        {
            // Line 1 — "...pay immediately a sum of Rs. ___ being"
            // Blank starts at x=387, baseline topY≈272 (same line structure as Colombo)
            writeText($"{d.Total:F2}", 387, 272, fontRegular, 10);

            // Line 2 — "...Rs ___ for postage, Rs. ___ for dishonoured cheque charges..."
            // NWP has no "/- " suffix; BankCharges blank positioned slightly differently
            writeText($"{d.Postage:F0}", 179, 280, fontRegular, 10);
            writeText($"{d.BankCharges:F0}", 344, 280, fontRegular, 10);

            // Line 3 — "...surcharge of Rs.___ on the value..."
            // NWP writes a currency amount (Rs.X), not a percentage
            // Blank starts at x=278, baseline topY≈291
            writeText($"Rs.{d.Surcharge:F0}", 278, 291, fontRegular, 10);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PDF SERVICE
    // ══════════════════════════════════════════════════════════════════════════
    public class PdfService
    {
        private readonly string _templatesFolder;
        private readonly string _imagesFolder;

        // ── Province → (template filename, logo filename) ─────────────────────
        // Key   = myadd_code exactly as stored in cheqmy_no (trimmed, upper-case)
        // Value = (PDF template filename, logo PNG filename)
        // Both files live in wwwroot/templates/ and wwwroot/images/ respectively.
        //
        // To add a new province:
        //   1. Drop the .pdf into wwwroot/templates/
        //   2. Drop the .png into wwwroot/images/
        //   3. Add one line here.
        // ─────────────────────────────────────────────────────────────────────
        private static readonly Dictionary<string, (string Template, string Logo)> ProvinceFiles =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["1"] = ("1- Western Province North.pdf", "1- Western Province North.png"),
                ["2"] = ("2 - WPSII.pdf", "2 - WPSII.png"),
                ["4"] = ("4 - Northern province.pdf", "4 - Northern province.png"),
                ["5"] = ("5 - central province.pdf", "5 - central province.png"),
                ["6"] = ("6 - Uva Province.pdf", "6 - Uva Province.png"),
                ["7"] = ("7- eastern province.pdf", "7- eastern province.png"),
                ["8"] = ("8 - NWP.pdf", "8 - NWP.png"),
                ["9"] = ("9 - Sabaragamuwa.pdf", "9 - Sabaragamuwa.png"),
                ["111"] = ("111 - Head Office.pdf", "111 - Head Office.png"),
                ["222"] = ("222 - Colombo City.pdf", "222 - Colombo City.png"),
                ["A"] = ("A - North Central Province.pdf", "A - North Central Province.png"),
                ["B"] = ("B - Southern Province.pdf", "B - Southern Province.png"),
                ["C"] = ("C - WPS1.pdf", "C - WPS1.png"),
                ["D"] = ("D - Noerh Western Province 2.pdf", "D - Noerh Western Province 2.png"),
                ["E"] = ("E - Central Province 2.pdf", "E - Central Province 2.png"),
                ["F"] = ("F - Southern Province 2.pdf", "F - Southern Province 2.png"),
            };

        // ── Province → drawing strategy ───────────────────────────────────────
        // All 16 provinces are explicitly mapped here for clarity.
        // Only "8" (NWP) uses NwpDrawer — every other province uses ColomboCityDrawer.
        // DefaultDrawer below acts as a safety net for any unknown code.
        // ─────────────────────────────────────────────────────────────────────
        private static readonly Dictionary<string, IProvinceDrawer> ProvinceDrawers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["1"] = new ColomboCityDrawer(),
                ["2"] = new ColomboCityDrawer(),
                ["4"] = new ColomboCityDrawer(),
                ["5"] = new ColomboCityDrawer(),
                ["6"] = new ColomboCityDrawer(),
                ["7"] = new ColomboCityDrawer(),
                ["8"] = new NwpDrawer(),            // ← only exception
                ["9"] = new ColomboCityDrawer(),
                ["111"] = new ColomboCityDrawer(),
                ["222"] = new ColomboCityDrawer(),
                ["A"] = new ColomboCityDrawer(),
                ["B"] = new ColomboCityDrawer(),
                ["C"] = new ColomboCityDrawer(),
                ["D"] = new ColomboCityDrawer(),
                ["E"] = new ColomboCityDrawer(),
                ["F"] = new ColomboCityDrawer(),
            };

        // Safety net — if an unknown province code somehow reaches GenerateNotice,
        // ColomboCityDrawer is used rather than crashing the PDF generation.
        private static readonly IProvinceDrawer DefaultDrawer = new ColomboCityDrawer();

        public PdfService(IWebHostEnvironment env)
        {
            _templatesFolder = Path.Combine(env.WebRootPath, "templates");
            _imagesFolder = Path.Combine(env.WebRootPath, "images");
        }

        // ── Resolve template + logo paths for a given province code ───────────
        // Throws clearly if the province code is unknown or the template file is
        // missing, so you get an actionable error instead of a silent wrong-template bug.
        // ─────────────────────────────────────────────────────────────────────
        private (string TemplatePath, string LogoPath) ResolvePaths(string myAddCode)
        {
            string key = myAddCode.Trim().ToUpper();

            if (!ProvinceFiles.TryGetValue(key, out var files))
                throw new KeyNotFoundException(
                    $"No template mapping found for province code '{myAddCode}'. " +
                    $"Add it to PdfService.ProvinceFiles.");

            string templatePath = Path.Combine(_templatesFolder, files.Template);
            string logoPath = Path.Combine(_imagesFolder, files.Logo);

            if (!File.Exists(templatePath))
                throw new FileNotFoundException(
                    $"PDF template not found on disk: {templatePath}");

            // Logo missing is non-fatal — DrawImage skips gracefully.
            return (templatePath, logoPath);
        }

        // ══════════════════════════════════════════════════════════════════════
        // MAIN ENTRY POINT
        // d.MyAddCode (populated by GetNoticeDetailAsync from the DB) drives
        // which template and logo are used. The controller does not change.
        // ══════════════════════════════════════════════════════════════════════
        public byte[] GenerateNotice(Model1.ChequeNoticeDetail d)
        {
            var (templatePath, logoPath) = ResolvePaths(d.MyAddCode);

            using var outputStream = new MemoryStream();
            using var reader = new PdfReader(templatePath);
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(reader, writer);

            var page = pdfDoc.GetFirstPage();
            var canvas = new PdfCanvas(page);
            var pageHeight = page.GetPageSize().GetHeight();

            // ── Fonts ─────────────────────────────────────────────────────────
            var fontRegular = PdfFontFactory.CreateFont("C:/Windows/Fonts/arial.ttf", PdfEncodings.IDENTITY_H);
            var fontBold = PdfFontFactory.CreateFont("C:/Windows/Fonts/arialbd.ttf", PdfEncodings.IDENTITY_H);
            var fontItalic = PdfFontFactory.CreateFont("C:/Windows/Fonts/ariali.ttf", PdfEncodings.IDENTITY_H);
            var fontBoldItalic = PdfFontFactory.CreateFont("C:/Windows/Fonts/arialbi.ttf", PdfEncodings.IDENTITY_H);

            // ── Helper: paint a white rectangle ───────────────────────────────
            // x, topY = top-left corner measured from TOP of page
            void WhiteOut(float x, float topY, float width, float height)
            {
                canvas.SetFillColor(ColorConstants.WHITE)
                      .Rectangle(x, pageHeight - topY - height, width, height)
                      .Fill();
            }

            // ── Helper: write text at a position ──────────────────────────────
            // x, topY = baseline position measured from TOP of page
            void WriteText(string text, float x, float topY,
                           PdfFont font, float fontSize)
            {
                if (string.IsNullOrEmpty(text)) return;
                canvas.BeginText()
                      .SetFontAndSize(font, fontSize)
                      .SetColor(ColorConstants.BLACK, true)
                      .MoveText(x, pageHeight - topY)
                      .ShowText(text.Trim())
                      .EndText();
            }

            // ── Helper: draw a PNG logo ───────────────────────────────────────
            // x, topY = top-left corner measured from TOP of page
            void DrawImage(string imagePath, float x, float topY,
                           float width, float height)
            {
                if (!File.Exists(imagePath)) return;  // missing logo — skip silently

                var imageData = ImageDataFactory.Create(imagePath);
                var image = new iText.Layout.Element.Image(imageData);

                image.SetFixedPosition(x, pageHeight - topY - height);
                image.ScaleToFit(width, height);

                var document = new iText.Layout.Document(pdfDoc);
                document.Add(image);
            }

            // ══════════════════════════════════════════════════════════════════
            // SECTION A — LOGO  (top-right corner)
            // White out the original logo area and draw the province-specific logo.
            // ══════════════════════════════════════════════════════════════════
            WhiteOut(378, 20, 70, 60);
            DrawImage(logoPath, 378, 20, 70, 60);

            // ══════════════════════════════════════════════════════════════════
            // SECTION B — MY NO  (top-left)
            // Template prints "MyNo :". White out the value area to the right
            // of the colon and write MyBranch (e.g. "6").
            // ══════════════════════════════════════════════════════════════════
            WhiteOut(90, 30, 200, 16);
            WriteText(d.MyBranch, 90, 45, fontRegular, 9);

            // ══════════════════════════════════════════════════════════════════
            // SECTION C — NOTICE NUMBER  (same baseline as MyBranch, right-of-centre)
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.NoticeNo, 250, 45, fontBold, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION D — CUSTOMER NAME
            // Template prints "Mr/Mrs/Miss". Customer name written after the gap.
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.CustomerName, 103, 103, fontBold, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION E — CUSTOMER ADDRESS  (3 lines, ~14 pts apart)
            // ══════════════════════════════════════════════════════════════════
            WhiteOut(56, 108, 300, 50);  // erase all 3 address lines at once
            WriteText(d.Address1, 103, 120, fontBold, 10);

            if (!string.IsNullOrWhiteSpace(d.Address2))
                WriteText(d.Address2, 103, 134, fontBold, 10);   // x=56, consistent with Address1

            if (!string.IsNullOrWhiteSpace(d.Address3))
                WriteText(d.Address3, 103, 148, fontBold, 10);   // x=56, consistent

            // ══════════════════════════════════════════════════════════════════
            // SECTION F — DATE  (top-right, same row as address area)
            // ══════════════════════════════════════════════════════════════════
            WriteText(DateTime.Today.ToString("dd/MM/yyyy"), 377, 160, fontRegular, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION G — OFFICE ADDRESS BLOCK  (top-right, below logo)
            // White out the pre-printed office text and write 5 DB values.
            // ══════════════════════════════════════════════════════════════════
            //WhiteOut(375, 80, 220, 80);
            WriteText(d.OfficeDesc1, 375, 95, fontRegular, 10);
            WriteText(d.OfficeDesc2, 375, 107, fontRegular, 10);
            WriteText(d.OfficeDesc3, 375, 119, fontRegular, 10);
            WriteText(d.OfficeDesc4, 375, 131, fontRegular, 10);
            WriteText($"Tel. :{d.Tel}", 375, 143, fontRegular, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION H — ACCOUNT NUMBER
            // "Dishonoured cheque - Account No:" heading.
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.AccountNo, 372, 192, fontBold, 11);

            // ══════════════════════════════════════════════════════════════════
            // SECTION I — CHEQUE DETAILS LINE
            // "The cheque bearing no ___ dated ___ for Rs. ___ forwarded by you"
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.ChequeNo, 167, 234, fontBold, 10);
            WriteText(d.ChequeDate, 246, 235, fontBold, 10);
            WriteText($"{d.Amount:F2}", 333, 234, fontBold, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION J — CHARGES PARAGRAPH  (province-specific layout)
            // This is the only section that differs between province templates.
            // The correct drawer is selected by province code; everything else
            // above and below this block is identical across all templates.
            //
            // Coordinates measured via pdfplumber on the Colombo City template:
            //   Line 1 (Total):       blank x=387–465, baseline topY=272
            //   Line 2 (Postage):     blank x=179–257, baseline topY=280
            //   Line 2 (BankCharges): blank x=350–428, baseline topY=280
            //   Line 3 (Surcharge%):  blank x=278–311, baseline topY=291
            // ══════════════════════════════════════════════════════════════════
            var drawer = ProvinceDrawers.GetValueOrDefault(
                d.MyAddCode.Trim().ToUpper(), DefaultDrawer);

            drawer.DrawChargesParagraph(d, WriteText, fontRegular);

            // ══════════════════════════════════════════════════════════════════
            // SECTION K — AREA  (bottom of main body, above "Copy to" section)
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.Area, 58, 585, fontRegular, 9);

            // ══════════════════════════════════════════════════════════════════
            // SECTION L — AMOUNT BREAKDOWN TABLE  (bottom-right)
            // ══════════════════════════════════════════════════════════════════
            WriteText($"{d.Amount:F2}", 450, 678, fontItalic, 10);
            WriteText($"{d.Postage:F2}", 465, 691, fontItalic, 10);
            WriteText($"{d.BankCharges:F2}", 465, 706, fontItalic, 10);
            WriteText($"{d.Surcharge:F2}", 463, 720, fontItalic, 10);
            WriteText($"{d.Total:F2}", 450, 745, fontBold, 10);

            // ─────────────────────────────────────────────────────────────────
            canvas.Release();
            pdfDoc.Close();

            return outputStream.ToArray();
        }
    }
}