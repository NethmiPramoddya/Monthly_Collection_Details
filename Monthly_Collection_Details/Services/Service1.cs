using Informix.Net.Core;
using Microsoft.Extensions.Configuration;
using Monthly_Collection_Details.Models;

namespace Monthly_Collection_Details.Services
{
    public class Service1
    {
        private readonly string _connectionString;   // LargestCustomers — cheqmy_*, prn_dat_1
        private readonly string _connectionString2;  // pmnt_consld      — chq_mnyord
        private readonly HttpClient _httpClient;
        private readonly string _billCycleUrl;

        public Service1(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LargestCustomers");
            _connectionString2 = configuration.GetConnectionString("pmnt_consld");
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _billCycleUrl = configuration["ExternalApis:BillCycleUrl"];
        }

        // ──────────────────────────────────────────────────────────────────
        // GET all rows from cheqmy_no
        // Used to populate the province dropdown on the login page
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.CheqMyNoRecord>> GetAllAsync()
        {
            var records = new List<Model1.CheqMyNoRecord>();

            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = @"
                SELECT
                    my_branch,
                    myadd_code,
                    opentime,
                    mycode_desc
                FROM cheqmy_no
                ORDER BY my_branch";

            using var cmd = new IfxCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new Model1.CheqMyNoRecord
                {
                    MyBranch = reader["my_branch"].ToString()?.Trim() ?? "",
                    MyAddCode = reader["myadd_code"].ToString()?.Trim() ?? "",
                    OpenTime = reader["opentime"].ToString()?.Trim() ?? "",
                    MyCodeDesc = reader["mycode_desc"].ToString()?.Trim() ?? ""
                });
            }

