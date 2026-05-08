using System.ComponentModel.DataAnnotations;

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
            public decimal Amount { get; set; }   // amount
            public decimal Postage { get; set; }   // postage
            public decimal BankCharges { get; set; }   // bank_charges
            public decimal Surcharge { get; set; }   // surcharge
            public decimal Percentage { get; set; }   // percentage
            public decimal Total { get; set; }   // amount + postage + bankcharges + surcharge
            public string Remark { get; set; }   // remark
            public string MyAddCode { get; set; }   // myadd_code
            public string Tel { get; set; }   // myadd_tel from cheqmy_address
            public string OfficeDesc1 { get; set; }   // myadd_desc1 from cheqmy_address
            public string OfficeDesc2 { get; set; }   // myadd_desc2 from cheqmy_address
        }
    }
}