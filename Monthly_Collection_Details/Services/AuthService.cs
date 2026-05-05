using Monthly_Collection_Details.Models.Auth;
using Informix.Net.Core;
using Microsoft.Extensions.Configuration;
using Monthly_Collection_Details.Models.Auth;

namespace Monthly_Collection_Details.Services
{
    public class AuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LargestCustomers");
        }

        public async Task<LoginResponse> LoginAsync(
            string username, string password, string myAddCode)
        {
            using (var con = new IfxConnection(_connectionString))
            {
                await con.OpenAsync();

                // Join cheqmy_user with cheqmy_no to get mycode_desc for the region.
                // All three fields must match — this ensures a user from region "WP"
                // cannot log in with a "CP" code even if they guess the password.
                string sql = @"
                    SELECT
                        u.usern,
                        u.name,
                        u.designation,
                        u.level,
                        u.myadd_code,
                        n.mycode_desc
                    FROM cheqmy_user u
                    INNER JOIN cheqmy_no n ON n.myadd_code = u.myadd_code
                    WHERE u.usern      = ?
                      AND u.passwrd   = ?
                      AND u.myadd_code = ?";

                using (var cmd = new IfxCommand(sql, con))
                {
                    // Parameters in the same order as the ? placeholders
                    cmd.Parameters.Add(new IfxParameter { Value = username.Trim() });
                    cmd.Parameters.Add(new IfxParameter { Value = password.Trim() });
                    cmd.Parameters.Add(new IfxParameter { Value = myAddCode.Trim().ToUpper() });

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        // Record found — credentials are valid and region matches
                        return new LoginResponse
                        {
                            Success = true,
                            Message = "Login successful.",
                            Username = reader["usern"].ToString().Trim(),
                            Name = reader["name"].ToString().Trim(),
                            Designation = reader["designation"].ToString().Trim(),
                            Level = reader["level"].ToString().Trim(),
                            MyAddCode = reader["myadd_code"].ToString().Trim(),
                            MyCodeDesc = reader["mycode_desc"].ToString().Trim()
                        };
                    }
                    else
                    {
                        // No record found — wrong credentials or wrong region
                        return new LoginResponse
                        {
                            Success = false,
                            Message = "Invalid username, password, or region code."
                        };
                    }
                }
            }
        }
    }
}




