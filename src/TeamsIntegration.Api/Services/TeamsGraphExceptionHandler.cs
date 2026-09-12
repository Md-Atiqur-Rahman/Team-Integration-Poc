using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsGraphException(int statusCode, string detail) : Exception(detail)
{
    public const string ReauthenticationRequiredCode = "reauthentication_required";
    public const string StaleConfigurationCode = "stale_configuration";

    public int StatusCode { get; } = statusCode;

    public string? RetryAfter { get; init; }
}

public static class TeamsGraphExceptionHandler
{
    public static Task HandleAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is not TeamsGraphException graphException)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            });
        }

        context.Response.StatusCode = graphException.StatusCode;
        if (graphException.StatusCode == StatusCodes.Status429TooManyRequests
            && !string.IsNullOrWhiteSpace(graphException.RetryAfter))
        {
            context.Response.Headers.RetryAfter = graphException.RetryAfter;
        }

        var problemDetails = new ProblemDetails
        {
            Status = graphException.StatusCode,
            Title = graphException.StatusCode == StatusCodes.Status401Unauthorized
                ? "Authentication required"
                : "Microsoft Graph request failed",
            Detail = graphException.Message
        };

        if (graphException.StatusCode == StatusCodes.Status401Unauthorized)
        {
            problemDetails.Extensions["code"] = TeamsGraphException.ReauthenticationRequiredCode;
        }
        else if (graphException.StatusCode == StatusCodes.Status409Conflict)
        {
            problemDetails.Extensions["code"] = TeamsGraphException.StaleConfigurationCode;
        }

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
