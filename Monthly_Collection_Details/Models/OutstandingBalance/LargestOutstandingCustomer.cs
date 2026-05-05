using Monthly_Collection_Details.Models.OutstandingBalance;


namespace Monthly_Collection_Details.Models.OutstandingBalance
{
    public class LargestOutstandingCustomer
    {
        public string AccountNumber { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public string Region { get; set; }
        public string Area { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal kwh_charge { get; set; }
        public decimal tariff_code { get; set; }
    }
}