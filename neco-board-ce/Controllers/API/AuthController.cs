using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request.Auth;
using neco_board_ce.Models.DTO.Response.Auth;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Services.Logs;
using System.Security.Claims;

namespace neco_board_ce.Controllers.API
{
    /// <summary>
    /// Handles user authentication: login, registration, token refresh, logout, and current-user info.
    /// </summary>
    /// <remarks>
    /// Authentication is token-based. A short-lived JWT access token is returned in the JSON response body,
    /// while a long-lived refresh token is stored in an <c>HttpOnly</c> cookie named <c>refreshToken</c>.
    /// Cookie expiry is driven by the <c>Jwt:RefreshTtl</c> configuration value (in days).
    /// Registration is restricted to users holding the <c>ADMIN</c> or <c>OWNER</c> role.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Authentication")]
    [EnableRateLimiting("AuthPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AuditService _auditService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public AuthController(AuthService authService, AuditService auditService, ILogger<AuthController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _authService = authService;
            _auditService = auditService;
            _logger = logger;
            _config = configuration;
            _env = env;
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <remarks>
        /// On success, the response body contains a <see cref="RefreshResponse"/> with the access token,
        /// and the <c>refreshToken</c> <c>HttpOnly</c> cookie is set in the response.
        /// </remarks>
        /// <param name="dto">Request body containing the user's credentials (login and password).</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> with <see cref="RefreshResponse"/> on success;
        /// <see cref="UnauthorizedObjectResult"/> with <see cref="ErrorMessageResponse"/> when credentials are invalid.
        /// </returns>
        /// <response code="200">Authentication successful. Response body contains the JWT access token. The refresh token cookie is set.</response>
        /// <response code="401">Invalid credentials. Response body contains the error description.</response>
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

        /// <summary>
        /// Register
        /// </summary>
        /// <remarks>
        /// The <c>[Authorize(Roles = "ADMIN,OWNER")]</c> attribute causes the framework to
        /// return <c>401</c> for unauthenticated requests and <c>403</c> for authenticated
        /// requests that lack the required role — before the action body is reached.
        /// </remarks>
        /// <param name="dto">Request body containing the new user's registration details.</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="BadRequestObjectResult"/> with <see cref="ErrorMessageResponse"/> when the service rejects the request;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks the ADMIN or OWNER role.
        /// </returns>
        /// <response code="200">User registered successfully.</response>
        /// <response code="400">Registration failed. Response body contains the error description.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The authenticated caller does not hold the ADMIN or OWNER role.</response>
        [HttpPost("register", Name = "Register")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success) return BadRequest(new ErrorMessageResponse { Message = result.Error ?? "unknown error" });

            return Ok();
        }

        /// <summary>
        /// Refresh
        /// </summary>
        /// <remarks>
        /// Issues a new JWT access token using the refresh token stored in the request cookie.
        /// Reads the <c>refreshToken</c> value from the <c>HttpOnly</c> request cookie.
        /// On success, returns a new access token in the body and rotates the <c>refreshToken</c>
        /// cookie with a fresh value and expiry.
        /// <c>401</c> is returned in two distinct cases:
        /// <list type="bullet">
        ///   <item><description>The <c>refreshToken</c> cookie is absent — response body is empty.</description></item>
        ///   <item><description>The service rejects the token (expired, revoked, or malformed) — response body contains <see cref="ErrorMessageResponse"/>.</description></item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// <see cref="OkObjectResult"/> with <see cref="RefreshResponse"/> on success;
        /// <see cref="UnauthorizedResult"/> when the cookie is absent;
        /// <see cref="UnauthorizedObjectResult"/> with <see cref="ErrorMessageResponse"/> when the token is invalid.
        /// </returns>
        /// <response code="200">Token refreshed. Response body contains the new JWT access token. The refresh token cookie is rotated.</response>
        /// <response code="401">Refresh token cookie is missing, expired, revoked, or otherwise invalid.</response>
        [HttpPost("refresh", Name = "Refresh")]
        [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies[Constants.Auth.RefreshTokenCookie];
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

        /// <summary>
        /// Logout
        /// </summary>
        /// <remarks>
        /// Revokes the current refresh token and clears the authentication cookie.
        /// Always returns <c>200 OK</c> regardless of whether the <c>refreshToken</c> cookie is present.
        /// If the cookie exists, the token is revoked via the auth service and the cookie is deleted.
        /// If the cookie is absent, the request is logged as a warning and <c>200</c> is returned
        /// without further action. This behaviour prevents session-state enumeration through the logout endpoint.
        /// </remarks>
        /// <returns>
        /// <see cref="OkResult"/> always.
        /// </returns>
        /// <response code="200">Logout processed. If a refresh token was present, it has been revoked and the cookie cleared.</response>
        [HttpPost("logout", Name = "Logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[Constants.Auth.RefreshTokenCookie];
            if (refreshToken is not null)
            {
                await _authService.RevokeAsync(refreshToken);
                Response.Cookies.Delete(Constants.Auth.RefreshTokenCookie);
                _logger.LogInformation("User logged out successfully.");
            } else
            {
                _logger.LogWarning("Logout attempted without a refresh token.");
            }

            return Ok();
        }

        /// <summary>
        /// Get Me Info
        /// </summary>
        /// <remarks>
        /// Returns the profile of the currently authenticated user, extracted from JWT claims.
        /// Reads the following claims from the validated JWT and maps them to <see cref="MeResponse"/>:
        /// <list type="bullet">
        ///   <item><description><c>Id</c> — <see cref="ClaimTypes.NameIdentifier"/></description></item>
        ///   <item><description><c>Login</c> — <see cref="ClaimTypes.Name"/></description></item>
        ///   <item><description><c>Role</c> — <see cref="ClaimTypes.Role"/></description></item>
        ///   <item><description><c>Name</c> — custom claim <c>"name"</c></description></item>
        ///   <item><description><c>Avatar</c> — custom claim <c>"avatar"</c> (nullable)</description></item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// <see cref="OkObjectResult"/> with <see cref="MeResponse"/> on success;
        /// <see cref="UnauthorizedResult"/> when the request carries no valid JWT.
        /// </returns>
        /// <response code="200">Returns the authenticated user's profile from JWT claims.</response>
        /// <response code="401">The request does not contain a valid JWT access token.</response>
        [HttpGet("me", Name = "GetMeInfo")]
        [Authorize]
        [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Me() => Ok(new MeResponse
        {
            Id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Login = User.FindFirstValue(ClaimTypes.Name)!,
            Role = User.FindFirstValue(ClaimTypes.Role)!,
            Name = User.FindFirstValue(Constants.Auth.ClaimName)!,
            Avatar = User.FindFirstValue(Constants.Auth.ClaimAvatar)
        });

        private void SetRefreshTokenCookie(string token)
        {
            // In production the SPA is served from another origin over HTTPS, so the cookie must be
            // Secure + SameSite=None to be sent on cross-site requests. In development (http://localhost)
            // Secure would drop the cookie, so fall back to an insecure Lax cookie there.
            var isDev = _env.IsDevelopment();
            Response.Cookies.Append(Constants.Auth.RefreshTokenCookie, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev,
                SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTtl"))
            });
        }
    }
}
