using Informix.Net.Core;
using Monthly_Collection_Details.Models;

namespace Monthly_Collection_Details.Services
{
    public class Service2
    {
        private readonly string _connectionString;
        private readonly string _connectionString2;
        private readonly string _connectionString3;

        public Service2(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LargestCustomers");
            _connectionString2 = configuration.GetConnectionString("POS");
            _connectionString3 = configuration.GetConnectionString("JDBC");
        }

        // ── DB1: POST - Get return_cheques by acct_number ────────────────
        public async Task<List<ReturnChequeDetail>> GetReturnChequeDetailsByAccountAsync(string accountNo)
        {
            var results = new List<ReturnChequeDetail>();

            using (var conn = new IfxConnection(_connectionString2))
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

        // ── DB3: GET - Get all chq_mnyord ────────────────────────────────
        public async Task<List<ChqMnyord>> GetAllChqMnyordAsync()
        {
            var results = new List<ChqMnyord>();

            using (var conn = new IfxConnection(_connectionString3))
            {
                await conn.OpenAsync();

                string query = @"SELECT pay_mode, acno_pivno, trans_date, center, count_no,
                                        stub_no, trans_amt, chq_mny_no, bnk_post_code, bran_code
                                 FROM chq_mnyord";

                using (var cmd = new IfxCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new ChqMnyord
                            {
                                pay_mode = reader["pay_mode"] != DBNull.Value ? reader["pay_mode"].ToString() : null,
                                acno_pivno = reader["acno_pivno"] != DBNull.Value ? reader["acno_pivno"].ToString() : null,
                                trans_date = reader["trans_date"] != DBNull.Value ? reader["trans_date"].ToString() : null,
                                center = reader["center"] != DBNull.Value ? reader["center"].ToString() : null,
                                count_no = reader["count_no"] != DBNull.Value ? reader["count_no"].ToString() : null,
                                stub_no = reader["stub_no"] != DBNull.Value ? Convert.ToInt32(reader["stub_no"]) : 0,
                                trans_amt = reader["trans_amt"] != DBNull.Value ? Convert.ToDecimal(reader["trans_amt"]) : 0,
                                chq_mny_no = reader["chq_mny_no"] != DBNull.Value ? reader["chq_mny_no"].ToString() : null,
                                bnk_post_code = reader["bnk_post_code"] != DBNull.Value ? reader["bnk_post_code"].ToString() : null,
                                bran_code = reader["bran_code"] != DBNull.Value ? reader["bran_code"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }

        // ── DB2: GET - Get all cheqmy_remarks ────────────────────────────
        public async Task<List<CheqmyRemark>> GetAllCheqmyRemarksAsync()
        {
            var results = new List<CheqmyRemark>();

            using (var conn = new IfxConnection(_connectionString))
            {
                await conn.OpenAsync();

                // ⚠️ Removed status1 — column does not exist in this table
                string query = @"SELECT cheqremark_code, remark
                                 FROM cheqmy_remarks";

                using (var cmd = new IfxCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new CheqmyRemark
                            {
                                cheqremark_code = reader["cheqremark_code"] != DBNull.Value ? reader["cheqremark_code"].ToString() : null,
                                remark = reader["remark"] != DBNull.Value ? reader["remark"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }

        // ── DB2: GET - Get all cheqmy_chargers ───────────────────────────
        public async Task<List<CheqmyCharger>> GetAllCheqmyChargersAsync()
        {
            var results = new List<CheqmyCharger>();

            using (var conn = new IfxConnection(_connectionString))
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

        // ── DB2: GET - Get all provinces ─────────────────────────────────
        public async Task<List<Province>> GetAllProvincesAsync()
        {
            var results = new List<Province>();

            using (var conn = new IfxConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT status1, prov_code, prov_name
                                 FROM provinces";

                using (var cmd = new IfxCommand(query, conn))
                {
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

        // ── DB2: GET - Get all cheqmy_details ────────────────────────────
        public async Task<List<CheqmyDetail>> GetAllCheqmyDetailsAsync()
        {
            var results = new List<CheqmyDetail>();

            using (var conn = new IfxConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT FIRST 50 myadd_code, my_branch, my_code, acct_number, cheq_no,
                        cheq_date, no_months, entry_date, postage, surcharge,
                        bank_charges, percentage, remark, amount, cust_fname,
                        cust_lname, address_1, area_name, address_2, address_3,
                        confrm, con_date, con_user, stjrnl, stprint, stemail,
                        rathmalana, japura, colcity, headoffice, kiribathgoda,
                        kandy, sabgamuwa, nwp, ncp, np, sp, uva
                 FROM cheqmy_details";

                using (var cmd = new IfxCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new CheqmyDetail
                            {
                                MyaddCode = reader["myadd_code"] != DBNull.Value ? reader["myadd_code"].ToString() : null,
                                MyBranch = reader["my_branch"] != DBNull.Value ? reader["my_branch"].ToString() : null,
                                MyCode = reader["my_code"] != DBNull.Value ? reader["my_code"].ToString() : null,
                                AcctNumber = reader["acct_number"] != DBNull.Value ? reader["acct_number"].ToString() : null,
                                CheqNo = reader["cheq_no"] != DBNull.Value ? reader["cheq_no"].ToString() : null,
                                CheqDate = reader["cheq_date"] != DBNull.Value ? reader["cheq_date"].ToString() : null,
                                NoMonths = reader["no_months"] != DBNull.Value ? Convert.ToInt32(reader["no_months"]) : 0,
                                EntryDate = reader["entry_date"] != DBNull.Value ? reader["entry_date"].ToString() : null,
                                Postage = reader["postage"] != DBNull.Value ? Convert.ToDecimal(reader["postage"]) : 0,
                                Surcharge = reader["surcharge"] != DBNull.Value ? Convert.ToDecimal(reader["surcharge"]) : 0,
                                BankCharges = reader["bank_charges"] != DBNull.Value ? Convert.ToDecimal(reader["bank_charges"]) : 0,
                                Percentage = reader["percentage"] != DBNull.Value ? Convert.ToDecimal(reader["percentage"]) : 0,
                                Remark = reader["remark"] != DBNull.Value ? reader["remark"].ToString() : null,
                                Amount = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0,
                                CustFname = reader["cust_fname"] != DBNull.Value ? reader["cust_fname"].ToString() : null,
                                CustLname = reader["cust_lname"] != DBNull.Value ? reader["cust_lname"].ToString() : null,
                                Address1 = reader["address_1"] != DBNull.Value ? reader["address_1"].ToString() : null,
                                AreaName = reader["area_name"] != DBNull.Value ? reader["area_name"].ToString() : null,
                                Address2 = reader["address_2"] != DBNull.Value ? reader["address_2"].ToString() : null,
                                Address3 = reader["address_3"] != DBNull.Value ? reader["address_3"].ToString() : null,
                                Confrm = reader["confrm"] != DBNull.Value ? reader["confrm"].ToString() : null,
                                ConDate = reader["con_date"] != DBNull.Value ? reader["con_date"].ToString() : null,
                                ConUser = reader["con_user"] != DBNull.Value ? reader["con_user"].ToString() : null,
                                Stjrnl = reader["stjrnl"] != DBNull.Value ? reader["stjrnl"].ToString() : null,
                                Stprint = reader["stprint"] != DBNull.Value ? reader["stprint"].ToString() : null,
                                Stemail = reader["stemail"] != DBNull.Value ? reader["stemail"].ToString() : null,
                                Rathmalana = reader["rathmalana"] != DBNull.Value ? reader["rathmalana"].ToString() : null,
                                Japura = reader["japura"] != DBNull.Value ? reader["japura"].ToString() : null,
                                Colcity = reader["colcity"] != DBNull.Value ? reader["colcity"].ToString() : null,
                                Headoffice = reader["headoffice"] != DBNull.Value ? reader["headoffice"].ToString() : null,
                                Kiribathgoda = reader["kiribathgoda"] != DBNull.Value ? reader["kiribathgoda"].ToString() : null,
                                Kandy = reader["kandy"] != DBNull.Value ? reader["kandy"].ToString() : null,
                                Sabgamuwa = reader["sabgamuwa"] != DBNull.Value ? reader["sabgamuwa"].ToString() : null,
                                Nwp = reader["nwp"] != DBNull.Value ? reader["nwp"].ToString() : null,
                                Ncp = reader["ncp"] != DBNull.Value ? reader["ncp"].ToString() : null,
                                Np = reader["np"] != DBNull.Value ? reader["np"].ToString() : null,
                                Sp = reader["sp"] != DBNull.Value ? reader["sp"].ToString() : null,
                                Uva = reader["uva"] != DBNull.Value ? reader["uva"].ToString() : null
                            });
                        }
                    }
                }
            }

            return results;
        }
    }
}

    
