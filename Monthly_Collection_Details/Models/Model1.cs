using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Monthly_Collection_Details.Models
{
    public class Model1
    {
        // ──────────────────────────────────────────────────────────────────
        // cheqmy_no table — used for the province dropdown on the login page
        // ──────────────────────────────────────────────────────────────────
        public class CheqMyNoRecord
        {
            public string MyBranch { get; set; }   // my_branch
            public string MyAddCode { get; set; }   // myadd_code
            public string OpenTime { get; set; }   // opentime
            public string MyCodeDesc { get; set; }   // mycode_desc
        }

        // ──────────────────────────────────────────────────────────────────
        // Request model — sent by the frontend after login
        // myAddCode comes from login response, fromDate entered by user,
        // toDate is always overridden to today server-side
        // ──────────────────────────────────────────────────────────────────
        public class ChequeReportRequest
        {
            [Required(ErrorMessage = "Province code is required.")]
            [StringLength(3, MinimumLength = 1)]
            public string MyAddCode { get; set; }   // locked to login province

            [Required(ErrorMessage = "From date is required.")]
            public DateOnly FromDate { get; set; }  // user enters this

            public DateOnly ToDate { get; set; }    // always overridden to today
        }

        // ──────────────────────────────────────────────────────────────────
        // Response model — one object per row returned from cheqmy_details
        // ──────────────────────────────────────────────────────────────────
        public class ChequeReportRecord
        {
            public string Branch { get; set; }   // my_branch
            public string NoticeNo { get; set; }   // my_code
            public string Account { get; set; }   // acct_number
            public string ChequeNo { get; set; }   // cheq_no
            public string ChequeDate { get; set; }   // cheq_date
            public int Months { get; set; }   // no_months
            public decimal Percentage { get; set; }   // percentage
            public DateOnly EntryDate { get; set; }   // entry_date
            public decimal Postage { get; set; }   // postage
            public decimal Surcharge { get; set; }   // surcharge
            public decimal BankCharges { get; set; }   // bank_charges
            public string Remark { get; set; }   // remark
            public decimal Amount { get; set; }   // amount
            public string Name { get; set; }   // cust_fname + cust_lname
            public string Address { get; set; }   // address_1 + address_2 + address_3
            public string Area { get; set; }   // area_name
        }

        // Used by the PDF generator — one full record for a single notice
        public class ChequeNoticeDetail
        {
            public string NoticeNo { get; set; }   // my_code  e.g. 6/2009/04/05
            public string AccountNo { get; set; }   // acct_number
            public string ChequeNo { get; set; }   // cheq_no
            public string ChequeDate { get; set; }   // cheq_date
            public string EntryDate { get; set; }   // entry_date formatted
            public string CustomerName { get; set; }   // cust_fname + cust_lname
            public string Address1 { get; set; }   // address_1
            public string Address2 { get; set; }   // address_2
            public string Address3 { get; set; }   // address_3
            public string Area { get; set; }   // area_name
            public decimal Amount { get; set; }   // amount
            public decimal Postage { get; set; }   // postage
            public decimal BankCharges { get; set; }   // bank_charges
            public decimal Surcharge { get; set; }   // surcharge
            public decimal Percentage { get; set; }   // percentage
            public decimal Total { get; set; }   // amount + postage + bankcharges + surcharge
            public string Remark { get; set; }   // remark
            public string MyAddCode { get; set; }   // myadd_code
            public string Tel { get; set; }   // myadd_tel from cheqmy_address
            public string OfficeDesc1 { get; set; }
            public string OfficeDesc2 { get; set; }
            public string OfficeDesc3 { get; set; }   // myadd_desc3 — street address
            public string OfficeDesc4 { get; set; }   // myadd_desc4 — city/postal
            public string MyBranch { get; set; }   // my_branch from cheqmy_details
        }

        // ──────────────────────────────────────────────────────────────────
        // Request: Search by Account No
        // ──────────────────────────────────────────────────────────────────
        public class SearchByAccountRequest
        {
            [Required]
            public string AccountNo { get; set; } = string.Empty;

            [Required]
            public string ReceivedDate { get; set; } = string.Empty;   // dd/MM/yyyy

            [Required]
            public int BillCycle { get; set; }
        }

        // ──────────────────────────────────────────────────────────────────
        // Request: Search by Cheque No
        // ──────────────────────────────────────────────────────────────────
        public class SearchByChequeRequest
        {
            [Required]
            public string ChequeNo { get; set; } = string.Empty;

            [Required]
            public string BankCode { get; set; } = string.Empty;

            [Required]
            public string BranchCode { get; set; } = string.Empty;

            [Required]
            public int BillCycle { get; set; }
        }

        // ──────────────────────────────────────────────────────────────────
        // Response: New Defaulter search result
        // ──────────────────────────────────────────────────────────────────
        public class NewDefaulterResult
        {
            public string CustomerName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Area { get; set; } = string.Empty;
            public string AreaCode { get; set; } = string.Empty;
            public string ChequeNo { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string AccountNo { get; set; } = string.Empty;
            public string Branch { get; set; } = string.Empty;
            public string BankCode { get; set; } = string.Empty;
        }

        // ──────────────────────────────────────────────────────────────────
        // Bill Cycle dropdown item
        // ──────────────────────────────────────────────────────────────────
        public class BillCycleRecord
        {
            public int BillCycle { get; set; }
            public string DisplayName { get; set; } = string.Empty;
        }

        // Internal helper — never exposed via API
        public class CustomerInfo
        {
            public string CustomerName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Area { get; set; } = string.Empty;
            public string AreaCode { get; set; } = string.Empty;
        }

        // ──────────────────────────────────────────────────────────────────
        // Raw DTO — matches exact field names from the external bill cycle API
        // http://10.128.1.227:5010/api/BulkEmail/billinfo
        // Never exposed via our own API — mapped to BillCycleRecord instead
        // ──────────────────────────────────────────────────────────────────
        public class BillCycleApiDto
        {
            public string bill_cycle { get; set; } = string.Empty;  // e.g. "105"
            public string bill_mnth { get; set; } = string.Empty;  // e.g. "1997 May"
        }

        // ──────────────────────────────────────────────────────────────────
        // 90-day Cheque Transaction Search models
        // ──────────────────────────────────────────────────────────────────

        // Raw row fetched from chq_mnyord for 90-day window
        public class ChequeTransactionRow
        {
            public string AccountNo { get; set; } = string.Empty;
            public string ChequeNo { get; set; } = string.Empty;
            public string BankCode { get; set; } = string.Empty;
            public string BranchCode { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string ChequeDate { get; set; } = string.Empty;  // formatted dd/MM/yyyy
            public string ReturnReason { get; set; } = string.Empty;
        }

        // One item in the results dropdown
        public class ChequeDropdownItem
        {
            public string Label { get; set; } = string.Empty;   // text shown in dropdown
            public string GroupKey { get; set; } = string.Empty;   // ChequeNo (account mode) or AccountNo (cheque mode)
            public List<ChequeTransactionRow> Transactions { get; set; } = new();
        }

        // Customer info enriched from prn_dat_1 — used inside the detail card
        public class ChequeCustomerInfo
        {
            public string AccountNo { get; set; } = string.Empty;
            public string CustomerName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Area { get; set; } = string.Empty;
        }

        // One result card — shown when user selects a dropdown item
        public class ChequeDetailCard
        {
            public string GroupKey { get; set; } = string.Empty;
            public string Branch { get; set; } = string.Empty;
            public ChequeCustomerInfo? Customer { get; set; }
            public List<ChequeTransactionRow> Transactions { get; set; } = new();
        }

        // Full API response for both search modes
        public class ChequeSearchResponse
        {
            public string Mode { get; set; } = string.Empty;   // "account" | "cheque"
            public List<ChequeDropdownItem> DropdownItems { get; set; } = new();
            public List<ChequeDetailCard> Cards { get; set; } = new();
        }

        //insertion
        // ──────────────────────────────────────────────────────────────────
        // ▼▼▼ NEW: Remark dropdown item — from cheqmy_remarks table ▼▼▼
        // ──────────────────────────────────────────────────────────────────
        public class RemarkRecord
        {
            public string RemarkCode { get; set; } = string.Empty;  // cheqremark_code
            public string RemarkText { get; set; } = string.Empty;  // remark
        }

        // ──────────────────────────────────────────────────────────────────
        // ▼▼▼ NEW: Charges config — from cheqmy_chargers table ▼▼▼
        // Postage, bank charges, surcharge %, and minimum months are
        // stored centrally so every branch uses the same values.
        // ──────────────────────────────────────────────────────────────────
        public class ChargesConfig
        {
            public decimal Postage { get; set; }
            public decimal BankCharges { get; set; }
            public decimal Percentage { get; set; }  // surcharge %
            public int NoMonths { get; set; }  // min months to block
        }

        // ──────────────────────────────────────────────────────────────────
        // ▼▼▼ NEW: Insert request — saves a new defaulter to cheqmy_details ▼▼▼
        // Frontend sends this after the user fills the "Save Cheque Details" form.
        // myadd_code comes from the authenticated user's session (login response).
        // ──────────────────────────────────────────────────────────────────
        public class SaveChequeDetailsRequest
        {
            [Required] public string MyAddCode { get; set; } = string.Empty; // from login
            [Required] public string MyCode { get; set; } = string.Empty; // user-entered notice no
            [JsonIgnore] public string MyBranch { get; set; } = string.Empty; // optional, resolved from cheqmy_no
            [Required] public string AcctNumber { get; set; } = string.Empty; // acct_number
            [Required] public string CheqNo { get; set; } = string.Empty; // cheq_no
            [Required] public decimal Amount { get; set; }                 // trans_amt
            [Required] public string CheqDate { get; set; } = string.Empty; // dd/MM/yyyy
            [Required] public string RemarkCode { get; set; } = string.Empty; // cheqremark_code
            [Required] public decimal Postage { get; set; }
            [Required] public decimal BankCharges { get; set; }
            [Required] public decimal Surcharge { get; set; }  // calculated: amount * percentage / 100
            [Required] public decimal Percentage { get; set; }
            [Required] public int NoMonths { get; set; }  // must be >= 3

            // Pre-filled from customer lookup — editable by user
            [Required] public string CustFname { get; set; } = string.Empty;
             public string CustLname { get; set; } = string.Empty;
            [Required] public string Address1 { get; set; } = string.Empty;
            public string Address2 { get; set; } = string.Empty;
            public string Address3 { get; set; } = string.Empty;
            public string AreaName { get; set; } = string.Empty;
        }
    }
}