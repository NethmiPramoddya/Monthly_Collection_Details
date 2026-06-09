using Informix.Net.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Monthly_Collection_Details.Models;
using System.Globalization;

namespace Monthly_Collection_Details.Services
{
    public class Service1
    {
        private readonly string _connectionString;   // LargestCustomers — cheqmy_*, prn_dat_1
        private readonly string _connectionString2;  // pmnt_consld      — chq_mnyord
        private readonly string _bulkDb;              // BulkDb           — for future bulk operations, not used yet
        private readonly HttpClient _httpClient;
        private readonly string _billCycleUrl;
        private readonly ILogger<Service1> _logger;

        public Service1(IConfiguration configuration, ILogger<Service1> logger)
        {
            _connectionString = configuration.GetConnectionString("LargestCustomers");
            _connectionString2 = configuration.GetConnectionString("pmnt_consld");
            _bulkDb = configuration.GetConnectionString("BulkDb");
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _billCycleUrl = configuration["ExternalApis:BillCycleUrl"];
            _logger = logger;
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
                  AND d.confrm      = 'Y'
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
                    d.area_name,    
                    a.myadd_tel,
                    a.myadd_desc1,
                    a.myadd_desc2,
                    a.myadd_desc3,
                    a.myadd_desc4,
                    d.my_branch
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
                Area = reader["area_name"].ToString()?.Trim() ?? "",
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
                OfficeDesc2 = reader["myadd_desc2"].ToString()?.Trim() ?? "",
                OfficeDesc3 = reader["myadd_desc3"].ToString()?.Trim() ?? "",
                OfficeDesc4 = reader["myadd_desc4"].ToString()?.Trim() ?? "",
                MyBranch = reader["my_branch"].ToString()?.Trim() ?? "",
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
        string accountNo, DateTime receivedDate, int billCycle,
        string myAddCode, Model1.BillingType billingType)        // ← ADD billingType
        {
            string trimmedAccount = accountNo.Trim();

            var chequeTask = FetchChequeRowsByAccountAsync(trimmedAccount, receivedDate);

            // ← Route customer fetch based on billing type
            var customerTask = billingType == Model1.BillingType.HeavySupply
                ? FetchHeavyCustomerByAccountAsync(trimmedAccount, billCycle)
                : FetchCustomerByAccountAsync(trimmedAccount, billCycle);

            var branchTask = ResolveMyBranchAsync(myAddCode.Trim().ToUpper());

            await Task.WhenAll(chequeTask, customerTask, branchTask);

            var chequeRows = chequeTask.Result;
            var customer = customerTask.Result;
            var myBranch = branchTask.Result;

            if (chequeRows.Count == 0 || customer == null)
                return new List<Model1.NewDefaulterResult>();

            return chequeRows.Select(row => new Model1.NewDefaulterResult
            {
                CustomerName = customer.CustomerName,
                Address = customer.Address,
                Area = customer.Area,
                AreaCode = customer.AreaCode,
                ChequeNo = row.chequeNo,
                Amount = row.amount,
                AccountNo = row.acno,
                Branch = myBranch,
                BranchCode = row.branch,
                BankCode = row.bankCode
            }).ToList();
        }


        public async Task<List<Model1.NewDefaulterResult>> SearchByChequeAsync(
    string chequeNo, string bankCode, string branchCode, int billCycle, string myAddCode, Model1.BillingType billingType)
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

            if (string.IsNullOrEmpty(accountNo)) return results;

            // ── Step 2: prn_dat_1 + cheqmy_no — fire in parallel ──────────
            var customerTask = billingType == Model1.BillingType.HeavySupply
        ? FetchHeavyCustomerByAccountAsync(accountNo, billCycle)
        : FetchCustomerByAccountAsync(accountNo, billCycle);

            var branchTask = ResolveMyBranchAsync(myAddCode.Trim().ToUpper());

            await Task.WhenAll(customerTask, branchTask);

