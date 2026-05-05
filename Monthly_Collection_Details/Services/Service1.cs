using Informix.Net.Core;
using Microsoft.Extensions.Configuration;
using Monthly_Collection_Details.Models;

namespace Monthly_Collection_Details.Services
{
    public class Service1
    {
        private readonly string _connectionString;

        // One constructor — both methods share the same connection string
        public Service1(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LargestCustomers");
        }

        // ──────────────────────────────────────────────────────────────────
        // GET all rows from cheqmy_no
        // Used to populate the province dropdown on the login page
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.CheqMyNoRecord>> GetAllAsync()
        {
            var records = new List<Model1.CheqMyNoRecord>();

            using (var con = new IfxConnection(_connectionString))
            {
                await con.OpenAsync();

                string sql = @"
                    SELECT
                        my_branch,
                        myadd_code,
                        opentime,
                        mycode_desc
                    FROM cheqmy_no
                    ORDER BY my_branch";

                using (var cmd = new IfxCommand(sql, con))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Model1.CheqMyNoRecord
                        {
                            MyBranch = reader["my_branch"].ToString(),
                            MyAddCode = reader["myadd_code"].ToString(),
                            OpenTime = reader["opentime"].ToString(),
                            MyCodeDesc = reader["mycode_desc"].ToString()
                        });
                    }
                }
            }

            return records;
        }

        // ──────────────────────────────────────────────────────────────────
        // GET cheque report filtered by province code and date range
        // myAddCode comes from login — user is locked to their own province
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.ChequeReportRecord>> GetReportAsync(
            string myAddCode, DateOnly fromDate, DateOnly toDate)
        {
            var records = new List<Model1.ChequeReportRecord>();

            using (var con = new IfxConnection(_connectionString))
            {
                await con.OpenAsync();

                string sql = @"
                    SELECT
                        d.my_branch,
                        d.my_code,
                        d.acct_number,
                        d.cheq_no,
                        d.cheq_date,
                        d.no_months,
                        d.percentage,
                        d.entry_date,
                        d.postage,
                        d.surcharge,
                        d.bank_charges,
                        d.remark,
                        d.amount,
                        d.cust_fname,
                        d.cust_lname,
                        d.address_1 || ' ' || d.address_2 || ' ' || d.address_3 AS full_address,
                        d.area_name
                    FROM cheqmy_details d
                    INNER JOIN cheqmy_address a ON a.myadd_code = d.myadd_code
                    WHERE d.myadd_code  = ?
                      AND d.entry_date >= ?
                      AND d.entry_date <= ?
                    ORDER BY d.entry_date, d.my_branch, d.my_code";

                using (var cmd = new IfxCommand(sql, con))
                {
                    // Parameters in the same order as the ? placeholders above
                    cmd.Parameters.Add(new IfxParameter { Value = myAddCode.Trim().ToUpper() });
                    cmd.Parameters.Add(new IfxParameter { Value = fromDate.ToDateTime(TimeOnly.MinValue) });
                    cmd.Parameters.Add(new IfxParameter { Value = toDate.ToDateTime(TimeOnly.MinValue) });

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        records.Add(new Model1.ChequeReportRecord
                        {
                            Branch = reader["my_branch"].ToString().Trim(),
                            NoticeNo = reader["my_code"].ToString().Trim(),
                            Account = reader["acct_number"].ToString().Trim(),
                            ChequeNo = reader["cheq_no"].ToString().Trim(),
                            ChequeDate = reader["cheq_date"].ToString().Trim(),
                            Months = Convert.ToInt32(reader["no_months"]),
                            Percentage = Convert.ToDecimal(reader["percentage"]),
                            EntryDate = DateOnly.FromDateTime(Convert.ToDateTime(reader["entry_date"])),
                            Postage = Convert.ToDecimal(reader["postage"]),
                            Surcharge = Convert.ToDecimal(reader["surcharge"]),
                            BankCharges = Convert.ToDecimal(reader["bank_charges"]),
                            Remark = reader["remark"].ToString().Trim(),
                            Amount = Convert.ToDecimal(reader["amount"]),
                            Name = reader["cust_fname"].ToString().Trim()
                                          + " " + reader["cust_lname"].ToString().Trim(),
                            Address = reader["full_address"].ToString().Trim(),
                            Area = reader["area_name"].ToString().Trim()
                        });
                    }
                }
            }

            return records;
        }
    }
}