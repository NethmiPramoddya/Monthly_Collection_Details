using Informix.Net.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Monthly_Collection_Details.Models;
using QuestPDF.Infrastructure;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Monthly_Collection_Details.Services
{
    public class DatabaseService
    {
        private readonly DatabaseSettings _dbSettings;

        public DatabaseService(IOptions<DatabaseSettings> options)
        {
            _dbSettings = options.Value;
        }

        private CollectionSummary ExecuteQuery(string connectionString, string sql, DateTime fromDate, DateTime toDate)
        {
            var result = new CollectionSummary();

            using var conn = new IfxConnection(connectionString);
            conn.Open();

            using var cmd = new IfxCommand(sql, conn);

            cmd.Parameters.Add(new IfxParameter { IfxType = IfxType.Date, Value = fromDate.Date });
            cmd.Parameters.Add(new IfxParameter { IfxType = IfxType.Date, Value = toDate.Date.AddDays(1) });

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                result.count_no = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                result.trans_amt = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
            }

            return result;
        }


        //BUSINESS METHODS

        public CollectionSummary GetCollectionSummary(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode = 'C'
                  AND pay_type = 'B'
                  AND count_no IN ('01','02','03','04','05','06','07','08','09','10','12')
                  AND trans_type = 0;
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetChequeDraftSummary(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND pay_type = 'B'
                  AND trans_type = 0
                  AND agent = 'CEBH'
                  AND pay_mode IN ('Q','D')
                  AND count_no IN ('01','05','06','07','08','09','10','11','12');
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetBankTransfer(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode = 'M'
                  AND pay_type = 'B'
                  AND count_no IN ('12','05','10','09','07','08')
                  AND trans_type = 0;
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetKioks(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode = 'C'
                  AND pay_type = 'B'
                  AND count_no IN ('0M')
                  AND trans_type = 0;
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }


        public CollectionSummary GetCredit(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND trans_type = 0
                  AND agent = 'CEBH'
                  AND pay_mode = 'R'
                  AND pay_type = 'B';
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetMudaligeMawathaCash(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode = 'C'
                  AND count_no IN ('0C','0B')
                  AND trans_type = 0
                  AND pay_type = 'B';
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetMudaligeMawathaChecksAndDrafts(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode IN ('Q', 'D')
                  AND count_no IN ('0C','0B','0A')
                  AND trans_type = 0
                  AND pay_type = 'B';
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetBambalapitiyaCash(DateTime fromDate, DateTime toDate) // bambalapitiya cash
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode = 'C'
                  AND count_no IN ('0D','0E')
                  AND trans_type = 0
                  AND pay_type = 'B';
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetBambalapitiyaChecksAndDrafts(DateTime fromDate, DateTime toDate) // bambalapitiy acheckes and drafts
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode IN ('Q', 'D')
                  AND count_no IN ('0F','0D')
                  AND trans_type = 0
                  AND pay_type = 'B';
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }


        public CollectionSummary GetMalawattaRoadcash(DateTime fromDate, DateTime toDate) // malawatta road cash
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode = 'C'
                  AND count_no IN ('0G','0H','0J')
                  AND trans_type = 0
                  AND pay_type = 'B';
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetMalawattaRoadChecksAndDrafts(DateTime fromDate, DateTime toDate) // Malawatta Road Checks and Drafts
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                       SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND pay_mode IN ('Q', 'D')
                  AND count_no IN ('0J','0G')
                  AND trans_type = 0
                  AND pay_type = 'B';
            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetSubTotal(DateTime fromDate, DateTime toDate) //Sub Total
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
                FROM cus_tran
                WHERE pay_type = 'B'
                  AND trans_date >= ?
                  AND trans_date < ?
                  AND agent = 'CEBH'
                  AND trans_type = 0
                  AND count_no NOT IN ('0K');
                            ";

            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        //second database connections


        //Peoples Bank (offline_payments – count_no = 55)

        public CollectionSummary GetPeoplesBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                        SELECT 
                COUNT(acc_no) AS PaymentCount, 
                SUM(trans_amt) AS TotalAmount 
                FROM offline_payments 
                WHERE trans_date >= ? 
                  AND trans_date < ?
                  AND count_no = '55';
        ";

            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //1.a Peoples Bank Internet Payments Bill
        public CollectionSummary GetPeoplesBankInternetPaymentsBill(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                        SELECT
                        COUNT(*) AS PaymentCount,
                        SUM(bill_amt) AS TotalAmount
                        FROM crdtauth
                        WHERE cash_date >= ?
                      AND cash_date < ?
                      AND bank_code = '7135'
                      AND bran_code = 'CRC'
                      AND payment_type = 'Bil';
        ";

            return ExecuteQuery(_dbSettings.THIRD_DB, sql, fromDate, toDate);
        }

        //1.b. Peoples Bank Internet Payments PIV 

        public CollectionSummary GetPeoplesBankInternetPaymentsPIV(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                        SELECT
                        COUNT(*) AS PaymentCount,
                        SUM(bill_amt) AS TotalAmount
                    FROM crdtauth
                    WHERE bank_date >= ?
                      AND bank_date < ?
                      AND bank_code = '7135'
                      AND bran_code = 'CRC'
                      AND payment_type = 'PIV';
        ";

            return ExecuteQuery(_dbSettings.THIRD_DB, sql, fromDate, toDate);
        }


        ////Bank of Ceylon (count_no = 30)

        public CollectionSummary GetBankOfCeylon(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                    SELECT 
                        COUNT(acc_no) AS PaymentCount,
                        SUM(trans_amt) AS TotalAmount
                    FROM offline_payments
                    WHERE trans_date >= ?
                      AND trans_date < ?
                      AND count_no = '30';
                    ";

            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        public CollectionSummary GetNationalSavingsBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                        SELECT 
                        COUNT(acc_no) AS PaymentCount,
                        SUM(trans_amt) AS TotalAmount
                        FROM offline_payments
                        WHERE trans_date >= ?
                        AND trans_date < ?
                        AND count_no = '56';
        ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //4. Hatton National Bank
        public CollectionSummary GetHattonNationalBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '35'
                  AND center <> 'CRC'
                  AND pay_mode <> 'I';
            ";

            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //6. Commercial Bank
        public CollectionSummary GetCommercialBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '37';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //7. HSBC
        public CollectionSummary GetHSBC(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '39';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //8. National Development Bank
        public CollectionSummary GetNationalDevelopmentBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '81';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //9.a National Trust Bank
        public CollectionSummary GetNationalTrustBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '80';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //9.b. Amex Internet Payments(NTB) Bill
        public CollectionSummary GetAmexInternetPaymentsNTBBill(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(bill_amt) AS TotalAmount
            FROM crdtauth
            WHERE cash_date >= ?
              AND cash_date < ?
              AND bank_code = '7162'
              AND bran_code = 'CRC'
              AND payment_type = 'Bil';
            ";
            return ExecuteQuery(_dbSettings.THIRD_DB, sql, fromDate, toDate);
        }


        //9.b. Amex Internet Payments(NTB) PIV
        public CollectionSummary GetAmexInternetPaymentsNTBPIV(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(bill_amt) AS TotalAmount
                FROM crdtauth
                WHERE bank_date >= ?
                  AND bank_date < ?
                  AND bank_code = '7162'
                  AND bran_code = 'CRC'
                  AND payment_type = 'PIV';
            ";
            return ExecuteQuery(_dbSettings.THIRD_DB, sql, fromDate, toDate);
        }


        //10. Sampath Bank
        public CollectionSummary GetSampathBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '29'
                  AND agent <> 'ARPC';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //11. Seylan Bank
        public CollectionSummary GetSeylanBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no IN ('28','36');
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //12. Union Bank
        public CollectionSummary GetUnionBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '33';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //13. HDFC Bank
        public CollectionSummary GetHdfcBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '20';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //14. DFCC Bank
        public CollectionSummary GetDfccBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '24';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //15. ABANS
        public CollectionSummary GetAbans(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '42';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //16. SINGER
        public CollectionSummary GetSinger(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '41';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //17. Cargills Food City
        public CollectionSummary GetCargills(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '31';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //18. PanAsia Bank
        public CollectionSummary GetPanAsiaBank(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '26';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //19. Arpico Supermarket
        public CollectionSummary GetArpico(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT COUNT(acc_no) AS PaymentCount, SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND (
                        count_no = '27'
                        OR (count_no = '29' AND agent = 'ARPC')
                      );
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //20.a CEB Internet Payments(HNB) Bill
        public CollectionSummary GetCEBInternetPaymentsHNBBill(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(bill_amt) AS TotalAmount
            FROM crdtauth
            WHERE cash_date >= ?
              AND cash_date < ?
              AND bank_code = '7083'
              AND bran_code = 'CRC'
              AND payment_type = 'Bil';
            ";
            return ExecuteQuery(_dbSettings.THIRD_DB, sql, fromDate, toDate);
        }

        //20.b CEB Internet Payments(HNB) Piv 
        public CollectionSummary GetCEBInternetPaymentsHNBPiv(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                        SELECT
                    COUNT(*) AS PaymentCount,
                    SUM(bill_amt) AS TotalAmount
                    FROM crdtauth
                    WHERE cash_date >= ?
                      AND cash_date < ?
                      AND bank_code = '7083'
                      AND bran_code = 'CRC'
                      AND payment_type = 'PIV';
            ";
            return ExecuteQuery(_dbSettings.THIRD_DB, sql, fromDate, toDate);
        }

        //21. Mobitel
        public CollectionSummary GetMobitel(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(acc_no) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date <  ?
                  AND count_no = '23';
            ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //22. Dialog
        public CollectionSummary GetDialog(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(acc_no) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '22';
                        ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //23. Laugh Supermarket
        public CollectionSummary GetLaughSupermarket(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(acc_no) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                AND trans_date < ?
                AND count_no = '25';
                        ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //24. Postal Department
        public CollectionSummary GetPostalDepartment(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(acc_no) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                  AND trans_date < ?
                  AND count_no = '40';
                        ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //25. Regional Development Bank(RDB)"
        public CollectionSummary GetRegionalDevelopmentBankRDB(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(acc_no) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
                FROM offline_payments
                WHERE trans_date >= ?
                AND trans_date < ?
                AND count_no = '32';
                        ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }

        //25. Agent collection Sub total
        public CollectionSummary GetAgentSubTotal(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                        SELECT
                        COUNT(acc_no) AS PaymentCount,
                        SUM(trans_amt) AS TotalAmount
                        FROM offline_payments
                        WHERE trans_date >= ?
                      AND trans_date < ?
                      AND count_no IN (
                            '55','30','56','35','34','37','38','39',
                            '81','80','29','28','36','33','42','41',
                            '31','20','21','22','23','24','25','26',
                            '27','40','32'
                          )
                      AND center <> 'CRC';
                        ";
            return ExecuteQuery(_dbSettings.SECOND_DB, sql, fromDate, toDate);
        }


        //PIV Section
        public CollectionSummary GetCashPayments(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
            FROM cus_tran
            WHERE trans_date >= ?
              AND trans_date < ?
              AND trans_type = 0
              AND pay_type = 'P'
              AND agent = 'CEBH'
              AND pay_mode IN ('C');
                        ";
            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetChequeDraftPayments(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
            FROM cus_tran
            WHERE trans_date >= ?
              AND trans_date < ?
              AND trans_type = 0
              AND pay_type = 'P'
              AND agent = 'CEBH'
              AND pay_mode IN ('Q','D');
                        ";
            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetCreditCardHQ(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
            FROM cus_tran
            WHERE trans_date >= ?
              AND trans_date < ?
              AND trans_type = 0
              AND pay_type = 'P'
              AND agent = 'CEBH'
              AND pay_mode IN ('R');
                        ";
            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetDirectTransferHQPIV(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
            FROM cus_tran
            WHERE trans_date >= ?
              AND trans_date < ?
              AND trans_type = 0
              AND pay_type = 'P'
              AND agent = 'CEBH'
              AND pay_mode IN ('M');
                        ";
            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        public CollectionSummary GetTotalSMOthers(DateTime fromDate, DateTime toDate)
        {
            string sql = @"
                SELECT
                COUNT(*) AS PaymentCount,
                SUM(trans_amt) AS TotalAmount
            FROM cus_tran
            WHERE trans_date >= ?
              AND trans_date <  ?
              AND trans_type = 0
              AND pay_type = 'P'
              AND agent = 'CEBH';
                        ";
            return ExecuteQuery(_dbSettings.POS, sql, fromDate, toDate);
        }

        

    }
}