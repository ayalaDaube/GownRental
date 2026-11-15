using System.Net;
using System.Net.Http.Json;
using Bl.Models;

namespace Server.Tests;

/// <summary>
/// The rules for gowns, written from what the shop needs and checked at and around every limit.
/// Demo data: model 1 has sizes 36, 38 and 44 (gowns 1, 2, 21); gown 1 was rented; 15 models, 23 gowns.
/// </summary>
public class GownRulesTests
{
    private static string Text(int length) => new('a', length);

    private static object Gown(int size = 38, double price = 100, string? name = "Aurora", string? color = "Silver",
        string? description = null, string? imageUrl = null) =>
        new { name, color, description, imageUrl, size, price };

    // ---- the limits of every field: the last allowed value is accepted, the first forbidden one is refused

    public static IEnumerable<object[]> Limits() => new[]
    {
        new object[] { "size 1", Gown(size: 1), HttpStatusCode.Created },
        new object[] { "size 100", Gown(size: 100), HttpStatusCode.Created },
        new object[] { "size 0", Gown(size: 0), HttpStatusCode.BadRequest },
        new object[] { "size 101", Gown(size: 101), HttpStatusCode.BadRequest },
        new object[] { "size -1", Gown(size: -1), HttpStatusCode.BadRequest },
        new object[] { "price 1", Gown(price: 1), HttpStatusCode.Created },
        new object[] { "price 100000", Gown(price: 100000), HttpStatusCode.Created },
        new object[] { "price 0.99", Gown(price: 0.99), HttpStatusCode.BadRequest },
        new object[] { "price 100000.01", Gown(price: 100000.01), HttpStatusCode.BadRequest },
        new object[] { "price 0", Gown(price: 0), HttpStatusCode.BadRequest },
        new object[] { "price -50", Gown(price: -50), HttpStatusCode.BadRequest },
        new object[] { "name of 60 characters", Gown(name: Text(60)), HttpStatusCode.Created },
        new object[] { "name of 61 characters", Gown(name: Text(61)), HttpStatusCode.BadRequest },
        new object[] { "no name", Gown(name: null), HttpStatusCode.BadRequest },
        new object[] { "empty name", Gown(name: ""), HttpStatusCode.BadRequest },
        new object[] { "color of 30 characters", Gown(color: Text(30)), HttpStatusCode.Created },
        new object[] { "color of 31 characters", Gown(color: Text(31)), HttpStatusCode.BadRequest },
        new object[] { "no color", Gown(color: null), HttpStatusCode.BadRequest },
        new object[] { "description of 200 characters", Gown(description: Text(200)), HttpStatusCode.Created },
        new object[] { "description of 201 characters", Gown(description: Text(201)), HttpStatusCode.BadRequest },
        new object[] { "no description", Gown(description: null), HttpStatusCode.Created },
        new object[] { "image address of 500 characters", Gown(imageUrl: "https://example.com/" + Text(480)), HttpStatusCode.Created },
        new object[] { "image address of 501 characters", Gown(imageUrl: "https://example.com/" + Text(481)), HttpStatusCode.BadRequest },
    };

    [Theory]
    [MemberData(nameof(Limits))]
    public async Task A_new_gown_is_accepted_up_to_the_limit_and_refused_beyond_it(string what, object body, HttpStatusCode expected)
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var models = app.Query(db => db.GownModels.Count());

        var response = await client.PostAsJsonAsync("/api/Gowns", body);

