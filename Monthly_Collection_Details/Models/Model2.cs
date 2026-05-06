namespace Monthly_Collection_Details.Models
{
    // ── Existing model (keep as is) ──────────────────────────────────────
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

    // ── NEW: chq_mnyord ─────────────────────────────────────────────────
    public class ChqMnyord
    {
        public string PayMode { get; set; }
        public string AcnoPivno { get; set; }
        public string TransDate { get; set; }
        public string Center { get; set; }
        public string CountNo { get; set; }
        public int StubNo { get; set; }
        public decimal TransAmt { get; set; }
        public string ChqMnyNo { get; set; }
        public string BnkPostCode { get; set; }
        public string BranCode { get; set; }
    }

    public class ChqMnyordRequest
    {
        public string AcnoPivno { get; set; }
    }

    // ── NEW: cheqmy_remarks ──────────────────────────────────────────────
    public class CheqmyRemark
    {
        public string Status1 { get; set; }
        public string ProvCode { get; set; }
        public string ProvName { get; set; }
    }

    public class CheqmyRemarkRequest
    {
        public string ProvCode { get; set; }
    }

    // ── NEW: cheqmy_chargers ─────────────────────────────────────────────
    public class CheqmyCharger
    {
        public decimal Postage { get; set; }
        public decimal BankCharges { get; set; }
        public decimal Percentage { get; set; }
        public int NoMonths { get; set; }
    }

    // ── NEW: provinces ───────────────────────────────────────────────────
    public class Province
    {
        public string Status1 { get; set; }
        public string ProvCode { get; set; }
        public string ProvName { get; set; }
    }

    public class Province2Request
    {
        public string ProvCode { get; set; }
    }
}