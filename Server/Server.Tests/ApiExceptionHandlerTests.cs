using System.Text.Json;
using Bl.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Errors;

namespace Server.Tests;

public class ApiExceptionHandlerTests
{
    private static async Task<(int Status, JsonElement Body, string? ContentType)> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        return (context.Response.StatusCode, body, context.Response.ContentType);
    }

    [Theory]
    [InlineData(typeof(InvalidRequestException), 400)]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    public async Task Domain_exceptions_become_the_matching_status_with_their_own_message(Type type, int expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(type, "The gown is already rented.")!;

        var (status, body, contentType) = await HandleAsync(exception);

        Assert.Equal(expectedStatus, status);
        Assert.Equal("The gown is already rented.", body.GetProperty("detail").GetString());
        Assert.Equal("application/problem+json", contentType);
    }

    [Fact]
    public async Task A_concurrent_edit_is_reported_as_a_conflict()
    {
        var (status, body, _) = await HandleAsync(new DbUpdateConcurrencyException("row version mismatch"));

        Assert.Equal(409, status);
        Assert.DoesNotContain("row version", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unexpected_errors_never_leak_their_details()
    {
        var (status, body, _) = await HandleAsync(
            new InvalidOperationException("Login failed for user 'sa' on server DB01 with password hunter2"));

        Assert.Equal(500, status);
        var text = body.GetRawText();
        Assert.DoesNotContain("hunter2", text);
        Assert.DoesNotContain("DB01", text);
        Assert.Equal(500, body.GetProperty("status").GetInt32());
    }
}
