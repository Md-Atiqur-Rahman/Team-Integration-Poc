using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Tests;

public sealed class TeamsGraphExceptionHandlerTests
{
    [Fact]
    public async Task HandleAsync_includes_reauthentication_required_code_for_401()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new TeamsGraphException(
                StatusCodes.Status401Unauthorized,
                "Microsoft Teams access needs reconnecting.")
        });

        await TeamsGraphExceptionHandler.HandleAsync(context);

        var document = await ReadResponseBodyAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal(
            TeamsGraphException.ReauthenticationRequiredCode,
            document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task HandleAsync_does_not_include_a_code_for_non_401_graph_failures()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new TeamsGraphException(
                StatusCodes.Status404NotFound,
                "The selected Team is no longer available or accessible.")
        });

        await TeamsGraphExceptionHandler.HandleAsync(context);

        var document = await ReadResponseBodyAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(document.RootElement.TryGetProperty("code", out _));
    }

    private static async Task<JsonDocument> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return JsonDocument.Parse(await reader.ReadToEndAsync());
    }
}
