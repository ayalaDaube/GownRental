using System.Net;
using System.Text.Json;

namespace Server.Tests;

public class GownDeletionTests
{
    // Demo data: gown 1 (model 1) was rented; gown 21 (model 1, size 44) never was.
    // Model 14 has two sizes (gowns 22 and 23) that were never rented. Model 15 has one size (gown 20).

    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    [Fact]
    public async Task Deleting_one_size_keeps_the_model_while_other_sizes_remain()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Gowns/21");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(app.Query(db => db.ModelDetails.Find(21)));
        Assert.NotNull(app.Query(db => db.GownModels.Find(1)));
    }

    [Fact]
    public async Task Deleting_the_last_size_removes_the_model_too()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Gowns/20");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(app.Query(db => db.GownModels.Find(15)));
    }

    [Fact]
    public async Task A_model_disappears_only_after_all_of_its_sizes_are_deleted()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        await client.DeleteAsync("/api/Gowns/22");
        Assert.NotNull(app.Query(db => db.GownModels.Find(14)));

        await client.DeleteAsync("/api/Gowns/23");
        Assert.Null(app.Query(db => db.GownModels.Find(14)));
    }

    [Fact]
    public async Task A_gown_with_rental_history_cannot_be_deleted()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Gowns/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(app.Query(db => db.ModelDetails.Find(1)));
        Assert.NotNull(app.Query(db => db.GownModels.Find(1)));
    }

    [Fact]
    public async Task Deleting_an_unknown_gown_is_a_clear_404()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Gowns/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        Assert.Contains("9999", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Deleting_a_model_removes_all_of_its_sizes()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Gowns/models/14");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(app.Query(db => db.GownModels.Find(14)));
        Assert.Empty(app.Query(db => db.ModelDetails.Where(d => d.ModelId == 14).ToList()));
    }

    [Fact]
    public async Task A_model_with_a_rented_size_is_not_deleted_at_all()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Gowns/models/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        // All-or-nothing: the never-rented size 21 of the same model must still be there.
        Assert.NotNull(app.Query(db => db.ModelDetails.Find(21)));
        Assert.NotNull(app.Query(db => db.GownModels.Find(1)));
    }

    [Fact]
    public async Task Deleting_an_unknown_model_is_a_404()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Gowns/models/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_requires_signing_in()
    {
        using var app = NewApp();

        var response = await app.CreateClient().DeleteAsync("/api/Gowns/21");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(app.Query(db => db.ModelDetails.Find(21)));
    }
}
