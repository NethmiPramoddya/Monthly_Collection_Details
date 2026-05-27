using System.ComponentModel.DataAnnotations;

namespace Monthly_Collection_Details.Models.Dishonored_Cheques_Models
{
    public class PdfModel
    {
        // ──────────────────────────────────────────────────────────────────
        // Request: Download Notice PDF
        // ──────────────────────────────────────────────────────────────────
        public class DownloadNoticePdfRequest
        {
            [Required(ErrorMessage = "Notice number is required.")]
            [StringLength(50, MinimumLength = 1)]
            public string NoticeNo { get; set; } = string.Empty;

            [Required(ErrorMessage = "Province code is required.")]
            [StringLength(3, MinimumLength = 1)]
            public string MyAddCode { get; set; } = string.Empty;
        }

        // ──────────────────────────────────────────────────────────────────
        // PDF Content — the actual data to be placed on the PDF
        // ──────────────────────────────────────────────────────────────────
        public class PdfNoticeContent
        {
            public string NoticeNo { get; set; } = string.Empty;           // my_code
            public string AccountNo { get; set; } = string.Empty;          // acct_number
            public string ChequeNo { get; set; } = string.Empty;           // cheq_no
            public string ChequeDate { get; set; } = string.Empty;         // cheq_date
            public string EntryDate { get; set; } = string.Empty;          // entry_date formatted
            public string CustomerName { get; set; } = string.Empty;       // cust_fname + cust_lname
            public string Address1 { get; set; } = string.Empty;           // address_1
            public string Address2 { get; set; } = string.Empty;           // address_2
            public string Address3 { get; set; } = string.Empty;           // address_3
            public decimal Amount { get; set; }                             // amount
            public decimal Postage { get; set; }                            // postage
            public decimal BankCharges { get; set; }                        // bank_charges
            public decimal Surcharge { get; set; }                          // surcharge
            public decimal Percentage { get; set; }                         // percentage
            public decimal Total { get; set; }                              // amount + postage + bankcharges + surcharge
            public string Remark { get; set; } = string.Empty;             // remark
            public string MyAddCode { get; set; } = string.Empty;          // myadd_code
            public string Tel { get; set; } = string.Empty;                // myadd_tel from cheqmy_address
            public string OfficeDesc1 { get; set; } = string.Empty;        // myadd_desc1 from cheqmy_address
            public string OfficeDesc2 { get; set; } = string.Empty;        // myadd_desc2 from cheqmy_address
        }
    }
}