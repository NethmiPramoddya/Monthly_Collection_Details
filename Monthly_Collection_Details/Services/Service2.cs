using Informix.Net.Core;
using Monthly_Collection_Details.Models;

namespace Monthly_Collection_Details.Services
{
    public class Service2
    {
        private readonly string _connectionString;

        public Service2(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("POS");
        }

        public async Task<List<ReturnChequeDetail>> GetReturnChequeDetailsByAccountAsync(string accountNo)
        {
            var results = new List<ReturnChequeDetail>();

            using (var conn = new IfxConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT acct_number, cheq_no, cheq_date, no_months, entry_date, allow, prov_code
                    FROM return_cheques
                    WHERE acct_number = ?";

                using (var cmd = new IfxCommand(query, conn))
                {
                    cmd.Parameters.Add(new IfxParameter("acct_number", IfxType.VarChar)).Value = accountNo;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new ReturnChequeDetail
                            {
                                AcctNumber = reader["acct_number"] != DBNull.Value
                                                ? reader["acct_number"].ToString() : null,
                                CheqNo = reader["cheq_no"] != DBNull.Value
                                                ? reader["cheq_no"].ToString() : null,
                                CheqDate = reader["cheq_date"] != DBNull.Value
                                                ? reader["cheq_date"].ToString() : null,
                                NoMonths = reader["no_months"] != DBNull.Value
                                                ? Convert.ToInt32(reader["no_months"]) : 0,
                                EntryDate = reader["entry_date"] != DBNull.Value
                                                ? reader["entry_date"].ToString() : null,
                                Allow = reader["allow"] != DBNull.Value
                                                ? reader["allow"].ToString() : null,
                                ProvCode = reader["prov_code"] != DBNull.Value
                                                ? reader["prov_code"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }
    }
}