using Informix.Net.Core;
using Monthly_Collection_Details.Models.OutstandingBalance;
using Monthly_Collection_Details.Services;
using Informix.Net.Core;
using Microsoft.Extensions.Configuration;
using Monthly_Collection_Details.Models;
using Monthly_Collection_Details.Models.OutstandingBalance;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monthly_Collection_Details.Services
{
    public class OutstandingCustomerService
    {
        private readonly string _connectionString;

        public OutstandingCustomerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LargestCustomers");
        }

        private async Task<List<LargestOutstandingCustomer>> GetTopCustomersAsync(
            string filterColumn, string filterValue, int billCycle)
        {
            var customers = new List<LargestOutstandingCustomer>();

            using (var con = new IfxConnection(_connectionString))
            {
                await con.OpenAsync();

                string sql = $@"
                    SELECT FIRST 50
                        acct_number,
                        cust_fname,
                        cust_lname,
                        address_1 || ' ' || address_2 || ' ' || address_3 AS address,
                        prov_name,
                        region,
                        area_name,
                        crnt_balance,
                        kwh_charge,
                        tariff_code
                    FROM largestout_bal
                    WHERE {filterColumn} = ?
                      AND bill_cycle = ?
                    ORDER BY crnt_balance DESC";

                using (var cmd = new IfxCommand(sql, con))
                {
                    cmd.Parameters.Add(new IfxParameter { Value = filterValue });
                    cmd.Parameters.Add(new IfxParameter { Value = billCycle });

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        customers.Add(new LargestOutstandingCustomer
                        {
                            AccountNumber = reader["acct_number"].ToString(),
                            CustomerName = reader["cust_fname"].ToString() + " " + reader["cust_lname"].ToString(),
                            Address = reader["address"].ToString(),
                            Province = reader["prov_name"].ToString(),
                            Region = reader["region"].ToString(),
                            Area = reader["area_name"].ToString(),
                            CurrentBalance = Convert.ToDecimal(reader["crnt_balance"]),
                            kwh_charge = Convert.ToDecimal(reader["kwh_charge"]),
                            tariff_code = Convert.ToDecimal(reader["tariff_code"])
                        });
                    }
                }
            }

            return customers;
        }

        public Task<List<LargestOutstandingCustomer>> GetTopProvinceCustomersAsync(string provCode, int billCycle)
        {
            return GetTopCustomersAsync("prov_code", provCode, billCycle);
        }

        public Task<List<LargestOutstandingCustomer>> GetTopRegionCustomersAsync(string region, int billCycle)
        {
            return GetTopCustomersAsync("region", region, billCycle);
        }

        public Task<List<LargestOutstandingCustomer>> GetTopAreaCustomersAsync(string areaCode, int billCycle)
        {
            return GetTopCustomersAsync("area_code", areaCode, billCycle);
        }

        // ✅ NEW: Separate method for CEB-wide query (no filter column)
        public async Task<List<LargestOutstandingCustomer>> GetTopCEBCustomersAsync(int billCycle)
        {
            var customers = new List<LargestOutstandingCustomer>();

            using (var con = new IfxConnection(_connectionString))
            {
                await con.OpenAsync();

                string sql = @"
                    SELECT FIRST 50
                        acct_number,
                        cust_fname,
                        cust_lname,
                        address_1 || ' ' || address_2 || ' ' || address_3 AS address,
                        prov_name,
                        region,
                        area_name,
                        crnt_balance,
                        kwh_charge,
                        tariff_code
                    FROM largestout_bal
                    WHERE bill_cycle = ?
                    ORDER BY crnt_balance DESC";

                using (var cmd = new IfxCommand(sql, con))
                {
                    cmd.Parameters.Add(new IfxParameter { Value = billCycle });

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        customers.Add(new LargestOutstandingCustomer
                        {
                            AccountNumber = reader["acct_number"].ToString(),
                            CustomerName = reader["cust_fname"].ToString() + " " + reader["cust_lname"].ToString(),
                            Address = reader["address"].ToString(),
                            Province = reader["prov_name"].ToString(),
                            Region = reader["region"].ToString(),
                            Area = reader["area_name"].ToString(),
                            CurrentBalance = Convert.ToDecimal(reader["crnt_balance"]),
                            kwh_charge = Convert.ToDecimal(reader["kwh_charge"]),
                            tariff_code = Convert.ToDecimal(reader["tariff_code"])

                        });
                    }
                }
            }

            return customers;
        }
    }
}