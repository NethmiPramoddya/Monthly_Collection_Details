using Informix.Net.Core;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Monthly_Collection_Details.Models;


namespace Monthly_Collection_Details.Services
{
    public class CustomerService
    {
        private readonly string _connectionString;

        public CustomerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LargestCustomers");
        }

        private async Task<List<LargestCustomer>> GetTopCustomersAsync(string filterColumn, string filterValue, int billCycle)
        {
            var customers = new List<LargestCustomer>();

            using (IfxConnection con = new IfxConnection(_connectionString))
            {
                await con.OpenAsync();

                string sql = $@"
                    SELECT FIRST 50
                        acct_number,
                        cust_fname,
                        cust_lname,
                        address_1 ||''|| address_2 ||''|| address_3 AS address,
                        prov_code,
                        region,
                        area_code,
                        kwh_cons,
                        tariff_code
                    FROM largestcustomers
                    WHERE {filterColumn} = ?
                      AND bill_cycle = ?
                    ORDER BY kwh_cons DESC";

                using (IfxCommand cmd = new IfxCommand(sql, con))
                {
                    cmd.Parameters.Add(new IfxParameter()).Value = filterValue;
                    cmd.Parameters.Add(new IfxParameter()).Value = billCycle;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customers.Add(new LargestCustomer
                            {
                                AccountNumber = reader["acct_number"].ToString(),
                                CustomerName = reader["cust_fname"] + " " + reader["cust_lname"],
                                Address = reader["address"].ToString(),
                                Province = reader["prov_code"].ToString(),
                                Region = reader["region"].ToString(),
                                Area = reader["area_code"].ToString(),
                                KwhCons = Convert.ToDecimal(reader["kwh_cons"]),
                                tariff_code = Convert.ToDecimal(reader["tariff_code"])

                            });
                        }
                    }
                }
            }

            return customers;
        }

        public Task<List<LargestCustomer>> GetTopProvinceCustomersAsync(string provCode, int billCycle)
        {
            return GetTopCustomersAsync("prov_code", provCode, billCycle);
        }

        public Task<List<LargestCustomer>> GetTopRegionCustomersAsync(string region, int billCycle)
        {
            return GetTopCustomersAsync("region", region, billCycle);
        }

        public Task<List<LargestCustomer>> GetTopAreaCustomersAsync(string areaCode, int billCycle)
        {
            return GetTopCustomersAsync("area_code", areaCode, billCycle);
        }
    }
}