            var customer = customerTask.Result;
            var myBranch = branchTask.Result;

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
                Branch = myBranch,   // ← my_branch display value
                BranchCode = branch,     // ← bran_code kept for functional use
                BankCode = bankCodeDb
            });

            return results;
        }

        // ──────────────────────────────────────────────────────────────────
        // SEARCH by Cheque No
        // Step 1: single-row fetch from chq_mnyord  (_connectionString2)
        // Step 2: customer lookup from prn_dat_1    (_connectionString)
        // Step 3: merge in C#
        // ──────────────────────────────────────────────────────────────────
        //public async Task<List<Model1.NewDefaulterResult>> SearchByChequeAsync(
        //    string chequeNo, string bankCode, string branchCode, int billCycle)
        //{
        //    var results = new List<Model1.NewDefaulterResult>();

        //    // ── Step 1: chq_mnyord → _connectionString2 ───────────────────
        //    string? accountNo = null;
        //    decimal amount = 0;
        //    string branch = "";
        //    string bankCodeDb = "";
        //    string chequeNoDb = "";

        //    using (var con = new IfxConnection(_connectionString2))
        //    {
        //        await con.OpenAsync();

        //        const string sql = @"
        //            SELECT FIRST 1
        //                acno_pivno, trans_amt, bran_code, bnk_post_code, chq_mny_no
        //            FROM chq_mnyord
        //            WHERE TRIM(chq_mny_no)    = ?
        //              AND TRIM(bnk_post_code) = ?
        //              AND TRIM(bran_code)     = ?";

        //        using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
        //        cmd.Parameters.Add(new IfxParameter { Value = chequeNo.Trim() });
        //        cmd.Parameters.Add(new IfxParameter { Value = bankCode.Trim() });
        //        cmd.Parameters.Add(new IfxParameter { Value = branchCode.Trim() });

        //        using var reader = await cmd.ExecuteReaderAsync();
        //        if (await reader.ReadAsync())
        //        {
        //            accountNo = reader["acno_pivno"].ToString()?.Trim();
        //            amount = Convert.ToDecimal(reader["trans_amt"]);
        //            branch = reader["bran_code"].ToString()?.Trim() ?? "";
        //            bankCodeDb = reader["bnk_post_code"].ToString()?.Trim() ?? "";
        //            chequeNoDb = reader["chq_mny_no"].ToString()?.Trim() ?? "";
        //        }
        //    }

        //    // Cheque not found — stop here
        //    if (string.IsNullOrEmpty(accountNo)) return results;

        //    // ── Step 2: prn_dat_1 → _connectionString ─────────────────────
        //    var customer = await FetchCustomerByAccountAsync(accountNo, billCycle);

        //    if (customer == null) return results;

        //    // ── Step 3: merge ──────────────────────────────────────────────
        //    results.Add(new Model1.NewDefaulterResult
        //    {
        //        CustomerName = customer.CustomerName,
        //        Address = customer.Address,
        //        Area = customer.Area,
        //        AreaCode = customer.AreaCode,
        //        ChequeNo = chequeNoDb,
        //        Amount = amount,
        //        AccountNo = accountNo,
        //        Branch = myBranch,    // ← D.G.M/C.C/ACCT/REV/C — shown on frontend
        //        BranchCode = branch,      // ← 001 — kept for functional use, not displayed
        //        BankCode = bankCodeDb
        //    });

        //    return results;
        //}

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
        // Resolves area_name from areas table — shows name not code
        // Database: LargestCustomers (_connectionString)
        private async Task<Model1.CustomerInfo?> FetchCustomerByAccountAsync(
            string accountNo, int billCycle)
        {
            // ── Step 1: fetch customer row from prn_dat_1 ──────────────────────
            string fname, lname, addr1, addr2, addr3, areaCd;

            using (var con = new IfxConnection(_connectionString))
            {
                await con.OpenAsync();

                const string sql = @"
            SELECT FIRST 1
                cust_fname, cust_lname,
                address_1, address_2, address_3,
                area_code
            FROM prn_dat_1
            WHERE TRIM(acct_number) = ?
              AND bill_cycle        = ?";

                using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
                cmd.Parameters.Add(new IfxParameter { Value = accountNo });
                cmd.Parameters.Add(new IfxParameter { Value = billCycle });

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;

                fname = reader["cust_fname"].ToString()?.Trim() ?? "";
                lname = reader["cust_lname"].ToString()?.Trim() ?? "";
                addr1 = reader["address_1"].ToString()?.Trim() ?? "";
                addr2 = reader["address_2"].ToString()?.Trim() ?? "";
                addr3 = reader["address_3"].ToString()?.Trim() ?? "";
                areaCd = reader["area_code"].ToString()?.Trim() ?? "";
            }

            // ── Step 2: resolve area_name from areas table ─────────────────────
            string areaName = areaCd;   // fallback: show area code if lookup fails

            if (!string.IsNullOrWhiteSpace(areaCd))
            {
                using var con2 = new IfxConnection(_connectionString);
                await con2.OpenAsync();

                const string areaSql = @"
            SELECT FIRST 1 area_name
            FROM areas
            WHERE TRIM(area_code) = ?";

                using var areaCmd = new IfxCommand(areaSql, con2) { CommandTimeout = 5 };
                areaCmd.Parameters.Add(new IfxParameter { Value = areaCd });

                var result = await areaCmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    areaName = result.ToString()?.Trim() ?? areaCd;
            }

            // ── Step 3: build and return ───────────────────────────────────────
            return new Model1.CustomerInfo
            {
                CustomerName = (fname + " " + lname).Trim(),
                Address = string.Join(", ",
                                   new[] { addr1, addr2, addr3 }
                                   .Where(s => !string.IsNullOrEmpty(s))),
                Area = areaName,   // ← now shows "PILIYANDALA" not "21"
                AreaCode = areaCd      // ← still keeps "21" for functional use
            };
        }

        // Overload — no billCycle filter (used by 90-day cheque search)
        // Returns the most recent customer record for the account
        private async Task<Model1.ChequeCustomerInfo?> FetchChequeCustomerByAccountAsync(string accountNo)
        {
            // ── Step 1: fetch customer row from prn_dat_1 ──────────────────────
            string acctNo, fname, lname, addr1, addr2, addr3, areaCd;

            using (var con = new IfxConnection(_connectionString))
            {
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

                acctNo = reader["acct_number"].ToString()?.Trim() ?? "";
                fname = reader["cust_fname"].ToString()?.Trim() ?? "";
                lname = reader["cust_lname"].ToString()?.Trim() ?? "";
                addr1 = reader["address_1"].ToString()?.Trim() ?? "";
                addr2 = reader["address_2"].ToString()?.Trim() ?? "";
                addr3 = reader["address_3"].ToString()?.Trim() ?? "";
                areaCd = reader["area_code"].ToString()?.Trim() ?? "";
            }

            // ── Step 2: resolve area_name from areas table ─────────────────────
            string areaName = areaCd;   // fallback: show area code if lookup fails

            if (!string.IsNullOrWhiteSpace(areaCd))
            {
                using var con2 = new IfxConnection(_connectionString);
                await con2.OpenAsync();

                const string areaSql = @"
            SELECT FIRST 1 area_name
            FROM areas
            WHERE TRIM(area_code) = ?";

                using var areaCmd = new IfxCommand(areaSql, con2) { CommandTimeout = 5 };
                areaCmd.Parameters.Add(new IfxParameter { Value = areaCd });

                var result = await areaCmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    areaName = result.ToString()?.Trim() ?? areaCd;
            }

            // ── Step 3: build and return ───────────────────────────────────────
            return new Model1.ChequeCustomerInfo
            {
                AccountNo = acctNo,
                CustomerName = (fname + " " + lname).Trim(),
                Address = string.Join(", ",
                                   new[] { addr1, addr2, addr3 }
                                   .Where(s => !string.IsNullOrEmpty(s))),
                Area = areaName   // ← now shows name not code
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
        // Database: LargestCustomers (_connectionString)
        // Returns the provided notice number so the frontend can display it.
        // ──────────────────────────────────────────────────────────────────
        public async Task<string> SaveChequeDetailsAsync(Model1.SaveChequeDetailsRequest req)
        {
            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            // ── Step 1: Validate inputs ─────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(req.MyAddCode))
                throw new ArgumentException("MyAddCode is required.");

            if (string.IsNullOrWhiteSpace(req.MyCode))
                throw new ArgumentException("Notice number (MyCode) is required.");

            if (string.IsNullOrWhiteSpace(req.AcctNumber))
                throw new ArgumentException("Account number is required.");

            if (string.IsNullOrWhiteSpace(req.CheqNo))
                throw new ArgumentException("Cheque number is required.");

            if (string.IsNullOrWhiteSpace(req.RemarkCode))
                throw new ArgumentException("Remark code is required.");

            if (string.IsNullOrWhiteSpace(req.CustFname))
                throw new ArgumentException("Customer first name is required.");

            if (string.IsNullOrWhiteSpace(req.Address1))
                throw new ArgumentException("Address line 1 is required.");

            if (req.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            if (req.NoMonths < 3)
                throw new ArgumentException("No of months must be at least 3.");

            if (!string.IsNullOrWhiteSpace(req.CheqDate) &&
                !DateTime.TryParseExact(
                    req.CheqDate.Trim(),
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                throw new ArgumentException("CheqDate must be in dd/MM/yyyy format or left empty.");
            }

            // ── Step 2: Resolve my_branch from cheqmy_no ───────────────────────────
            const string branchSql = @"
        SELECT FIRST 1 my_branch
        FROM cheqmy_no
        WHERE myadd_code = ?";

            string myBranch;
            using (var branchCmd = new IfxCommand(branchSql, con) { CommandTimeout = 5 })
            {
                branchCmd.Parameters.Add(CreateParameter(IfxType.Char, req.MyAddCode.Trim().ToUpper(), 20));
                var branchResult = await branchCmd.ExecuteScalarAsync();
                myBranch = branchResult?.ToString()?.Trim() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(myBranch))
                throw new ArgumentException("Invalid MyAddCode. Branch not found.");

            // ── Step 3: Duplicate check (cheque number or notice number) ───────────
            const string dupSql = @"
        SELECT COUNT(*)
        FROM cheqmy_details
        WHERE myadd_code = ?
          AND (my_code = ? OR (my_branch = ? AND cheq_no = ?))";

            using (var dupCmd = new IfxCommand(dupSql, con) { CommandTimeout = 5 })
            {
                dupCmd.Parameters.Add(CreateParameter(IfxType.Char, req.MyAddCode.Trim().ToUpper(), 20));
                dupCmd.Parameters.Add(CreateParameter(IfxType.Char, req.MyCode.Trim(), 20));
                dupCmd.Parameters.Add(CreateParameter(IfxType.Char, myBranch, 30));
                dupCmd.Parameters.Add(CreateParameter(IfxType.Char, req.CheqNo.Trim(), 10));

                var dupResult = await dupCmd.ExecuteScalarAsync();
                if (GetScalarInt(dupResult) > 0)
                {
                    _logger.LogWarning(
                        "Duplicate cheque defaulter detected. NoticeNo {NoticeNo}, ChequeNo {ChequeNo}, MyAddCode {MyAddCode}",
                        req.MyCode.Trim(),
                        req.CheqNo.Trim(),
                        req.MyAddCode.Trim());
                    throw new InvalidOperationException(
                        $"Notice number {req.MyCode} or cheque {req.CheqNo} is already recorded.");
                }
            }

            // ── Step 4: Resolve area_name from prn_dat_1 → areas ───────────────────
            var resolvedAreaName = await ResolveAreaNameAsync(con, req.AcctNumber.Trim());
            if (string.IsNullOrWhiteSpace(resolvedAreaName))
                resolvedAreaName = req.AreaName.Trim();

            // ── Step 5: Normalize and cap values to schema limits ─────────────────
            static string Cap(string s, int max) =>
                s.Length > max ? s[..max] : s;

            var myAddCode = Cap(req.MyAddCode.Trim().ToUpper(), 20);
            var myBranchCap = Cap(myBranch, 30);
            var myCodeCap = Cap(req.MyCode.Trim(), 20);
            var acctNumber = Cap(req.AcctNumber.Trim(), 10);
            var cheqNoCap = Cap(req.CheqNo.Trim(), 10);
            var cheqDateCap = string.IsNullOrWhiteSpace(req.CheqDate) ? null : Cap(req.CheqDate.Trim(), 10);
            var remarkCap = Cap(req.RemarkCode.Trim(), 50);
            var fnameCap = Cap(req.CustFname.Trim(), 20);
            var lnameCap = Cap(req.CustLname.Trim(), 20);
            var addr1Cap = Cap(req.Address1.Trim(), 30);
            var addr2Cap = Cap(req.Address2.Trim(), 25);
            var addr3Cap = Cap(req.Address3.Trim(), 20);
            var areaCap = Cap(resolvedAreaName, 20);

            // ── Step 6: Insert ─────────────────────────────────────────────────────
            // Column order matches legacy VB.NET exactly.
            const string insertSql = @"
        INSERT INTO cheqmy_details (
            myadd_code, my_branch,    my_code,
            acct_number, cheq_no,     amount,
            cheq_date,   no_months,   entry_date,
            postage,     bank_charges, surcharge, percentage,
            remark,
            cust_fname,  cust_lname,
            address_1,   address_2,   address_3,  area_name,
            confrm,  stjrnl, stprint, stemail,
            rathmalana, japura, colcity,      headoffice,
            kiribathgoda, kandy, sabgamuwa,   nwp
        ) VALUES (
            ?,?,?, ?,?,?, ?,?,?, ?,?,?,?,
            ?, ?,?, ?,?,?,?,
            'N','N','N','N',
            '0','0','0','0','0','0','0','0'
        )";

            using var tx = con.BeginTransaction();

            try
            {
                using var ins = new IfxCommand(insertSql, con) { CommandTimeout = 10, Transaction = tx };

                ins.Parameters.Add(CreateParameter(IfxType.Char, myAddCode, 20));    // myadd_code
                ins.Parameters.Add(CreateParameter(IfxType.Char, myBranchCap, 30));  // my_branch
                ins.Parameters.Add(CreateParameter(IfxType.Char, myCodeCap, 20));    // my_code

                ins.Parameters.Add(CreateParameter(IfxType.Char, acctNumber, 10));   // acct_number
                ins.Parameters.Add(CreateParameter(IfxType.Char, cheqNoCap, 10));    // cheq_no
                ins.Parameters.Add(CreateParameter(IfxType.Decimal, req.Amount));   // amount

                // cheq_date is stored as CHAR in Informix
                // cheq_date stored as CHAR — null when user leaves it blank
                if (cheqDateCap is null)
                    ins.Parameters.Add(new IfxParameter { Value = DBNull.Value });
                else
                    ins.Parameters.Add(CreateParameter(IfxType.Char, cheqDateCap, 10));  // cheq_date
                ins.Parameters.Add(CreateParameter(IfxType.Integer, req.NoMonths)); // no_months
                ins.Parameters.Add(CreateParameter(IfxType.Date, DateTime.Today));  // entry_date

                ins.Parameters.Add(CreateParameter(IfxType.Decimal, req.Postage));      // postage
                ins.Parameters.Add(CreateParameter(IfxType.Decimal, req.BankCharges));  // bank_charges
                ins.Parameters.Add(CreateParameter(IfxType.Decimal, req.Surcharge));    // surcharge
                ins.Parameters.Add(CreateParameter(IfxType.Decimal, req.Percentage));   // percentage

                ins.Parameters.Add(CreateParameter(IfxType.Char, remarkCap, 50));   // remark
                ins.Parameters.Add(CreateParameter(IfxType.Char, fnameCap, 20));    // cust_fname
                ins.Parameters.Add(CreateParameter(IfxType.Char, lnameCap, 20));    // cust_lname

                ins.Parameters.Add(CreateParameter(IfxType.Char, addr1Cap, 30));    // address_1
                ins.Parameters.Add(CreateParameter(IfxType.Char, addr2Cap, 25));    // address_2
                ins.Parameters.Add(CreateParameter(IfxType.Char, addr3Cap, 20));    // address_3
                ins.Parameters.Add(CreateParameter(IfxType.Char, areaCap, 20));     // area_name

                // Hardcoded flags are inline in SQL — no parameters needed for them

                _logger.LogInformation(
                    "Saving cheque defaulter. NoticeNo {NoticeNo}, ChequeNo {ChequeNo}, MyAddCode {MyAddCode}",
                    myCodeCap,
                    cheqNoCap,
                    myAddCode);

                await ins.ExecuteNonQueryAsync();
                await tx.CommitAsync();

                return myCodeCap;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(
                    ex,
                    "Failed to save cheque defaulter. NoticeNo {NoticeNo}, ChequeNo {ChequeNo}, MyAddCode {MyAddCode}",
                    req.MyCode.Trim(),
                    req.CheqNo.Trim(),
                    req.MyAddCode.Trim());
                throw;
            }
        }

        private static IfxParameter CreateParameter(IfxType type, object value, int? size = null)
        {
            var param = new IfxParameter
            {
                IfxType = type,
                Value = value
            };

            if (size.HasValue)
                param.Size = size.Value;

            return param;
        }

        private static int GetScalarInt(object? value)
        {
            if (value is null || value == DBNull.Value)
                return 0;

            if (value is IfxDecimal ifxDecimal)
                return int.Parse(ifxDecimal.ToString(), CultureInfo.InvariantCulture);

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static async Task<string> ResolveAreaNameAsync(IfxConnection con, string accountNo)
        {
            const string areaCodeSql = @"
        SELECT FIRST 1 area_code
        FROM prn_dat_1
        WHERE TRIM(acct_number) = ?
        ORDER BY bill_cycle DESC";

            string areaCode = string.Empty;
            using (var areaCodeCmd = new IfxCommand(areaCodeSql, con) { CommandTimeout = 5 })
            {
                areaCodeCmd.Parameters.Add(new IfxParameter { Value = accountNo });
                var codeResult = await areaCodeCmd.ExecuteScalarAsync();
                areaCode = codeResult?.ToString()?.Trim() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(areaCode))
                return string.Empty;

            const string areaNameSql = @"
        SELECT FIRST 1 area_name
        FROM areas
        WHERE area_code = ?";

            using var areaNameCmd = new IfxCommand(areaNameSql, con) { CommandTimeout = 5 };
            areaNameCmd.Parameters.Add(new IfxParameter { Value = areaCode });
            var nameResult = await areaNameCmd.ExecuteScalarAsync();
            return nameResult?.ToString()?.Trim() ?? string.Empty;
        }

        // Resolves my_branch from cheqmy_no using the logged-in user's myadd_code
        // Database: LargestCustomers (_connectionString)
        private async Task<string> ResolveMyBranchAsync(string myAddCode)
        {
            using var con = new IfxConnection(_connectionString);
            await con.OpenAsync();

            const string sql = @"
        SELECT FIRST 1 my_branch
        FROM cheqmy_no
        WHERE myadd_code = ?";

            using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
            cmd.Parameters.Add(new IfxParameter { Value = myAddCode });

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString()?.Trim() ?? string.Empty;
        }

        // Fetches customer info from `customer` table — Heavy Supply Billing
        // Database: LargestCustomers (_connectionString)
        private async Task<Model1.CustomerInfo?> FetchHeavyCustomerByAccountAsync(
    string accountNo, int billCycle)
        {
            // ── Step 1: fetch customer row from BulkDb ─────────────────────────
            string name, addr1, addr2, addr3, areaCd;

            using (var con = new IfxConnection(_bulkDb))
            {
                await con.OpenAsync();

                const string sql = @"
            SELECT FIRST 1
                c.name,
                c.address_l1,
                c.address_l2,
                c.city,
                c.area_cd
            FROM customer c
            WHERE TRIM(c.acc_nbr) = ?";

                using var cmd = new IfxCommand(sql, con) { CommandTimeout = 5 };
                cmd.Parameters.Add(new IfxParameter { Value = accountNo });

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;

                name = reader["name"].ToString()?.Trim() ?? "";
                addr1 = reader["address_l1"].ToString()?.Trim() ?? "";
                addr2 = reader["address_l2"].ToString()?.Trim() ?? "";
                addr3 = reader["city"].ToString()?.Trim() ?? "";
                areaCd = reader["area_cd"].ToString()?.Trim() ?? "";
            }

            // ── Step 2: resolve area_name from LargestCustomers ───────────────
            // areas table lives in LargestCustomers, not BulkDb
            string areaName = areaCd;   // fallback: show area code if lookup fails

            if (!string.IsNullOrWhiteSpace(areaCd))
            {
                using var con2 = new IfxConnection(_connectionString);
                await con2.OpenAsync();

                const string areaSql = @"
            SELECT FIRST 1 area_name
            FROM areas
            WHERE TRIM(area_code) = ?";

                using var areaCmd = new IfxCommand(areaSql, con2) { CommandTimeout = 5 };
                areaCmd.Parameters.Add(new IfxParameter { Value = areaCd });

                var result = await areaCmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    areaName = result.ToString()?.Trim() ?? areaCd;
            }

            // ── Step 3: build and return ───────────────────────────────────────
            return new Model1.CustomerInfo
            {
                CustomerName = name,
                Address = string.Join(", ",
                                   new[] { addr1, addr2, addr3 }
                                   .Where(s => !string.IsNullOrEmpty(s))),
                Area = areaName,
                AreaCode = areaCd
            };
        }
    }
}