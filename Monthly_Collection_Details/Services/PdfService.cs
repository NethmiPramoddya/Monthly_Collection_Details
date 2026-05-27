using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.AspNetCore.Hosting;
using Monthly_Collection_Details.Models;

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
    // Template paragraph (gaps shown as ___):
    //   "...pay immediately a sum of Rs. _TOTAL_ being
    //    the value of dishonoured cheque, Rs. _POSTAGE_/- for postage,
    //    Rs. _BANKCHARGES_/- for dishonoured cheque charges charged
    //    by the bank and surcharge of _PERCENTAGE_% on the value of the cheque..."
    // ══════════════════════════════════════════════════════════════════════════
    public class ColomboCityDrawer : IProvinceDrawer
    {
        public void DrawChargesParagraph(
            Model1.ChequeNoticeDetail d,
            Action<string, float, float, PdfFont, float> writeText,
            PdfFont fontRegular)
        {
            // Line 1 — "...pay immediately a sum of Rs. ___ being"
            writeText($"{d.Total:F2}", 394, 270, fontRegular, 9);

            // Line 2 — "...Rs. ___ /- for postage, Rs. ___ /- for dishonoured cheque charges..."
            writeText($"{d.Postage:F0}", 205, 285, fontRegular, 9);
            writeText($"{d.BankCharges:F0}", 310, 285, fontRegular, 9);

            // Line 3 — "...surcharge of ___% on the value..."
            writeText($"{d.Percentage:F0}", 173, 298, fontRegular, 9);
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
    //   - Wider gaps for postage and bank charges values
    //   - Line 3 writes surcharge amount (Rs.X) instead of percentage
    // ══════════════════════════════════════════════════════════════════════════
    public class NwpDrawer : IProvinceDrawer
    {
        public void DrawChargesParagraph(
            Model1.ChequeNoticeDetail d,
            Action<string, float, float, PdfFont, float> writeText,
            PdfFont fontRegular)
        {
            // Line 1 — "...pay immediately a sum of Rs. ___ being"
            writeText($"{d.Total:F2}", 394, 270, fontRegular, 9);

            // Line 2 — "...Rs ___ for postage, Rs. ___ for dishonoured cheque charges..."
            writeText($"{d.Postage:F0}", 205, 285, fontRegular, 9);
            writeText($"{d.BankCharges:F0}", 344, 285, fontRegular, 9);

            // Line 3 — "...surcharge of Rs.___ on the value..."
            writeText($"Rs.{d.Surcharge:F0}", 214, 299, fontRegular, 9);
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
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontItalic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
            var fontBoldItalic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLDOBLIQUE);

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
            WriteText(d.CustomerName, 103, 103, fontBoldItalic, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION E — CUSTOMER ADDRESS  (3 lines, ~14 pts apart)
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.Address1, 56, 120, fontBoldItalic, 10);

            if (!string.IsNullOrWhiteSpace(d.Address2))
                WriteText(d.Address2, 56, 134, fontBoldItalic, 10);

            if (!string.IsNullOrWhiteSpace(d.Address3))
                WriteText(d.Address3, 56, 148, fontBoldItalic, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION F — DATE  (top-right, same row as address area)
            // ══════════════════════════════════════════════════════════════════
            WriteText(DateTime.Today.ToString("dd/MM/yyyy"), 378, 160, fontBoldItalic, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION G — OFFICE ADDRESS BLOCK  (top-right, below logo)
            // White out the pre-printed office text and write 5 DB values.
            // ══════════════════════════════════════════════════════════════════
            //WhiteOut(375, 80, 220, 80);
            WriteText(d.OfficeDesc1, 375, 95, fontRegular, 9);
            WriteText(d.OfficeDesc2, 375, 107, fontRegular, 9);
            WriteText(d.OfficeDesc3, 375, 119, fontRegular, 9);
            WriteText(d.OfficeDesc4, 375, 131, fontRegular, 9);
            WriteText($"Tel. :{d.Tel}", 375, 143, fontRegular, 9);

            // ══════════════════════════════════════════════════════════════════
            // SECTION H — ACCOUNT NUMBER
            // "Dishonoured cheque - Account No:" heading.
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.AccountNo, 367, 200, fontBold, 11);

            // ══════════════════════════════════════════════════════════════════
            // SECTION I — CHEQUE DETAILS LINE
            // "The cheque bearing no ___ dated ___ for Rs. ___ forwarded by you"
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.ChequeNo, 146, 227, fontBold, 10);
            WriteText(d.ChequeDate, 240, 227, fontBold, 10);
            WriteText($"{d.Amount:F2}", 365, 228, fontBold, 10);

            // ══════════════════════════════════════════════════════════════════
            // SECTION J — CHARGES PARAGRAPH  (province-specific layout)
            // This is the only section that differs between province templates.
            // The correct drawer is selected by province code; everything else
            // above and below this block is identical across all templates.
            // ══════════════════════════════════════════════════════════════════
            var drawer = ProvinceDrawers.GetValueOrDefault(
                d.MyAddCode.Trim().ToUpper(), DefaultDrawer);

            drawer.DrawChargesParagraph(d, WriteText, fontRegular);

            // ══════════════════════════════════════════════════════════════════
            // SECTION K — AREA  (bottom of main body, above "Copy to" section)
            // ══════════════════════════════════════════════════════════════════
            WriteText(d.Area, 105, 585, fontBold, 9);

            // ══════════════════════════════════════════════════════════════════
            // SECTION L — AMOUNT BREAKDOWN TABLE  (bottom-right)
            // ══════════════════════════════════════════════════════════════════
            WriteText($"{d.Amount:F2}", 490, 688, fontItalic, 10);
            WriteText($"{d.Postage:F2}", 490, 702, fontItalic, 10);
            WriteText($"{d.BankCharges:F2}", 490, 716, fontItalic, 10);
            WriteText($"{d.Surcharge:F2}", 490, 729, fontItalic, 10);
            WriteText($"{d.Total:F2}", 490, 745, fontBold, 10);

            // ─────────────────────────────────────────────────────────────────
            canvas.Release();
            pdfDoc.Close();

            return outputStream.ToArray();
        }
    }
}