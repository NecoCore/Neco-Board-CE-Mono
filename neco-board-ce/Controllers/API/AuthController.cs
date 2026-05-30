using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.DTO.Response.Auth;
using neco_board_ce.Models.DTO.Response.Massages;
using neco_board_ce.Services.Authentication;
using System.Security.Claims;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Authification")]
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

        [HttpPost("login", Name = "Login")]
        [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success) return Unauthorized(new ErrorMessageResponse { Message = result.Error ?? "unknown error" });

            SetRefreshTokenCookie(result.RefreshToken!);
            return Ok(new RefreshResponse { AccessToken = result.AccessToken! });
        }

        [HttpPost("register", Name = "Register")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success) return BadRequest(new ErrorMessageResponse { Message = result.Error ?? "unknown error" });

            return Ok();
        }

        [HttpPost("refresh", Name = "Refresh")]
        [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status401Unauthorized)]
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
                return Unauthorized(new ErrorMessageResponse { Message = result.Error ?? "unknown error" });
            }

            SetRefreshTokenCookie(result.RefreshToken!);
            return Ok(new RefreshResponse { AccessToken = result.AccessToken! });
        }

        [HttpPost("logout", Name = "Logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        [HttpGet("me", Name = "GetMeInfo")]
        [Authorize]
        [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Me() => Ok(new MeResponse
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Login = User.FindFirstValue(ClaimTypes.Name)!,
            Role = User.FindFirstValue(ClaimTypes.Role)!,
            Name = User.FindFirstValue("name")!,
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
