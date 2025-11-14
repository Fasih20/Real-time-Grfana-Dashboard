using Microsoft.AspNetCore.Mvc;
using Suparco.Api.Models;
using Suparco.Api.Services;
using Suparco.Api.Helpers;

namespace Suparco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtTokenHelper _tokenHelper;

        public AuthController(UserService userService, JwtTokenHelper tokenHelper)
        {
            _userService = userService;
            _tokenHelper = tokenHelper;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserModel login)
        {
            var user = _userService.ValidateUser(login.Username, login.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password" });

            var token = _tokenHelper.GenerateToken(user);
            return Ok(new { token, user.Role });
        }
    }
}