        Assert.True(response.StatusCode == expected, $"{what}: expected {expected}, got {response.StatusCode}");
        Assert.Equal(expected == HttpStatusCode.Created ? models + 1 : models, app.Query(db => db.GownModels.Count()));
    }

    // ---- image addresses: only normal web pictures

    [Theory]
    [InlineData("http://example.com/gown.jpg")]
    [InlineData("https://example.com/gown.jpg")]
    [InlineData("https://cdn.example.com/a/b/c.png?width=800&format=webp")]
    [InlineData("HTTPS://EXAMPLE.COM/GOWN.JPG")]
    [InlineData("https://example.com:8443/gown.jpg")]
    public async Task A_web_address_is_accepted_as_a_picture(string address)
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", Gown(imageUrl: address));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("ftp://example.com/gown.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("file:///C:/Windows/win.ini")]
    [InlineData("//evil.example.com/gown.jpg")]
    [InlineData("/relative/path.jpg")]
    [InlineData("not a url")]
    [InlineData("http://")]
    [InlineData("https:///nohost.jpg")]
    [InlineData("mailto:someone@example.com")]
    public async Task Anything_that_is_not_a_normal_web_address_is_refused_as_a_picture(string address)
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", Gown(imageUrl: address));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_same_address_rules_apply_when_a_model_is_edited()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var ok = await client.PutAsJsonAsync("/api/Gowns/models/1", new { name = "Simple Gown", color = "blue", imageUrl = "https://example.com/a.jpg" });
        var bad = await client.PutAsJsonAsync("/api/Gowns/models/1", new { name = "Simple Gown", color = "blue", imageUrl = "javascript:alert(1)" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal("https://example.com/a.jpg", app.Query(db => db.GownModels.Find(1))!.ImageUrl);
    }

    [Fact]
    public async Task Editing_a_model_can_remove_its_picture_and_description()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        await client.PutAsJsonAsync("/api/Gowns/models/1", new { name = "Simple Gown", color = "blue", description = "x", imageUrl = "https://example.com/a.jpg" });

        var response = await client.PutAsJsonAsync("/api/Gowns/models/1", new { name = "Simple Gown", color = "blue", description = "", imageUrl = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = app.Query(db => db.GownModels.Find(1))!;
        Assert.Null(stored.ImageUrl);
        Assert.Null(stored.Description);
    }

    // ---- prices are exact

    [Theory]
    [InlineData(99.99)]
    [InlineData(1234.56)]
    [InlineData(0.1 + 1)]
    [InlineData(19999.95)]
    public async Task A_price_with_cents_comes_back_exactly(double price)
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var created = await client.PostAsJsonAsync("/api/Gowns", Gown(price: price));
        var gown = (await created.Content.ReadFromJsonAsync<ModelFullDetails>())!;
        var fetched = (await client.GetFromJsonAsync<ModelFullDetails>($"/api/Gowns/{gown.Id}"))!;

        Assert.Equal(price, fetched.Price);
    }

    // ---- sizes of a model

    [Fact]
    public async Task Different_models_may_have_the_same_size()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        // Models 2 and 3 both have size 36 in the demo data; add size 36 to a third model.
        var response = await client.PostAsJsonAsync("/api/Gowns", new { modelId = 4, size = 40, price = 500 });
        var again = await client.PostAsJsonAsync("/api/Gowns", new { modelId = 5, size = 40, price = 150 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task The_sizes_of_a_model_are_listed_from_the_smallest_to_the_largest()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        await client.PostAsJsonAsync("/api/Gowns", new { modelId = 1, size = 34, price = 200 });
        await client.PostAsJsonAsync("/api/Gowns", new { modelId = 1, size = 40, price = 200 });

        var models = await Api.Groups(client);

        Assert.All(models, model =>
        {
            var sizes = model.Details.Select(d => d.Size).ToList();
            Assert.Equal(sizes.OrderBy(s => s).ToList(), sizes);
        });
        Assert.Equal(new[] { 34, 36, 38, 40, 44 }, models.Single(m => m.Id == 1).Details.Select(d => d.Size).ToArray());
    }

    [Fact]
    public async Task The_models_are_listed_in_a_stable_order()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var models = await Api.Groups(client);

        Assert.Equal(models.Select(m => m.Id).OrderBy(i => i).ToList(), models.Select(m => m.Id).ToList());
    }

    // ---- a gown that was rented keeps its size

    [Fact]
    public async Task The_size_of_a_gown_that_was_rented_cannot_be_changed_but_its_price_can()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        // Gown 1 (size 36) was rented in the demo data: the rental records say size 36.
        var changeSize = await client.PutAsJsonAsync("/api/Gowns/1", new { size = 42, price = 220, isActive = true });
        var changePrice = await client.PutAsJsonAsync("/api/Gowns/1", new { size = 36, price = 260, isActive = true });

        Assert.Equal(HttpStatusCode.Conflict, changeSize.StatusCode);
        Assert.Equal(HttpStatusCode.OK, changePrice.StatusCode);
        Assert.Equal(36, app.Query(db => db.ModelDetails.Find(1))!.Size);
        Assert.Equal(260, app.Query(db => db.ModelDetails.Find(1))!.Price);
    }

    [Fact]
    public async Task The_size_of_a_gown_that_was_never_rented_can_be_changed()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/Gowns/21", new { size = 46, price = 90, isActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(46, app.Query(db => db.ModelDetails.Find(21))!.Size);
    }

    [Fact]
    public async Task What_every_rental_says_about_size_always_matches_its_gown()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var rentals = (await client.GetFromJsonAsync<List<RentalFullDetails>>("/api/Rentals"))!;

        Assert.NotEmpty(rentals);
        Assert.All(rentals, rental =>
            Assert.Equal(app.Query(db => db.ModelDetails.Find(rental.GownId))!.Size, rental.Size));
    }

    // ---- ids

    [Fact]
    public async Task Thirty_new_gowns_never_get_an_id_that_is_already_used()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var seenModels = new HashSet<int>(app.Query(db => db.GownModels.Select(m => m.Id).ToList()));
        var seenGowns = new HashSet<int>(app.Query(db => db.ModelDetails.Select(d => d.GownId).ToList()));

        for (var i = 0; i < 30; i++)
        {
            var response = await client.PostAsJsonAsync("/api/Gowns", Gown(name: "Gown " + i));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = (await response.Content.ReadFromJsonAsync<ModelFullDetails>())!;
            Assert.True(seenModels.Add(created.ModelId), $"model id {created.ModelId} was already used");
            Assert.True(seenGowns.Add(created.Id), $"gown id {created.Id} was already used");
        }
    }

    [Fact]
    public async Task After_the_newest_gown_is_deleted_a_new_one_still_gets_a_free_id()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var first = (await (await client.PostAsJsonAsync("/api/Gowns", Gown())).Content.ReadFromJsonAsync<ModelFullDetails>())!;
        await client.DeleteAsync($"/api/Gowns/{first.Id}");

        var second = await client.PostAsJsonAsync("/api/Gowns", Gown());

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var created = (await second.Content.ReadFromJsonAsync<ModelFullDetails>())!;
        Assert.Equal(1, app.Query(db => db.ModelDetails.Count(d => d.GownId == created.Id)));
    }

    [Fact]
    public async Task A_gown_that_was_deleted_is_gone_everywhere()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var created = (await (await client.PostAsJsonAsync("/api/Gowns", Gown())).Content.ReadFromJsonAsync<ModelFullDetails>())!;

        await client.DeleteAsync($"/api/Gowns/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Gowns/{created.Id}")).StatusCode);
        Assert.DoesNotContain(await Api.Groups(client), m => m.Id == created.ModelId);
        Assert.DoesNotContain((await client.GetFromJsonAsync<List<ModelFullDetails>>("/api/Gowns"))!, g => g.Id == created.Id);
    }

    // ---- what a visitor of the shop may or may not see

    [Fact]
    public async Task The_gown_list_with_a_date_marks_a_retired_gown_unavailable_on_any_date()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        await client.PutAsJsonAsync("/api/Gowns/21", new { size = 44, price = 90, isActive = false });

        foreach (var days in new[] { 0, 1, 30, 400 })
        {
            var models = await Api.Groups(client, $"/api/Gowns/byDate?date={Api.Future(days)}");
            Assert.False(models.SelectMany(m => m.Details).Single(d => d.GownId == 21).IsAvailable, $"available on day +{days}");
        }
    }

    [Fact]
    public async Task Asking_about_today_is_allowed_and_yesterday_is_not()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var today = await client.GetAsync($"/api/Gowns/byDate?date={Api.Future(0)}");
        var yesterday = await client.GetAsync($"/api/Gowns/byDate?date={Api.Future(-1)}");

        Assert.Equal(HttpStatusCode.OK, today.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, yesterday.StatusCode);
    }
}
