using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using Microsoft.AspNetCore.Hosting;
using Monthly_Collection_Details.Models;

namespace Monthly_Collection_Details.Services
{
    public class PdfService
    {
        private readonly string _templatePath;
        private readonly string _logoPath;       

        public PdfService(IWebHostEnvironment env)
        {
            _templatePath = Path.Combine(
                env.WebRootPath, "templates", "cheque_notice_template.pdf");

            // Path to the new EDL logo
            _logoPath = Path.Combine(
                        env.WebRootPath, "images", "EDLLOGO.png");
        }

        public byte[] GenerateNotice(Model1.ChequeNoticeDetail d)
        {
            using var outputStream = new MemoryStream();
            using var reader = new PdfReader(_templatePath);
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(reader, writer);

            var page = pdfDoc.GetFirstPage();
            var canvas = new PdfCanvas(page);
            var pageHeight = page.GetPageSize().GetHeight();

            // ── Fonts ─────────────────────────────────────────────────────────
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontItalic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
            var fontBoldItalic = PdfFontFactory.CreateFont(
                                     StandardFonts.HELVETICA_BOLDOBLIQUE);

            // ── Helper: white out rectangle ───────────────────────────────────
            void WhiteOut(float x, float topY, float width, float height)
            {
                canvas.SetFillColor(ColorConstants.WHITE)
                      .Rectangle(x, pageHeight - topY - height, width, height)
                      .Fill();
            }

            // ── Helper: write text at coordinate ─────────────────────────────
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

            // ── Helper: draw image at coordinate ─────────────────────────────
            // x, topY = top-left corner of the image from TOP of page
            // width, height = size of the image in points
            void DrawImage(string imagePath, float x, float topY,
                float width, float height)
            {
                if (!File.Exists(imagePath))
                    return;

                var imageData = ImageDataFactory.Create(imagePath);

                // Create image element
                var image = new iText.Layout.Element.Image(imageData);

                // Convert topY to bottomY
                float bottomY = pageHeight - topY - height;

                image.SetFixedPosition(x, bottomY);
                image.ScaleToFit(width, height);

                // Add to document
                var document = new iText.Layout.Document(pdfDoc);
                document.Add(image);
            }

            // LOGO — White out existing CEB logo, draw EDL logo in its place
            WhiteOut(378, 20, 70, 60);           // erase CEB logo
            DrawImage(_logoPath, 378, 20, 70, 60); // draw EDL logo        

            // 1. NOTICE NUMBER
            WriteText(d.NoticeNo, 250, 45, fontBold, 10);

            // 2. CUSTOMER NAME
            WriteText(d.CustomerName, 103, 103, fontBoldItalic, 10);

            // 3. ADDRESS LINES
            WriteText(d.Address1, 56, 120, fontBoldItalic, 10);

            if (!string.IsNullOrWhiteSpace(d.Address2))
                WriteText(d.Address2, 56, 132, fontBoldItalic, 10);

            if (!string.IsNullOrWhiteSpace(d.Address3))
                WriteText(d.Address3, 56, 144, fontBoldItalic, 10);

            // 4. DATE
            WriteText(
                DateTime.Today.ToString("dd /MM/yyyy"),
                378, 160, fontBoldItalic, 10);

            // 6. ACCOUNT NUMBER
            WriteText(d.AccountNo, 367, 203, fontBold, 11);

            // 7. CHEQUE NUMBER
            WriteText(d.ChequeNo, 146, 227, fontBold, 10);

            // 8. CHEQUE DATE
            WriteText(d.ChequeDate, 240, 227, fontBold, 10);

            // 9. AMOUNT INLINE
            WriteText($"{d.Amount:F2}", 363, 227, fontBold, 10);

            // 10. REMARK
            WriteText("", 59, 277, fontBold, 10);

            // 11. TOTAL INLINE
            WriteText($"{d.Total:F2}", 394, 270, fontBold, 10);

            // 12. AMOUNT BREAKDOWN TABLE

            // Amount
            WriteText($"{d.Amount:F2}", 490, 688, fontItalic, 10);

            // Postage
            WriteText($"{d.Postage:F2}", 490, 702, fontItalic, 10);

            // Bank Charges
            WriteText($"{d.BankCharges:F2}", 490, 716, fontItalic, 10);

            // Surcharge value
            WriteText($"{d.Surcharge:F2}", 490, 729, fontItalic, 10);

            // Total — bold
            WriteText($"{d.Total:F2}", 490, 745, fontBold, 10);

            canvas.Release();
            pdfDoc.Close();

            return outputStream.ToArray();
        }
    }
}