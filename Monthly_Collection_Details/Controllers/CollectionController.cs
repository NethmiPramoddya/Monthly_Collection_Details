using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Services;
using Monthly_Collection_Details.Models;
using Microsoft.AspNetCore.Authorization;

namespace Monthly_Collection_Details.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionController : ControllerBase
    {
        private readonly DatabaseService _db;
        private readonly PdfReportService _pdfService;

        public CollectionController(DatabaseService db, PdfReportService pdfService)
        {
            _db = db;
            _pdfService = pdfService;
        }

        [HttpGet("pdf")]
        public async Task<IActionResult> DownloadPdf(string fromDate, string toDate)
        {
            if (!DateTime.TryParseExact(fromDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var from))
                return BadRequest("Invalid From Date format. Use dd/MM/yyyy");

            if (!DateTime.TryParseExact(toDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var to))
                return BadRequest("Invalid To Date format. Use dd/MM/yyyy");

            if (from > to)
                return BadRequest("From Date cannot be greater than To Date.");

            if (from > DateTime.Today || to > DateTime.Today)
                return BadRequest("Dates cannot be in the future.");

            to = to.Date;

            var data = await LoadReportDataAsync(from, to);
            var pdfBytes = _pdfService.GenerateCollectionReport(data, fromDate, toDate);

            return File(pdfBytes, "application/pdf", $"Monthly_Collection_{fromDate}_to_{toDate}.pdf");
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(string fromDate, string toDate)
        {
            if (!DateTime.TryParse(fromDate, out var from))
                return BadRequest("Invalid From Date format. Use yyyy-MM-dd");

            if (!DateTime.TryParse(toDate, out var to))
                return BadRequest("Invalid To Date format. Use yyyy-MM-dd");

            if (from > to)
                return BadRequest("From Date cannot be greater than To Date. Please correct the dates.");

            if (from > DateTime.Today || to > DateTime.Today)
                return BadRequest("Dates cannot be in the future.");
            to = to.Date;

            var data = await LoadReportDataAsync(from, to);
            return Ok(data);
        }

        private async Task<CollectionReportData> LoadReportDataAsync(DateTime from, DateTime to)
        {
            // Execute ALL base queries in parallel (NO totals yet)
            var taskCash = Task.Run(() => _db.GetCollectionSummary(from, to));
            var taskChequeDraft = Task.Run(() => _db.GetChequeDraftSummary(from, to));
            var taskBankTransfer = Task.Run(() => _db.GetBankTransfer(from, to));
            var taskKiosk = Task.Run(() => _db.GetKioks(from, to));
            var taskCreditHQ = Task.Run(() => _db.GetCredit(from, to));
            var taskMudaligeCash = Task.Run(() => _db.GetMudaligeMawathaCash(from, to));
            var taskMudaligeChecks = Task.Run(() => _db.GetMudaligeMawathaChecksAndDrafts(from, to));
            var taskBambalapitiyaCash = Task.Run(() => _db.GetBambalapitiyaCash(from, to));
            var taskBambalapitiyaChecks = Task.Run(() => _db.GetBambalapitiyaChecksAndDrafts(from, to));
            var taskMalawattaCash = Task.Run(() => _db.GetMalawattaRoadcash(from, to));
            var taskMalawattaChecks = Task.Run(() => _db.GetMalawattaRoadChecksAndDrafts(from, to));
            var taskSubTotal = Task.Run(() => _db.GetSubTotal(from, to));

            // Agent Data
            var taskPeoplesBank = Task.Run(() => _db.GetPeoplesBank(from, to));
            var taskPBBill = Task.Run(() => _db.GetPeoplesBankInternetPaymentsBill(from, to));
            var taskPBPIV = Task.Run(() => _db.GetPeoplesBankInternetPaymentsPIV(from, to));
            var taskBOC = Task.Run(() => _db.GetBankOfCeylon(from, to));
            var taskNSB = Task.Run(() => _db.GetNationalSavingsBank(from, to));
            var taskHNB = Task.Run(() => _db.GetHattonNationalBank(from, to));
            var taskComBank = Task.Run(() => _db.GetCommercialBank(from, to));
            var taskHSBC = Task.Run(() => _db.GetHSBC(from, to));
            var taskNTB = Task.Run(() => _db.GetNationalTrustBank(from, to));
            var taskAmexBill = Task.Run(() => _db.GetAmexInternetPaymentsNTBBill(from, to));
            var taskAmexPIV = Task.Run(() => _db.GetAmexInternetPaymentsNTBPIV(from, to));
            var taskNDB = Task.Run(() => _db.GetNationalDevelopmentBank(from, to));
            var taskSampath = Task.Run(() => _db.GetSampathBank(from, to));
            var taskSeylan = Task.Run(() => _db.GetSeylanBank(from, to));
            var taskUnion = Task.Run(() => _db.GetUnionBank(from, to));
            var taskHdfc = Task.Run(() => _db.GetHdfcBank(from, to));
            var taskDfcc = Task.Run(() => _db.GetDfccBank(from, to));
            var taskAbans = Task.Run(() => _db.GetAbans(from, to));
            var taskSinger = Task.Run(() => _db.GetSinger(from, to));
            var taskCargills = Task.Run(() => _db.GetCargills(from, to));
            var taskPanAsia = Task.Run(() => _db.GetPanAsiaBank(from, to));
            var taskArpico = Task.Run(() => _db.GetArpico(from, to));
            var taskCEBBill = Task.Run(() => _db.GetCEBInternetPaymentsHNBBill(from, to));
            var taskCEBPIV = Task.Run(() => _db.GetCEBInternetPaymentsHNBPiv(from, to));
            var taskMobitel = Task.Run(() => _db.GetMobitel(from, to));
            var taskDialog = Task.Run(() => _db.GetDialog(from, to));
            var taskLaugh = Task.Run(() => _db.GetLaughSupermarket(from, to));
            var taskPostal = Task.Run(() => _db.GetPostalDepartment(from, to));
            var taskRDB = Task.Run(() => _db.GetRegionalDevelopmentBankRDB(from, to));
            var taskAgentSubTotal = Task.Run(() => _db.GetAgentSubTotal(from, to));

            // PIV
            var taskCashPayments = Task.Run(() => _db.GetCashPayments(from, to));
            var taskChequeDraftPayments = Task.Run(() => _db.GetChequeDraftPayments(from, to));
            var taskCreditCardHQ = Task.Run(() => _db.GetCreditCardHQ(from, to));
            var taskDirectTransfer = Task.Run(() => _db.GetDirectTransferHQPIV(from, to));
            var taskTotalSM = Task.Run(() => _db.GetTotalSMOthers(from, to));

            // Wait for ALL to complete
            await Task.WhenAll(
                taskCash, taskChequeDraft, taskBankTransfer, taskKiosk, taskCreditHQ,
                taskMudaligeCash, taskMudaligeChecks, taskBambalapitiyaCash, taskBambalapitiyaChecks,
                taskMalawattaCash, taskMalawattaChecks, taskSubTotal,
                taskPeoplesBank, taskPBBill, taskPBPIV, taskBOC, taskNSB, taskHNB,
                taskComBank, taskHSBC, taskNTB, taskAmexBill, taskAmexPIV, taskNDB,
                taskSampath, taskSeylan, taskUnion, taskHdfc, taskDfcc, taskAbans,
                taskSinger, taskCargills, taskPanAsia, taskArpico, taskCEBBill, taskCEBPIV,
                taskMobitel, taskDialog, taskLaugh, taskPostal, taskRDB, taskAgentSubTotal,
                taskCashPayments, taskChequeDraftPayments, taskCreditCardHQ, taskDirectTransfer,
                taskTotalSM
            );

            // Now fetch results
            var subTotal = await taskSubTotal;
            var pbBill = await taskPBBill;
            var pbPIV = await taskPBPIV;
            var cebBill = await taskCEBBill;
            var cebPIV = await taskCEBPIV;
            var amexBill = await taskAmexBill;
            var amexPIV = await taskAmexPIV;
            var agentSubTotal = await taskAgentSubTotal;
            var totalSM = await taskTotalSM;

            // Calculate totals IN MEMORY (no DB calls)
            var fullAgentSubTotal = new CollectionSummary
            {
                count_no = pbBill.count_no + pbPIV.count_no + cebBill.count_no +
                           cebPIV.count_no + amexBill.count_no + amexPIV.count_no +
                           agentSubTotal.count_no,
                trans_amt = pbBill.trans_amt + pbPIV.trans_amt + cebBill.trans_amt +
                            cebPIV.trans_amt + amexBill.trans_amt + amexPIV.trans_amt +
                            agentSubTotal.trans_amt
            };

            var totalCollectionOnSales = new CollectionSummary
            {
                count_no = subTotal.count_no + fullAgentSubTotal.count_no,
                trans_amt = subTotal.trans_amt + fullAgentSubTotal.trans_amt
            };

            var grandTotal = new CollectionSummary
            {
                count_no = subTotal.count_no + totalSM.count_no + fullAgentSubTotal.count_no,
                trans_amt = subTotal.trans_amt + totalSM.trans_amt + fullAgentSubTotal.trans_amt
            };

            return new CollectionReportData
            {
                Cash = await taskCash,
                ChequeDraft = await taskChequeDraft,
                BankTransfer = await taskBankTransfer,
                Kiosk = await taskKiosk,
                CreditHQ = await taskCreditHQ,
                MudaligeMawathaCash = await taskMudaligeCash,
                MudaligeMawathaChecksAndDrafts = await taskMudaligeChecks,
                BambalapitiyaCash = await taskBambalapitiyaCash,
                BambalapitiyaChecksAndDrafts = await taskBambalapitiyaChecks,
                MalawattaRoadcash = await taskMalawattaCash,
                MalawattaRoadChecksAndDrafts = await taskMalawattaChecks,
                SubTotal = subTotal,

                PeoplesBank = await taskPeoplesBank,
                PeoplesBankInternetBill = pbBill,
                PeoplesBankInternetPIV = pbPIV,
                BankOfCeylon = await taskBOC,
                NationalSavingsBank = await taskNSB,
                HattonNationalBank = await taskHNB,
                CommercialBank = await taskComBank,
                HSBC = await taskHSBC,
                NationalTrustBank = await taskNTB,
                AmexInternetPaymentsNTBBill = amexBill,
                AmexInternetPaymentsNTBPiv = amexPIV,
                NationalDevelopmentBank = await taskNDB,
                SampathBank = await taskSampath,
                SeylanBank = await taskSeylan,
                UnionBank = await taskUnion,
                HdfcBank = await taskHdfc,
                DfccBank = await taskDfcc,
                Abans = await taskAbans,
                Singer = await taskSinger,
                Cargills = await taskCargills,
                PanAsiaBank = await taskPanAsia,
                Arpico = await taskArpico,
                CEBInternetPaymentsHNBBill = cebBill,
                CEBInternetPaymentsHNBPiv = cebPIV,
                Mobitel = await taskMobitel,
                Dialog = await taskDialog,
                LaughSupermarket = await taskLaugh,
                PostalDepartment = await taskPostal,
                RegionalDevelopmentBankRDB = await taskRDB,
                AgentSubTotal = fullAgentSubTotal,

                CashPayments = await taskCashPayments,
                ChequeDraftPayments = await taskChequeDraftPayments,
                CreditCardHQ = await taskCreditCardHQ,
                DirectTransferHQPIV = await taskDirectTransfer,
                TotalSMOthers = totalSM,
                TotalCollectionOnSales = totalCollectionOnSales,
                GrandTotal = grandTotal
            };
        }
    }
}