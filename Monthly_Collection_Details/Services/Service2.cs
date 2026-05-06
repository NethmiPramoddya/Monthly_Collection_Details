using Informix.Net.Core;
using Monthly_Collection_Details.Models;

namespace Monthly_Collection_Details.Services
{
    public class Service2
    {
        private readonly string _connectionString;
        private readonly string _connectionString2;

        public Service2(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("POS");
            _connectionString2 = configuration.GetConnectionString("CheckDetails");
        }

        // ── DB1: Get return_cheques by acct_number ───────────────────────
        public async Task<List<ReturnChequeDetail>> GetReturnChequeDetailsByAccountAsync(string accountNo)
        {
            var results = new List<ReturnChequeDetail>();

            using (var conn = new IfxConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT acct_number, cheq_no, cheq_date, no_months,
                                        entry_date, allow, prov_code
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
                                AcctNumber = reader["acct_number"] != DBNull.Value ? reader["acct_number"].ToString() : null,
                                CheqNo = reader["cheq_no"] != DBNull.Value ? reader["cheq_no"].ToString() : null,
                                CheqDate = reader["cheq_date"] != DBNull.Value ? reader["cheq_date"].ToString() : null,
                                NoMonths = reader["no_months"] != DBNull.Value ? Convert.ToInt32(reader["no_months"]) : 0,
                                EntryDate = reader["entry_date"] != DBNull.Value ? reader["entry_date"].ToString() : null,
                                Allow = reader["allow"] != DBNull.Value ? reader["allow"].ToString() : null,
                                ProvCode = reader["prov_code"] != DBNull.Value ? reader["prov_code"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }

        // ── DB2: Get chq_mnyord by acno_pivno ────────────────────────────
        public async Task<List<ChqMnyord>> GetChqMnyordByAccountAsync(string acnoPivno)
        {
            var results = new List<ChqMnyord>();

            using (var conn = new IfxConnection(_connectionString2))
            {
                await conn.OpenAsync();

                string query = @"SELECT pay_mode, acno_pivno, trans_date, center, count_no,
                                        stub_no, trans_amt, chq_mny_no, bnk_post_code, bran_code
                                 FROM chq_mnyord
                                 WHERE acno_pivno = ?";

                using (var cmd = new IfxCommand(query, conn))
                {
                    cmd.Parameters.Add(new IfxParameter("acno_pivno", IfxType.VarChar)).Value = acnoPivno;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new ChqMnyord
                            {
                                PayMode = reader["pay_mode"] != DBNull.Value ? reader["pay_mode"].ToString() : null,
                                AcnoPivno = reader["acno_pivno"] != DBNull.Value ? reader["acno_pivno"].ToString() : null,
                                TransDate = reader["trans_date"] != DBNull.Value ? reader["trans_date"].ToString() : null,
                                Center = reader["center"] != DBNull.Value ? reader["center"].ToString() : null,
                                CountNo = reader["count_no"] != DBNull.Value ? reader["count_no"].ToString() : null,
                                StubNo = reader["stub_no"] != DBNull.Value ? Convert.ToInt32(reader["stub_no"]) : 0,
                                TransAmt = reader["trans_amt"] != DBNull.Value ? Convert.ToDecimal(reader["trans_amt"]) : 0,
                                ChqMnyNo = reader["chq_mny_no"] != DBNull.Value ? reader["chq_mny_no"].ToString() : null,
                                BnkPostCode = reader["bnk_post_code"] != DBNull.Value ? reader["bnk_post_code"].ToString() : null,
                                BranCode = reader["bran_code"] != DBNull.Value ? reader["bran_code"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }

        // ── DB2: Get cheqmy_remarks by prov_code ─────────────────────────
        public async Task<List<CheqmyRemark>> GetCheqmyRemarksByProvCodeAsync(string provCode)
        {
            var results = new List<CheqmyRemark>();

            using (var conn = new IfxConnection(_connectionString2))
            {
                await conn.OpenAsync();

                string query = @"SELECT status1, prov_code, prov_name
                                 FROM cheqmy_remarks
                                 WHERE prov_code = ?";

                using (var cmd = new IfxCommand(query, conn))
                {
                    cmd.Parameters.Add(new IfxParameter("prov_code", IfxType.VarChar)).Value = provCode;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new CheqmyRemark
                            {
                                Status1 = reader["status1"] != DBNull.Value ? reader["status1"].ToString() : null,
                                ProvCode = reader["prov_code"] != DBNull.Value ? reader["prov_code"].ToString() : null,
                                ProvName = reader["prov_name"] != DBNull.Value ? reader["prov_name"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }

        // ── DB2: Get all cheqmy_chargers ──────────────────────────────────
        public async Task<List<CheqmyCharger>> GetAllCheqmyChargersAsync()
        {
            var results = new List<CheqmyCharger>();

            using (var conn = new IfxConnection(_connectionString2))
            {
                await conn.OpenAsync();

                string query = @"SELECT postage, bankcharges, percentage, no_months
                                 FROM cheqmy_chargers";

                using (var cmd = new IfxCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new CheqmyCharger
                            {
                                Postage = reader["postage"] != DBNull.Value ? Convert.ToDecimal(reader["postage"]) : 0,
                                BankCharges = reader["bankcharges"] != DBNull.Value ? Convert.ToDecimal(reader["bankcharges"]) : 0,
                                Percentage = reader["percentage"] != DBNull.Value ? Convert.ToDecimal(reader["percentage"]) : 0,
                                NoMonths = reader["no_months"] != DBNull.Value ? Convert.ToInt32(reader["no_months"]) : 0
                            });
                        }
                    }
                }
            }

            return results;
        }

        // ── DB2: Get provinces by prov_code ───────────────────────────────
        public async Task<List<Province>> GetProvinceByCodeAsync(string provCode)
        {
            var results = new List<Province>();

            using (var conn = new IfxConnection(_connectionString2))
            {
                await conn.OpenAsync();

                string query = @"SELECT status1, prov_code, prov_name
                                 FROM provinces
                                 WHERE prov_code = ?";

                using (var cmd = new IfxCommand(query, conn))
                {
                    cmd.Parameters.Add(new IfxParameter("prov_code", IfxType.VarChar)).Value = provCode;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new Province
                            {
                                Status1 = reader["status1"] != DBNull.Value ? reader["status1"].ToString() : null,
                                ProvCode = reader["prov_code"] != DBNull.Value ? reader["prov_code"].ToString() : null,
                                ProvName = reader["prov_name"] != DBNull.Value ? reader["prov_name"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }
    }
}