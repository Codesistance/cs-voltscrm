using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VoltsCRM.Application.Common.Exceptions;

namespace VoltsCRM.API.Setup;

/// <summary>
/// Translates known exceptions into RFC7807 responses. FluentValidation failures become a
/// <see cref="ValidationProblemDetails"/> (<c>errors</c> keyed by property) so the frontend can map
/// server-side field errors back onto the form.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validation:
                {
                    var errors = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    await WriteAsync(httpContext, new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "One or more validation errors occurred.",
                    }, cancellationToken);
                    return true;
                }

            case NotFoundException notFound:
                {
                    await WriteAsync(httpContext, new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Resource not found.",
                        Detail = notFound.Message,
                    }, cancellationToken);
                    return true;
                }

            default:
                {
                    logger.LogError(exception, "Unhandled exception");
                    await WriteAsync(httpContext, new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "An unexpected error occurred.",
                    }, cancellationToken);
                    return true;
                }
        }
    }

    private static async Task WriteAsync(HttpContext ctx, ProblemDetails problem, CancellationToken ct)
    {
        ctx.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        // Serialize as object so the runtime type (e.g. ValidationProblemDetails.Errors) is included.
        await ctx.Response.WriteAsJsonAsync<object>(problem, ct);
    }
}
