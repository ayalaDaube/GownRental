using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bl.Models;

namespace Server.Tests;

public class GownEditingTests
{
    // Demo data: 15 models (ids 1-15) and 23 sizes (gown ids 1-23). Model 1 has sizes 36, 38 and 44.

    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    private static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        return problem.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
    }

    // ---- creating

    [Fact]
    public async Task A_new_model_gets_ids_from_the_server_even_if_the_caller_sends_others()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", new
        {
            id = 500, // ignored: the server decides the ids
            name = "Aurora Gown", color = "Silver", description = "Shimmering evening gown", size = 38, price = 320,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ModelFullDetails>())!;
        Assert.Equal(16, created.ModelId);
        Assert.Equal(24, created.Id);
        Assert.Equal("Aurora Gown", created.Name);
        Assert.True(created.IsActive);
        Assert.EndsWith("/api/Gowns/24", response.Headers.Location!.ToString());
        Assert.NotNull(app.Query(db => db.GownModels.Find(16)));
    }

    [Fact]
    public async Task Two_gowns_never_share_an_id()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var body = new { name = "Twin", color = "Red", size = 40, price = 100 };

        var first = (await (await client.PostAsJsonAsync("/api/Gowns", body)).Content.ReadFromJsonAsync<ModelFullDetails>())!;
        var second = (await (await client.PostAsJsonAsync("/api/Gowns", body)).Content.ReadFromJsonAsync<ModelFullDetails>())!;

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.ModelId, second.ModelId);
    }

    [Fact]
    public async Task A_size_can_be_added_to_an_existing_model_without_changing_the_model()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", new
        {
            modelId = 1, name = "Should be ignored", color = "Ignored", size = 40, price = 260,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ModelFullDetails>())!;
        Assert.Equal(1, created.ModelId);
        Assert.Equal("Simple Gown", created.Name);
        Assert.Equal(40, created.Size);
        Assert.Equal(4, app.Query(db => db.ModelDetails.Count(d => d.ModelId == 1)));
    }

    [Fact]
    public async Task Adding_a_size_the_model_already_has_is_a_conflict()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", new { modelId = 1, size = 36, price = 250 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("size 36", await DetailAsync(response));
    }

    [Fact]
    public async Task Adding_a_size_to_an_unknown_model_is_a_404()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", new { modelId = 9999, size = 36, price = 250 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("{\"color\":\"Red\",\"size\":38,\"price\":100}", "name")]
    [InlineData("{\"name\":\"X\",\"size\":38,\"price\":100}", "color")]
    public async Task A_new_model_needs_a_name_and_a_color(string json, string missing)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsync("/api/Gowns", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(missing, (await DetailAsync(response))!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(15, app.Query(db => db.GownModels.Count()));
    }

    [Theory]
    [InlineData(0, 100, null)]
    [InlineData(38, 0, null)]
    [InlineData(38, -5, null)]
    [InlineData(500, 100, null)]
    [InlineData(38, 100, "javascript:alert(1)")]
    [InlineData(38, 100, "not a url")]
    public async Task Invalid_values_are_rejected_with_a_message(int size, double price, string? imageUrl)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", new { name = "X", color = "Red", size, price, imageUrl });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        Assert.True(body.TryGetProperty("errors", out _));
        Assert.Equal(15, app.Query(db => db.GownModels.Count()));
    }

    [Fact]
    public async Task Creating_requires_signing_in()
    {
        using var app = NewApp();

        var response = await app.CreateClient().PostAsJsonAsync("/api/Gowns", new { name = "X", color = "Red", size = 38, price = 100 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- editing one size

    [Fact]
    public async Task A_size_can_be_edited_and_the_change_is_stored()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/Gowns/1", new { size = 37, price = 275, isActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = app.Query(db => db.ModelDetails.Find(1))!;
        Assert.Equal(37, stored.Size);
        Assert.Equal(275, stored.Price);
        Assert.Equal(1, stored.ModelId);
    }

    [Fact]
    public async Task Editing_a_size_to_one_the_model_already_has_is_a_conflict()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        // Gown 1 is size 36 and gown 2 is size 38, both of model 1.
        var response = await client.PutAsJsonAsync("/api/Gowns/1", new { size = 38, price = 220, isActive = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(36, app.Query(db => db.ModelDetails.Find(1))!.Size);
    }

    [Fact]
    public async Task Keeping_the_same_size_number_while_changing_the_price_is_fine()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/Gowns/1", new { size = 36, price = 999, isActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Editing_an_unknown_size_is_a_404_and_invalid_values_are_a_400()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var unknown = await client.PutAsJsonAsync("/api/Gowns/9999", new { size = 38, price = 100, isActive = true });
        var invalid = await client.PutAsJsonAsync("/api/Gowns/1", new { size = 38, price = -1, isActive = true });

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    // ---- editing a model

    [Fact]
    public async Task A_model_can_be_edited_and_the_change_is_stored_for_all_sizes()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/Gowns/models/1", new
        {
            name = "Simple Gown Deluxe", color = "sky blue", description = "Now with a belt",
            imageUrl = "https://example.com/gown.jpg",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = app.Query(db => db.GownModels.Find(1))!;
        Assert.Equal("Simple Gown Deluxe", stored.Name);
        Assert.Equal("https://example.com/gown.jpg", stored.ImageUrl);

        var groups = (await client.GetFromJsonAsync<List<ModelGrouped>>("/api/Gowns/groups"))!;
        Assert.Equal("Simple Gown Deluxe", groups.Single(m => m.Id == 1).Name);
        Assert.Equal("https://example.com/gown.jpg", groups.Single(m => m.Id == 1).Image);
    }

    [Fact]
    public async Task Editing_an_unknown_model_is_a_404_and_an_empty_name_is_a_400()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var unknown = await client.PutAsJsonAsync("/api/Gowns/models/9999", new { name = "X", color = "Red" });
        var noName = await client.PutAsJsonAsync("/api/Gowns/models/1", new { name = "", color = "Red" });

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noName.StatusCode);
        Assert.Equal("Simple Gown", app.Query(db => db.GownModels.Find(1))!.Name);
    }
}
