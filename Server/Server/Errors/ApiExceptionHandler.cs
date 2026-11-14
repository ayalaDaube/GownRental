using Bl.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Server.Errors
{
    /// <summary>
    /// Turns every unhandled exception into a standard "problem details" JSON response (RFC 7807).
    /// Domain exceptions carry a message meant for the user; anything else is a bug or an outage,
    /// so the client only gets a generic message and the real error goes to the log.
    /// </summary>
    public class ApiExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ApiExceptionHandler> logger;

        public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
        {
            this.logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, title, detail) = exception switch
            {
                InvalidRequestException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
                NotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                    "This record was changed by someone else a moment ago. Please reload and try again."),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected error",
                    "Something went wrong on our side. Please try again later."),
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            }
            else
            {
                logger.LogInformation("Request rejected ({Status}): {Message}", status, exception.Message);
            }

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = status, Title = title, Detail = detail },
                options: null,
                contentType: "application/problem+json",
                cancellationToken: cancellationToken);
            return true;
        }
    }
}
