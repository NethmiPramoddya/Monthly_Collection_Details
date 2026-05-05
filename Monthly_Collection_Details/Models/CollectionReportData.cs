using Microsoft.AspNetCore.SignalR;
using Monthly_Collection_Details.Models;

namespace Monthly_Collection_Details.Models
{
    public class CollectionReportData
    {
        public required CollectionSummary Cash { get; set; }
        public required CollectionSummary ChequeDraft { get; set; }
        public required CollectionSummary BankTransfer { get; set; }
        public required CollectionSummary Kiosk { get; set; }
        public required CollectionSummary CreditHQ { get; set; }
        public required CollectionSummary MudaligeMawathaCash { get; set; }
        public required CollectionSummary MudaligeMawathaChecksAndDrafts { get; set; }
        public required CollectionSummary BambalapitiyaCash { get; set; }
        public required CollectionSummary BambalapitiyaChecksAndDrafts { get; set; } 
        public required CollectionSummary MalawattaRoadcash {  get; set; }
        public required CollectionSummary MalawattaRoadChecksAndDrafts { get; set; }
        public required CollectionSummary SubTotal { get; set; }

        // NEW (SECOND DATABASE)
        public required CollectionSummary PeoplesBank { get; set; }
        public required CollectionSummary PeoplesBankInternetBill { get; set; }
        public required CollectionSummary PeoplesBankInternetPIV { get; set; }
        public required CollectionSummary BankOfCeylon { get; set; }
        public required CollectionSummary NationalSavingsBank { get; set; }
        public required CollectionSummary HattonNationalBank { get; set; }
        public required CollectionSummary CommercialBank { get; set; }
        public required CollectionSummary HSBC { get; set; }
        public required CollectionSummary NationalDevelopmentBank { get; set; }
        public required CollectionSummary NationalTrustBank { get; set; }
        public required CollectionSummary AmexInternetPaymentsNTBBill { get; set; }
        public required CollectionSummary AmexInternetPaymentsNTBPiv { get; set; }
        public required CollectionSummary SampathBank { get; set; }
        public required CollectionSummary SeylanBank { get; set; }
        public required CollectionSummary UnionBank { get; set; }
        public required CollectionSummary HdfcBank { get; set; }
        public required CollectionSummary DfccBank { get; set; }
        public required CollectionSummary Abans { get; set; }
        public required CollectionSummary Singer { get; set; }
        public required CollectionSummary Cargills { get; set; }
        public required CollectionSummary PanAsiaBank { get; set; }
        public required CollectionSummary Arpico { get; set; }
        public required CollectionSummary CEBInternetPaymentsHNBBill {  get; set; }
        public required CollectionSummary CEBInternetPaymentsHNBPiv { get; set; }
        public required CollectionSummary Mobitel { get; set; }
        public required CollectionSummary Dialog { get; set; }
        public required CollectionSummary LaughSupermarket { get; set; }
        public required CollectionSummary PostalDepartment { get; set; }
        public required CollectionSummary RegionalDevelopmentBankRDB { get; set; }
        public required CollectionSummary AgentSubTotal { get; set; }
        public required CollectionSummary TotalCollectionOnSales { get; set; }

        //PIV

        public required CollectionSummary CashPayments { get; set; }
        public required CollectionSummary ChequeDraftPayments { get; set; }
        public required CollectionSummary CreditCardHQ { get; set; }
        public required CollectionSummary DirectTransferHQPIV { get; set; }
        public required CollectionSummary TotalSMOthers { get; set; }
        public required CollectionSummary GrandTotal { get; set; }

    }
}
