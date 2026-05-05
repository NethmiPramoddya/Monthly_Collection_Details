namespace Monthly_Collection_Details.Models
{
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
}