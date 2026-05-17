using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Models.DTO.Request;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;

        public AuthController(AuthService authService, ILogger<AuthController> logger, IConfiguration configuration)
        {
            _authService = authService;
            _logger = logger;
            _config = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success) return Unauthorized(new { result.Error });

            SetRefreshTokenCookie(result.RefreshToken!);
            return Ok(new { result.AccessToken });
        }

        [HttpPost("register")]
        [Authorize(Roles = "ADMIN,OWNER")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success) return BadRequest(new { result.Error });

            return Ok();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken is null)
            {
                _logger.LogWarning("Refresh token is missing in the request.");
                return Unauthorized();
            }

            var result = await _authService.RefreshAsync(refreshToken);
            if (!result.Success)
            {
                _logger.LogError("Error refreshing token: {error}", result.Error);
                return Unauthorized(new { result.Error });
            }

            SetRefreshTokenCookie(result.RefreshToken!);
            return Ok(new { result.AccessToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken is not null)
            {
                await _authService.RevokeAsync(refreshToken);
                Response.Cookies.Delete("refreshToken");
                _logger.LogInformation("User logged out successfully.");
            } else
            {
                _logger.LogWarning("Logout attempted without a refresh token.");
            }

            return Ok();
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me() => Ok(new
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Login = User.FindFirstValue(ClaimTypes.Name),
            Role = User.FindFirstValue(ClaimTypes.Role),
            Name = User.FindFirstValue("name"),
            Avatar = User.FindFirstValue("avatar")
        });

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTtl"))
            });
        }
    }
}
