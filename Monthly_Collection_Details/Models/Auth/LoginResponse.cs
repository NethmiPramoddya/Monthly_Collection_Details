namespace Monthly_Collection_Details.Models.Auth
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public string MyAddCode { get; set; }   // myadd_code
        public string MyCodeDesc { get; set; }   // mycode_desc from cheqmy_no
        public string Name { get; set; }   // user's full name
        public string Designation { get; set; }   // user's designation
        public string Level { get; set; }   // user's access level
    }
}