            return records;
        }

        // ──────────────────────────────────────────────────────────────────
        // GET cheque report filtered by province code and date range
        // myAddCode comes from login — user is locked to their own province
        // toDate is always overridden to today in the controller
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.ChequeReportRecord>> GetReportAsync(
            string myAddCode, DateOnly fromDate, DateOnly toDate)
        {
            var records = new List<Model1.ChequeReportRecord>();

            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = @"
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

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 10 };
            cmd.Parameters.Add(new IfxParameter { Value = myAddCode.Trim().ToUpper() });
            cmd.Parameters.Add(new IfxParameter { Value = fromDate.ToDateTime(TimeOnly.MinValue) });
            cmd.Parameters.Add(new IfxParameter { Value = toDate.ToDateTime(TimeOnly.MinValue) });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new Model1.ChequeReportRecord
                {
                    Branch = reader["my_branch"].ToString()?.Trim() ?? "",
                    NoticeNo = reader["my_code"].ToString()?.Trim() ?? "",
                    Account = reader["acct_number"].ToString()?.Trim() ?? "",
                    ChequeNo = reader["cheq_no"].ToString()?.Trim() ?? "",
                    ChequeDate = reader["cheq_date"].ToString()?.Trim() ?? "",
                    Months = Convert.ToInt32(reader["no_months"]),
                    Percentage = Convert.ToDecimal(reader["percentage"]),
                    EntryDate = DateOnly.FromDateTime(Convert.ToDateTime(reader["entry_date"])),
                    Postage = Convert.ToDecimal(reader["postage"]),
                    Surcharge = Convert.ToDecimal(reader["surcharge"]),
                    BankCharges = Convert.ToDecimal(reader["bank_charges"]),
                    Remark = reader["remark"].ToString()?.Trim() ?? "",
                    Amount = Convert.ToDecimal(reader["amount"]),
                    Name = (reader["cust_fname"].ToString()?.Trim()
                                + " " + reader["cust_lname"].ToString()?.Trim()).Trim(),
                    Address = reader["full_address"].ToString()?.Trim() ?? "",
                    Area = reader["area_name"].ToString()?.Trim() ?? ""
                });
            }

            return records;
        }

        // ──────────────────────────────────────────────────────────────────
        // GET single notice detail for PDF generation
        // myAddCode from login — prevents cross-province access
        // ──────────────────────────────────────────────────────────────────
        public async Task<Model1.ChequeNoticeDetail?> GetNoticeDetailAsync(
            string noticeNo, string myAddCode)
        {
            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = @"
                SELECT
                    d.my_code,
                    d.acct_number,
                    d.cheq_no,
                    d.cheq_date,
                    d.entry_date,
                    d.cust_fname,
                    d.cust_lname,
                    d.address_1,
                    d.address_2,
                    d.address_3,
                    d.amount,
                    d.postage,
                    d.bank_charges,
                    d.surcharge,
                    d.percentage,
                    d.remark,
                    d.myadd_code,
                    a.myadd_tel,
                    a.myadd_desc1,
                    a.myadd_desc2
                FROM cheqmy_details d
                INNER JOIN cheqmy_address a ON a.myadd_code = d.myadd_code
                WHERE d.my_code    = ?
                  AND d.myadd_code = ?";

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 10 };
            cmd.Parameters.Add(new IfxParameter { Value = noticeNo.Trim() });
            cmd.Parameters.Add(new IfxParameter { Value = myAddCode.Trim().ToUpper() });

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            decimal amount = Convert.ToDecimal(reader["amount"]);
            decimal postage = Convert.ToDecimal(reader["postage"]);
            decimal bankCharges = Convert.ToDecimal(reader["bank_charges"]);
            decimal surcharge = Convert.ToDecimal(reader["surcharge"]);

            return new Model1.ChequeNoticeDetail
            {
                NoticeNo = reader["my_code"].ToString()?.Trim() ?? "",
                AccountNo = reader["acct_number"].ToString()?.Trim() ?? "",
                ChequeNo = reader["cheq_no"].ToString()?.Trim() ?? "",
                ChequeDate = reader["cheq_date"].ToString()?.Trim() ?? "",
                EntryDate = DateOnly
                                   .FromDateTime(Convert.ToDateTime(reader["entry_date"]))
                                   .ToString("dd/MM/yyyy"),
                CustomerName = (reader["cust_fname"].ToString()?.Trim()
                              + " " + reader["cust_lname"].ToString()?.Trim()).Trim(),
                Address1 = reader["address_1"].ToString()?.Trim() ?? "",
                Address2 = reader["address_2"].ToString()?.Trim() ?? "",
                Address3 = reader["address_3"].ToString()?.Trim() ?? "",
                Amount = amount,
                Postage = postage,
                BankCharges = bankCharges,
                Surcharge = surcharge,
                Percentage = Convert.ToDecimal(reader["percentage"]),
                Total = amount + postage + bankCharges + surcharge,
                Remark = reader["remark"].ToString()?.Trim() ?? "",
                MyAddCode = reader["myadd_code"].ToString()?.Trim() ?? "",
                Tel = reader["myadd_tel"].ToString()?.Trim() ?? "",
                OfficeDesc1 = reader["myadd_desc1"].ToString()?.Trim() ?? "",
                OfficeDesc2 = reader["myadd_desc2"].ToString()?.Trim() ?? ""
            };
        }

        // ──────────────────────────────────────────────────────────────────
        // GET bill cycles from external hosted API
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.BillCycleRecord>> GetBillCyclesAsync()
        {
            var response = await _httpClient.GetAsync(_billCycleUrl);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Bill cycle API returned {(int)response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();

            // Deserialize using the actual external API field names
            var raw = System.Text.Json.JsonSerializer.Deserialize<List<Model1.BillCycleApiDto>>(
                json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Model1.BillCycleApiDto>();

            // Map to our internal BillCycleRecord
            // Filter out any rows where bill_cycle is missing or not a valid number
            return raw
                .Where(r => !string.IsNullOrWhiteSpace(r.bill_cycle)
                         && !string.IsNullOrWhiteSpace(r.bill_mnth)
                         && int.TryParse(r.bill_cycle, out _))
                .Select(r => new Model1.BillCycleRecord
                {
                    BillCycle = int.Parse(r.bill_cycle),  // "105" → 105
                    DisplayName = $"{r.bill_cycle} — {r.bill_mnth}"          
                })
                .ToList();
        }

        // ──────────────────────────────────────────────────────────────────
        // SEARCH by Account No
        // Both DB queries fired in parallel with Task.WhenAll
        // chq_mnyord  → _connectionString2 (pmnt_consld)
        // prn_dat_1   → _connectionString  (LargestCustomers)
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.NewDefaulterResult>> SearchByAccountAsync(
            string accountNo, DateTime receivedDate, int billCycle)
        {
            string trimmedAccount = accountNo.Trim();

            // Fire both DB queries at the same time — no sequential waiting
            var chequeTask = FetchChequeRowsByAccountAsync(trimmedAccount, receivedDate);
            var customerTask = FetchCustomerByAccountAsync(trimmedAccount, billCycle);

            await Task.WhenAll(chequeTask, customerTask);

            var chequeRows = chequeTask.Result;
            var customer = customerTask.Result;

            if (chequeRows.Count == 0 || customer == null)
                return new List<Model1.NewDefaulterResult>();

            // Merge cheque rows with customer info in C#
            return chequeRows.Select(row => new Model1.NewDefaulterResult
            {
                CustomerName = customer.CustomerName,
                Address = customer.Address,
                Area = customer.Area,
                AreaCode = customer.AreaCode,
                ChequeNo = row.chequeNo,
                Amount = row.amount,
                AccountNo = row.acno,
                Branch = row.branch,
                BankCode = row.bankCode
            }).ToList();
        }

        // ──────────────────────────────────────────────────────────────────
        // SEARCH by Cheque No
        // Step 1: single-row fetch from chq_mnyord  (_connectionString2)
        // Step 2: customer lookup from prn_dat_1    (_connectionString)
        // Step 3: merge in C#
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.NewDefaulterResult>> SearchByChequeAsync(
            string chequeNo, string bankCode, string branchCode, int billCycle)
        {
            var results = new List<Model1.NewDefaulterResult>();

            // ── Step 1: chq_mnyord → _connectionString2 ───────────────────
            string? accountNo = null;
            decimal amount = 0;
            string branch = "";
            string bankCodeDb = "";
            string chequeNoDb = "";

            using (var con = new IfxConnection(_connectionString2))
            {
                await con.OpenAsync();

                const string sql = @"
                    SELECT FIRST 1
                        acno_pivno, trans_amt, bran_code, bnk_post_code, chq_mny_no
                    FROM chq_mnyord
                    WHERE TRIM(chq_mny_no)    = ?
                      AND TRIM(bnk_post_code) = ?
                      AND TRIM(bran_code)     = ?";

                using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
                cmd.Parameters.Add(new IfxParameter { Value = chequeNo.Trim() });
                cmd.Parameters.Add(new IfxParameter { Value = bankCode.Trim() });
                cmd.Parameters.Add(new IfxParameter { Value = branchCode.Trim() });

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    accountNo = reader["acno_pivno"].ToString()?.Trim();
                    amount = Convert.ToDecimal(reader["trans_amt"]);
                    branch = reader["bran_code"].ToString()?.Trim() ?? "";
                    bankCodeDb = reader["bnk_post_code"].ToString()?.Trim() ?? "";
                    chequeNoDb = reader["chq_mny_no"].ToString()?.Trim() ?? "";
                }
            }

            // Cheque not found — stop here
            if (string.IsNullOrEmpty(accountNo)) return results;

            // ── Step 2: prn_dat_1 → _connectionString ─────────────────────
            var customer = await FetchCustomerByAccountAsync(accountNo, billCycle);

            if (customer == null) return results;

            // ── Step 3: merge ──────────────────────────────────────────────
            results.Add(new Model1.NewDefaulterResult
            {
                CustomerName = customer.CustomerName,
                Address = customer.Address,
                Area = customer.Area,
                AreaCode = customer.AreaCode,
                ChequeNo = chequeNoDb,
                Amount = amount,
                AccountNo = accountNo,
                Branch = branch,
                BankCode = bankCodeDb
            });

            return results;
        }

        // ──────────────────────────────────────────────────────────────────
        // 90-DAY SEARCH — By Account No
        // Groups by ChequeNo → one dropdown entry + card per cheque
        // One account can have multiple cheques on different dates
        // ──────────────────────────────────────────────────────────────────
        public async Task<Model1.ChequeSearchResponse> Search90DayByAccountAsync(
            string accountNo, DateTime anchorDate, int billCycle)
        {
            // Step 1: all transactions for this account in the 90-day window
            var rows = await FetchTransactionsByAccountAsync(accountNo.Trim(), anchorDate);

            if (rows.Count == 0)
                return new Model1.ChequeSearchResponse { Mode = "account" };

            // Step 2: customer info — single call, reuse across all cards
            var customer = await FetchChequeCustomerByAccountAsync(accountNo.Trim());

            // Step 3: group by ChequeNo
            // Scenario A: 1 account, 1 cheque         → 1 dropdown item
            // Scenario B: 1 account, many cheques      → many dropdown items
            var grouped = rows.GroupBy(r => r.ChequeNo).ToList();

            var dropdownItems = new List<Model1.ChequeDropdownItem>();
            var cards = new List<Model1.ChequeDetailCard>();

            foreach (var grp in grouped)
            {
                var first = grp.First();
                var total = grp.Sum(r => r.Amount);
                var label = $"Cheque {grp.Key}  |  {first.ChequeDate}  |  LKR {total:N2}";

                dropdownItems.Add(new Model1.ChequeDropdownItem
                {
                    Label = label,
                    GroupKey = grp.Key,
                    Transactions = grp.ToList()
                });

                cards.Add(new Model1.ChequeDetailCard
                {
                    GroupKey = grp.Key,
                    Branch = first.BranchCode,
                    Customer = customer,
                    Transactions = grp.ToList()
                });
            }

            return new Model1.ChequeSearchResponse
            {
                Mode = "account",
                DropdownItems = dropdownItems,
                Cards = cards
            };
        }

        // ──────────────────────────────────────────────────────────────────
        // 90-DAY SEARCH — By Cheque No
        // Groups by AccountNo → one dropdown entry + card per account
        // One cheque can pay multiple accounts (split payment scenario)
        // ──────────────────────────────────────────────────────────────────
        public async Task<Model1.ChequeSearchResponse> Search90DayByChequeAsync(
            string chequeNo, string bankCode, string branchCode, int billCycle)
        {
            // Step 1: all accounts this cheque paid — last 90 days
            var rows = await FetchTransactionsByChequeAsync(
                chequeNo.Trim(), bankCode.Trim(), branchCode.Trim());

            if (rows.Count == 0)
                return new Model1.ChequeSearchResponse { Mode = "cheque" };

            // Step 2: group by AccountNo
            // Scenario A: cheque paid 1 account         → 1 dropdown item
            // Scenario B: cheque paid multiple accounts  → many dropdown items
            var grouped = rows.GroupBy(r => r.AccountNo).ToList();

            // Step 3: fetch all customer infos in parallel
            var customerTasks = grouped
                .Select(g => FetchChequeCustomerByAccountAsync(g.Key))
                .ToArray();
            var customers = await Task.WhenAll(customerTasks);

            var customerMap = grouped
                .Select((g, i) => new { g.Key, Info = customers[i] })
                .ToDictionary(x => x.Key, x => x.Info);

            var dropdownItems = new List<Model1.ChequeDropdownItem>();
            var cards = new List<Model1.ChequeDetailCard>();

            foreach (var grp in grouped)
            {
                var customer = customerMap.GetValueOrDefault(grp.Key);
                var total = grp.Sum(r => r.Amount);
                var label = $"Account {grp.Key}  |  {customer?.CustomerName ?? "Unknown"}  |  LKR {total:N2}";

                dropdownItems.Add(new Model1.ChequeDropdownItem
                {
                    Label = label,
                    GroupKey = grp.Key,
                    Transactions = grp.ToList()
                });

                cards.Add(new Model1.ChequeDetailCard
                {
                    GroupKey = grp.Key,
                    Branch = grp.First().BranchCode,
                    Customer = customer,
                    Transactions = grp.ToList()
                });
            }

            return new Model1.ChequeSearchResponse
            {
                Mode = "cheque",
                DropdownItems = dropdownItems,
                Cards = cards
            };
        }

        // ──────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // Small focused methods reused by both search methods above
        // ──────────────────────────────────────────────────────────────────

        // Fetches all cheque rows for an account number from chq_mnyord
        // Database: pmnt_consld (_connectionString2)
        private async Task<List<(string chequeNo, decimal amount, string branch, string bankCode, string acno)>>
            FetchChequeRowsByAccountAsync(string accountNo, DateTime receivedDate)
        {
            var rows = new List<(string, decimal, string, string, string)>();

            using var con = new IfxConnection(_connectionString2);
            await con.OpenAsync();

            const string sql = @"
                SELECT chq_mny_no, trans_amt, bran_code, bnk_post_code, acno_pivno
                FROM chq_mnyord
                WHERE TRIM(acno_pivno) = ?
                  AND trans_date       = ?";

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            cmd.Parameters.Add(new IfxParameter { Value = accountNo });
            cmd.Parameters.Add(new IfxParameter { Value = receivedDate });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add((
                    reader["chq_mny_no"].ToString()?.Trim() ?? "",
                    Convert.ToDecimal(reader["trans_amt"]),
                    reader["bran_code"].ToString()?.Trim() ?? "",
                    reader["bnk_post_code"].ToString()?.Trim() ?? "",
                    reader["acno_pivno"].ToString()?.Trim() ?? ""
                ));
            }

            return rows;
        }

        // Fetches customer info for an account number from prn_dat_1
        // Database: LargestCustomers (_connectionString)
        private async Task<Model1.CustomerInfo?> FetchCustomerByAccountAsync(
            string accountNo, int billCycle)
        {
            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = @"
                SELECT FIRST 1
                    cust_fname, cust_lname, address_1, address_2, address_3, area_code
                FROM prn_dat_1
                WHERE TRIM(acct_number) = ?
                  AND bill_cycle        = ?";

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            cmd.Parameters.Add(new IfxParameter { Value = accountNo });
            cmd.Parameters.Add(new IfxParameter { Value = billCycle });

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            string addr1 = reader["address_1"].ToString()?.Trim() ?? "";
            string addr2 = reader["address_2"].ToString()?.Trim() ?? "";
            string addr3 = reader["address_3"].ToString()?.Trim() ?? "";

            return new Model1.CustomerInfo
            {
                CustomerName = (reader["cust_fname"].ToString()?.Trim()
                              + " " + reader["cust_lname"].ToString()?.Trim()).Trim(),
                Address = string.Join(", ",
                               new[] { addr1, addr2, addr3 }
                               .Where(s => !string.IsNullOrEmpty(s))),
                Area = reader["area_code"].ToString()?.Trim() ?? "",
                AreaCode = reader["area_code"].ToString()?.Trim() ?? ""
            };
        }

        // Overload — no billCycle filter (used by 90-day cheque search)
        // Returns the most recent customer record for the account
        private async Task<Model1.ChequeCustomerInfo?> FetchChequeCustomerByAccountAsync(string accountNo)
        {
            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = @"
        SELECT FIRST 1
            acct_number, cust_fname, cust_lname,
            address_1, address_2, address_3, area_code
        FROM prn_dat_1
        WHERE TRIM(acct_number) = ?
        ORDER BY bill_cycle DESC";

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            cmd.Parameters.Add(new IfxParameter { Value = accountNo });

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            string addr1 = reader["address_1"].ToString()?.Trim() ?? "";
            string addr2 = reader["address_2"].ToString()?.Trim() ?? "";
            string addr3 = reader["address_3"].ToString()?.Trim() ?? "";

            return new Model1.ChequeCustomerInfo
            {
                AccountNo = reader["acct_number"].ToString()?.Trim() ?? "",
                CustomerName = (reader["cust_fname"].ToString()?.Trim()
                              + " " + reader["cust_lname"].ToString()?.Trim()).Trim(),
                Address = string.Join(", ",
                                   new[] { addr1, addr2, addr3 }
                                   .Where(s => !string.IsNullOrEmpty(s))),
                Area = reader["area_code"].ToString()?.Trim() ?? ""
            };
        }

        // Fetches all chq_mnyord rows for an account — last 90 days from anchorDate
        // anchorDate = POS received date if provided, else today
        private async Task<List<Model1.ChequeTransactionRow>> FetchTransactionsByAccountAsync(
            string accountNo, DateTime anchorDate)
        {
            var rows = new List<Model1.ChequeTransactionRow>();
            var cutoff = anchorDate.AddDays(-90);

            const string sql = @"
        SELECT acno_pivno, chq_mny_no, bnk_post_code, bran_code,
               trans_amt, trans_date
        FROM   chq_mnyord
        WHERE  TRIM(acno_pivno) = ?
          AND  trans_date      >= ?
        ORDER  BY trans_date DESC";

            using var con = new IfxConnection(_connectionString2);
            await con.OpenAsync();
            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            cmd.Parameters.Add(new IfxParameter { Value = accountNo });
            cmd.Parameters.Add(new IfxParameter { Value = cutoff });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new Model1.ChequeTransactionRow
                {
                    AccountNo = reader["acno_pivno"].ToString()?.Trim() ?? "",
                    ChequeNo = reader["chq_mny_no"].ToString()?.Trim() ?? "",
                    BankCode = reader["bnk_post_code"].ToString()?.Trim() ?? "",
                    BranchCode = reader["bran_code"].ToString()?.Trim() ?? "",
                    Amount = Convert.ToDecimal(reader["trans_amt"]),
                    ChequeDate = reader.IsDBNull(reader.GetOrdinal("trans_date")) ? "" :
                                   Convert.ToDateTime(reader["trans_date"]).ToString("dd/MM/yyyy"),
                    //ReturnReason = reader["chq_mnyord"].ToString()?.Trim() ?? ""
                });
            }
            return rows;
        }

        // Fetches all chq_mnyord rows for a cheque number — last 90 days
        // All accounts that cheque paid for come back as separate rows
        private async Task<List<Model1.ChequeTransactionRow>> FetchTransactionsByChequeAsync(
            string chequeNo, string bankCode, string branchCode)
        {
            var rows = new List<Model1.ChequeTransactionRow>();
            var cutoff = DateTime.Today.AddDays(-90);

            const string sql = @"
        SELECT acno_pivno, chq_mny_no, bnk_post_code, bran_code,
               trans_amt, trans_date
        FROM   chq_mnyord
        WHERE  TRIM(chq_mny_no)    = ?
          AND  TRIM(bnk_post_code) = ?
          AND  TRIM(bran_code)     = ?
          AND  trans_date         >= ?
        ORDER  BY trans_date DESC";

            using var con = new IfxConnection(_connectionString2);
            await con.OpenAsync();
            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            cmd.Parameters.Add(new IfxParameter { Value = chequeNo });
            cmd.Parameters.Add(new IfxParameter { Value = bankCode });
            cmd.Parameters.Add(new IfxParameter { Value = branchCode });
            cmd.Parameters.Add(new IfxParameter { Value = cutoff });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new Model1.ChequeTransactionRow
                {
                    AccountNo = reader["acno_pivno"].ToString()?.Trim() ?? "",
                    ChequeNo = reader["chq_mny_no"].ToString()?.Trim() ?? "",
                    BankCode = reader["bnk_post_code"].ToString()?.Trim() ?? "",
                    BranchCode = reader["bran_code"].ToString()?.Trim() ?? "",
                    Amount = Convert.ToDecimal(reader["trans_amt"]),
                    ChequeDate = reader.IsDBNull(reader.GetOrdinal("trans_date")) ? "" :
                                   Convert.ToDateTime(reader["trans_date"]).ToString("dd/MM/yyyy"),
                    //ReturnReason = reader["chq_mnyord"].ToString()?.Trim() ?? ""
                });
            }
            return rows;
        }

        //insertion
        // ──────────────────────────────────────────────────────────────────
        // ▼▼▼ NEW: Fetch remark options from cheqmy_remarks ▼▼▼
        // Database: LargestCustomers (_connectionString)
        // ──────────────────────────────────────────────────────────────────
        public async Task<List<Model1.RemarkRecord>> GetRemarksAsync()
        {
            var records = new List<Model1.RemarkRecord>();

            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = "SELECT cheqremark_code, remark FROM cheqmy_remarks ORDER BY cheqremark_code";

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new Model1.RemarkRecord
                {
                    RemarkCode = reader["cheqremark_code"].ToString()?.Trim() ?? "",
                    RemarkText = reader["remark"].ToString()?.Trim() ?? ""
                });
            }

            return records;
        }

        // ──────────────────────────────────────────────────────────────────
        // ▼▼▼ NEW: Fetch charges config from cheqmy_chargers ▼▼▼
        // Returns the first row — table holds a single config row.
        // Database: LargestCustomers (_connectionString)
        // ──────────────────────────────────────────────────────────────────
        public async Task<Model1.ChargesConfig?> GetChargesConfigAsync()
        {
            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = "SELECT FIRST 1 postage, bankcharges, percentage, no_months FROM cheqmy_chargers";

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new Model1.ChargesConfig
            {
                Postage = Convert.ToDecimal(reader["postage"]),
                BankCharges = Convert.ToDecimal(reader["bankcharges"]),
                Percentage = Convert.ToDecimal(reader["percentage"]),
                NoMonths = Convert.ToInt32(reader["no_months"])
            };
        }

        // ──────────────────────────────────────────────────────────────────
        // ▼▼▼ NEW: Save cheque defaulter to cheqmy_details ▼▼▼
        // Generates the notice number (my_code) by finding the current MAX
        // and incrementing by 1 — same logic as the legacy VB.NET system.
        // Database: LargestCustomers (_connectionString)
        // Returns the generated notice number so the frontend can display it.
        // ──────────────────────────────────────────────────────────────────
        public async Task<string> SaveChequeDetailsAsync(Model1.SaveChequeDetailsRequest req)
        {
            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            // ── Step 1: Check for duplicate (same branch + cheque already entered) ──
            const string dupSql = @"
        SELECT COUNT(*) AS cnt
        FROM cheqmy_details
        WHERE my_branch = ?
          AND cheq_no   = ?";

            using (var dupCmd = new IfxCommand(dupSql, con) { CommandTimeout = 5 })
            {
                dupCmd.Parameters.Add(new IfxParameter { Value = req.MyBranch.Trim() });
                dupCmd.Parameters.Add(new IfxParameter { Value = req.CheqNo.Trim() });

                var dupResult = await dupCmd.ExecuteScalarAsync();
                int dupCount = Convert.ToInt32(dupResult);

                if (dupCount > 0)
                    throw new InvalidOperationException(
                        $"Cheque {req.CheqNo} for branch {req.MyBranch} is already recorded.");
            }

            // ── Step 2: Generate next notice number (my_code) ──────────────────────
            // Format: my_branch / YYYY / MM / SEQ  e.g. 6/2009/04/42
            // We take the MAX seq for this branch+year+month and increment it.
            string year = DateTime.Today.Year.ToString();
            string month = DateTime.Today.Month.ToString("D2");

            string maxSql = $@"
        SELECT MAX(my_code) AS max_code
        FROM cheqmy_details
        WHERE my_branch  = ?
          AND myadd_code = ?";

            // We derive the next code outside the query because Informix
            // doesn't support sequence parsing in SQL easily.
            int nextSeq = 1;

            using (var maxCmd = new IfxCommand(maxSql, con) { CommandTimeout = 5 })
            {
                maxCmd.Parameters.Add(new IfxParameter { Value = req.MyBranch.Trim() });
                maxCmd.Parameters.Add(new IfxParameter { Value = req.MyAddCode.Trim().ToUpper() });

                var raw = await maxCmd.ExecuteScalarAsync();
                if (raw != null && raw != DBNull.Value)
                {
                    // my_code format: "6/2009/04/41" → last segment is the seq
                    var parts = raw.ToString()!.Split('/');
                    if (parts.Length == 4 && int.TryParse(parts[3], out int lastSeq))
                        nextSeq = lastSeq + 1;
                }
            }

            string myCode = $"{req.MyBranch.Trim()}/{year}/{month}/{nextSeq}";

            // ── Step 3: Insert ─────────────────────────────────────────────────────
            const string insertSql = @"
        INSERT INTO cheqmy_details (
            myadd_code, my_branch, my_code,
            acct_number, cheq_no, amount,
            cheq_date, no_months, entry_date,
            postage, bank_charges, surcharge, percentage,
            remark,
            cust_fname, cust_lname,
            address_1, address_2, address_3, area_name,
            confrm, stjrnl, stprint, stemail,
            rathmalana, japura, colcity, headoffice,
            kiribathgoda, kandy, sabgamuwa, nwp
        ) VALUES (
            ?,?,?, ?,?,?, ?,?,?, ?,?,?,?,
            ?, ?,?, ?,?,?,?,
            'N','N','N','N',
            '0','0','0','0','0','0','0','0'
        )";

            using var insCmd = new IfxCommand(insertSql, con) { CommandTimeout = 10 };

            insCmd.Parameters.Add(new IfxParameter { Value = req.MyAddCode.Trim().ToUpper() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.MyBranch.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = myCode });

            insCmd.Parameters.Add(new IfxParameter { Value = req.AcctNumber.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.CheqNo.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.Amount });

            // cheq_date stored as string in the legacy table (matches original VB code)
            insCmd.Parameters.Add(new IfxParameter { Value = req.CheqDate.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.NoMonths });
            insCmd.Parameters.Add(new IfxParameter { Value = DateTime.Today });   // entry_date = today

            insCmd.Parameters.Add(new IfxParameter { Value = req.Postage });
            insCmd.Parameters.Add(new IfxParameter { Value = req.BankCharges });
            insCmd.Parameters.Add(new IfxParameter { Value = req.Surcharge });
            insCmd.Parameters.Add(new IfxParameter { Value = req.Percentage });

            insCmd.Parameters.Add(new IfxParameter { Value = req.RemarkCode.Trim() });

            insCmd.Parameters.Add(new IfxParameter { Value = req.CustFname.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.CustLname.Trim() });

            insCmd.Parameters.Add(new IfxParameter { Value = req.Address1.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.Address2.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.Address3.Trim() });
            insCmd.Parameters.Add(new IfxParameter { Value = req.AreaName.Trim() });

            await insCmd.ExecuteNonQueryAsync();

            return myCode;  // returned to frontend for display
        }
    }
}