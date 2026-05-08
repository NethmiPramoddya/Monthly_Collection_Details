namespace Monthly_Collection_Details.Models
{
    // ── return_cheques ───────────────────────────────────────────────────
    public class ReturnChequeDetail
    {
        public string AcctNumber { get; set; }
        public string CheqNo { get; set; }
        public string CheqDate { get; set; }
        public int NoMonths { get; set; }
        public string EntryDate { get; set; }
        public string Allow { get; set; }
        public string ProvCode { get; set; }
    }

    public class ReturnChequeRequest
    {
        public string AccountNo { get; set; }
    }

    // ── chq_mnyord ───────────────────────────────────────────────────────
    public class ChqMnyord
    {
        public string pay_mode { get; set; }
        public string acno_pivno { get; set; }
        public string trans_date { get; set; }
        public string center { get; set; }
        public string count_no { get; set; }
        public int stub_no { get; set; }
        public decimal trans_amt { get; set; }
        public string chq_mny_no { get; set; }
        public string bnk_post_code { get; set; }
        public string bran_code { get; set; }
    }

    // ── cheqmy_remarks (status1 removed - column does not exist) ─────────
    public class CheqmyRemark
    {
        public string cheqremark_code { get; set; }
        public string remark { get; set; }
    }

    // ── cheqmy_chargers ──────────────────────────────────────────────────
    public class CheqmyCharger
    {
        public decimal Postage { get; set; }
        public decimal BankCharges { get; set; }
        public decimal Percentage { get; set; }
        public int NoMonths { get; set; }
    }

    // ── provinces ────────────────────────────────────────────────────────
    public class Province
    {
        public string Status1 { get; set; }
        public string ProvCode { get; set; }
        public string ProvName { get; set; }
    }

    // ── cheqmy_details ───────────────────────────────────────────────────
    public class CheqmyDetail
    {
        public string MyaddCode { get; set; }
        public string MyBranch { get; set; }
        public string MyCode { get; set; }
        public string AcctNumber { get; set; }
        public string CheqNo { get; set; }
        public string CheqDate { get; set; }
        public int NoMonths { get; set; }
        public string EntryDate { get; set; }
        public decimal Postage { get; set; }
        public decimal Surcharge { get; set; }
        public decimal BankCharges { get; set; }
        public decimal Percentage { get; set; }
        public string Remark { get; set; }
        public decimal Amount { get; set; }
        public string CustFname { get; set; }
        public string CustLname { get; set; }
        public string Address1 { get; set; }
        public string AreaName { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Confrm { get; set; }
        public string ConDate { get; set; }
        public string ConUser { get; set; }
        public string Stjrnl { get; set; }
        public string Stprint { get; set; }
        public string Stemail { get; set; }
        public string Rathmalana { get; set; }
        public string Japura { get; set; }
        public string Colcity { get; set; }
        public string Headoffice { get; set; }
        public string Kiribathgoda { get; set; }
        public string Kandy { get; set; }
        public string Sabgamuwa { get; set; }
        public string Nwp { get; set; }
        public string Ncp { get; set; }
        public string Np { get; set; }
        public string Sp { get; set; }
        public string Uva { get; set; }
    }
}