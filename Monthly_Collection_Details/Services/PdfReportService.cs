using Monthly_Collection_Details.Models;
using Monthly_Collection_Details.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Monthly_Collection_Details.Services
{
    public class PdfReportService
    {
        public byte[] GenerateCollectionReport(CollectionReportData data, string fromDate, string toDate)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);

                    page.Content().Column(col =>
                    {
                        BuildHeader(col, fromDate, toDate);

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                            });

                            BuildTableHeader(table);
                            BuildHQSection(table, data);
                            BuildCollectionCenterSection(table, data);
                            BuildCollectionAgentSection(table, data);
                            PIVSection(table, data);
                            BuildFooterSignatures(table);
                        });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(9).Italic().FontColor("#666666"));
                            text.Span("2026 Utility Solutions & Automation Branch,  EDL. All Rights Reserved");
                            text.Span("EDL (Pvt) Ltd");
                            text.Span($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        });

                });
            });

            return document.GeneratePdf();
        }

        private void BuildHeader(ColumnDescriptor col, string fromDate, string toDate)
        {
            col.Item().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(13).LineHeight(0.5f));
                text.Span("Monthly Collection Details Statement ").SemiBold();
                text.Line($"{fromDate} - {toDate}").Bold();
            });

            col.Item().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(13).LineHeight(0.7f));
                text.Line("EDL (Pvt) Ltd Headquarters").SemiBold();
            });

            //col.Item()
            //    .PaddingVertical(8)
            //    .LineHorizontal(1)
            //    .LineColor("#5D4037"); // Brown divider
        }

        private void BuildTableHeader(TableDescriptor table)
        {
            table.Header(header =>
            {
                header.Cell().Element(ReportStyles.HeaderStyle).Text("Description");
                header.Cell().Element(ReportStyles.HeaderStyle).Text("Pay mode");
                header.Cell().Element(ReportStyles.HeaderStyle).Text("No of payments");
                header.Cell().Element(ReportStyles.HeaderStyle).Text("Amount (LKR)");
                header.Cell().Element(ReportStyles.HeaderStyle).Text("Collection counter");
            });
        }

        private void BuildHQSection(TableDescriptor table, CollectionReportData data)
        {
            AddEmptyRow(table, "Paid direct to CEB-HQ POS Counters");
            AddSummaryRow(table, data.Cash, "1-8 (Excluding Credit Card - 8a)", ReportStyles.YellowCell, "Cash");
            AddSummaryRow(table, data.ChequeDraft, "(Counter 9 - Counter 12)", ReportStyles.CyanCell, "Cheques and Drafts");
            AddSummaryRow(table, data.CreditHQ, "Credit Card", ReportStyles.GreenCyanCell, "Credit Card");
            AddSummaryRow(table, data.BankTransfer, "(Counter 12,5,10)", ReportStyles.CyanCell, "Bank Transfer");
            AddSummaryRow(table, data.Kiosk, "(0M)", ReportStyles.CyanCell, "Kiosk");
        }

        private void BuildCollectionCenterSection(TableDescriptor table, CollectionReportData data)
        {
            AddEmptyRow(table, "Collection Centers - POS Counters");
            AddSummaryRowWithDescription(table, "(1). Mudalige Mawatha", data.MudaligeMawathaCash, "OA,OB", ReportStyles.YellowCell, "Cash");
            AddSummaryRow(table, data.MudaligeMawathaChecksAndDrafts, "OC", ReportStyles.CyanCell, "Cheques and Draft");
            AddSummaryRowWithDescription(table, "(2). Bambalapitiya", data.BambalapitiyaCash, "OD,OE", ReportStyles.YellowCell, "Cash");
            AddSummaryRow(table, data.BambalapitiyaChecksAndDrafts, "OC", ReportStyles.CyanCell, "Cheques and Draft");
            AddSummaryRowWithDescription(table, "(3). Malwatta Road", data.MalawattaRoadcash, "OG,OH", ReportStyles.YellowCell, "Cash");
            AddSummaryRow(table, data.MalawattaRoadChecksAndDrafts, "OJ", ReportStyles.CyanCell, "Cheques and Draft");
            AddSummaryRowWithDescription(table, (Action<TextDescriptor>)(t => t.Span("Sub Total").Bold()), data.SubTotal, "", ReportStyles.NormalCell, "");
        }

        private void BuildCollectionAgentSection(TableDescriptor table, CollectionReportData data)
        {
            AddEmptyRow(table, "Through Collection Agents");

            var agents = new[]
            {
                ("1. Peoples Bank", data.PeoplesBank, "Counter 55"),
                ("1.a Peoples Bank Internet Payments Bill", data.PeoplesBankInternetBill, "Counter 55i Bill"),
                ("1.b Peoples Bank Internet Payments PIV", data.PeoplesBankInternetPIV, "Refere-PIV"),
                ("2. Bank Of Ceylon", data.BankOfCeylon, "Counter 30"),
                ("3. National Savings Bank", data.NationalSavingsBank, "Counter 56"),
                ("4. Hatton National Bank", data.HattonNationalBank, "Counter 35"),
                ("6. Commercial Bank", data.CommercialBank, "Counter 37"),
                ("7. HSBC", data.HSBC, "Counter 39"),
                ("8. National Development Bank", data.NationalDevelopmentBank, "Counter 81"),
                ("9.a. National Trust Bank", data.NationalTrustBank, "Counter 80"),
                ("9.b. Amex Internet Payments NTB Bill", data.AmexInternetPaymentsNTBBill, "Counter 80i Bill"),
                ("9.b. Amex Internet Payments NTB PIV", data.AmexInternetPaymentsNTBPiv, "Refere-PIV"),
                ("10. Sampath Bank", data.SampathBank, "Counter 29"),
                ("11. Seylan Bank", data.SeylanBank, "Counter 28, 36"),
                ("12. Union Bank", data.UnionBank, "Counter 33"),
                ("13. HDFC", data.HdfcBank, "Counter 20"),
                ("14. DFCC Bank", data.DfccBank, "Counter 24"),
                ("15. ABANS", data.Abans, "Counter 42"),
                ("16. SINGER", data.Singer, "Counter 41"),
                ("17. Cargils Food City", data.Cargills, "Counter 31"),
                ("18. PanAsia Bank", data.PanAsiaBank, "Counter 26"),
                ("19. Arpico Supermarket", data.Arpico, "Counter 27"),
                ("20. CEB Internet Payments HNB Bill", data.CEBInternetPaymentsHNBBill, "Counter 35i"),
                //("20.a CEB Internet Payments HNB PIV", data.CEBInternetPaymentsHNBPiv, "Counter 35i-Piv"),
                ("21. Mobitel", data.Mobitel, "Counter 23"),
                ("22. Dialog", data.Dialog, "Counter 22"),
                ("23. Laugh Supermarket", data.LaughSupermarket, "Counter 22"),
            };

            foreach (var (name, summary, counter) in agents)
                AddSummaryRowWithDescription(table, name, summary, counter, ReportStyles.CyanCell, "");

            AddSummaryRowWithDescription(table, "24. Postal Department", data.PostalDepartment, "Counter 40", ReportStyles.YellowCyanCell, "");
            AddSummaryRowWithDescription(table, "25. Regional Development Bank(RDB)", data.RegionalDevelopmentBankRDB, "Counter 32", ReportStyles.CyanCell, "");
            AddSummaryRowWithDescription(table, (Action<TextDescriptor>)(t => t.Span("Sub Total").Bold()), data.AgentSubTotal, "", ReportStyles.NormalCell, "");
            AddSummaryRowWithDescription(table, "Total Collection on Sales",data.TotalCollectionOnSales,"(a)",ReportStyles.NormalCell, "");
            AddEmptyRow(table, "PIV");
        }

        private void PIVSection(TableDescriptor table, CollectionReportData data)
        {
            AddSummaryRowWithDescription(table, "Cash", data.CashPayments, "Cash", ReportStyles.YellowCyanCell, "");
            AddSummaryRowWithDescription(table, "Cheques and Drafts", data.ChequeDraftPayments, "Cheques and Draft", ReportStyles.YellowCyanCell, "");
            AddSummaryRowWithDescription(table, "Credit Card HQ", data.CreditCardHQ, "Credit Card", ReportStyles.YellowCyanCell, "");
            AddSummaryRowWithDescription(table, "Direct Transfer HQPIV", data.DirectTransferHQPIV, "Direct Transfer", ReportStyles.YellowCyanCell, "");
            //AddSummaryRowWithDescription(table, "Total S.M. & Others", data.TotalSMOthers, "(c)", ReportStyles.YellowCyanCell, "");
            AddSummaryRowWithDescription(table, (Action<TextDescriptor>)(t => t.Span("Total S.M. & Others").Bold()), data.TotalSMOthers, "(c)", ReportStyles.YellowCyanCell, "");
            AddSummaryRowWithDescription(table,(Action<TextDescriptor>)(t => t.Span("Grand Total").Bold()),data.GrandTotal, "(a)+(b)+(c)",ReportStyles.TotalBoldCell, "");
        }

        private void AddSummaryRow(TableDescriptor table, CollectionSummary summary, string counter, Func<IContainer, IContainer> style, string mode)
        {
            table.Cell().Element(ReportStyles.NormalCell).Text("");
            table.Cell().Element(style).Text(mode);
            table.Cell().Element(style).AlignRight().Text(summary.count_no.ToString("N0"));
            table.Cell().Element(style).AlignRight().Text(summary.trans_amt.ToString("N2"));
            table.Cell().Element(style).Text(counter).AlignCenter();
        }

        private void AddSummaryRowWithDescription(TableDescriptor table, object description, CollectionSummary summary, string counter, Func<IContainer, IContainer> style, string mode)
        {
            var descCell = table.Cell().Element(ReportStyles.NormalCell);
            if (description is string s) descCell.Text(s);
            else if (description is Action<TextDescriptor> action) descCell.AlignRight().Text(action);

            table.Cell().Element(style).Text(mode);
            table.Cell().Element(style).AlignRight().Text(summary.count_no.ToString("N0"));
            table.Cell().Element(style).AlignRight().Text(summary.trans_amt.ToString("N2"));
            table.Cell().Element(style).Text(counter).AlignCenter();
        }

        private void AddEmptyRow(TableDescriptor table, string title)
        {
            table.Cell().Element(ReportStyles.NormalCell).Text(title).Bold();
            for (int i = 0; i < 4; i++)
                table.Cell().Element(ReportStyles.NormalCell).Text("");
        }

        private void BuildFooterSignatures(TableDescriptor table)
        {
            table.Cell().Element(ReportStyles.NormalCell).Height(10).Text("");
            table.Cell().Element(ReportStyles.NormalCell).Height(10).Text("");
            table.Cell().Element(ReportStyles.NormalCell).Height(10).Text("");
            table.Cell().ColumnSpan(2).Element(ReportStyles.NormalCell).Height(10).Text("");

            table.Cell().Element(ReportStyles.NormalCell).Text("Date :");
            table.Cell().Element(ReportStyles.NormalCell).Text("Prepared :");
            table.Cell().Element(ReportStyles.NormalCell).Text("Check by :");
            table.Cell().ColumnSpan(2).Element(ReportStyles.NormalCell).Text("Accountant (Cash) :");
        }

    }
}