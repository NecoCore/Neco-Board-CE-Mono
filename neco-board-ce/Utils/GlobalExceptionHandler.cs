using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace neco_board_ce.Utils
{
    /// <summary>
    /// Catches unhandled exceptions and returns a standard RFC 7807 <see cref="ProblemDetails"/>
    /// response instead of a raw 500. Known exception types are mapped to meaningful status codes.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IProblemDetailsService problemDetailsService,
            IHostEnvironment env)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

            var (status, title) = exception switch
            {
                FileNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            httpContext.Response.StatusCode = status;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    // Leak details only in development; production gets a generic message.
                    Detail = _env.IsDevelopment() ? exception.Message : null
                }
            });
        }
    }
}
