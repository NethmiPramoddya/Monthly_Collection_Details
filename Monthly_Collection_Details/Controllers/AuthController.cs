using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Monthly_Collection_Details.Services;
using Monthly_Collection_Details.Models.Auth;


namespace Monthly_Collection_Details.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;

        public AuthController(AuthService service)
        {
            _service = service;
        }

        // POST api/auth/login
        // Body: { "username": "john", "password": "pass1", "myAddCode": "WP" }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null)
                return BadRequest("Request is null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.LoginAsync(
                request.Username,
                request.Password,
                request.MyAddCode);

            // Return 401 Unauthorized if login failed
            // Return 200 OK with user details if login succeeded
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }
    }
}
