using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bl.Models;

namespace Server.Tests;

/// <summary>
/// Whatever a client sends, however broken or hostile, the API answers with a clear 4xx and stays healthy:
/// never a 500, never a changed database.
/// </summary>
public class InputHardeningTests
{
    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static string Future(int days = 40) => Today.AddDays(days).ToString("yyyy-MM-dd");

    // Every endpoint that reads a body: (method, url).
    public static IEnumerable<object[]> WritingEndpoints() => new[]
    {
        new object[] { "POST", "/api/Gowns" },
        new object[] { "PUT", "/api/Gowns/1" },
        new object[] { "PUT", "/api/Gowns/models/1" },
        new object[] { "POST", "/api/Orders" },
        new object[] { "POST", "/api/Rentals" },
        new object[] { "PUT", "/api/Rentals" },
        new object[] { "POST", "/api/Appointment" },
        new object[] { "POST", "/api/Auth/login" },
    };

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url, HttpContent content) =>
        await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = content });

    // ---- broken bodies

    [Theory]
    [MemberData(nameof(WritingEndpoints))]
    public async Task A_body_that_is_not_valid_JSON_is_a_400(string method, string url)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await SendAsync(client, method, url, Json("{ this is : not json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(WritingEndpoints))]
    public async Task A_JSON_null_or_an_empty_body_is_a_400(string method, string url)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var jsonNull = await SendAsync(client, method, url, Json("null"));
        var empty = await SendAsync(client, method, url, Json(""));

        Assert.Equal(HttpStatusCode.BadRequest, jsonNull.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Theory]
    [MemberData(nameof(WritingEndpoints))]
    public async Task A_body_that_is_not_declared_as_JSON_is_a_415(string method, string url)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await SendAsync(client, method, url, new StringContent("{}", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(WritingEndpoints))]
    public async Task A_JSON_array_or_a_bare_number_where_an_object_is_expected_is_a_400(string method, string url)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var array = await SendAsync(client, method, url, Json("[1,2,3]"));
        var number = await SendAsync(client, method, url, Json("42"));

        Assert.Equal(HttpStatusCode.BadRequest, array.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, number.StatusCode);
    }

    // ---- wrong types and impossible values

    [Theory]
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":21,\"eventDate\":\"2026-02-30\"}")]     // a day that does not exist
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":21,\"eventDate\":\"not a date\"}")]
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":21,\"eventDate\":20260101}")]
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":21,\"eventDate\":\"\"}")]
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":\"twenty\",\"eventDate\":\"2030-01-01\"}")]
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":99999999999,\"eventDate\":\"2030-01-01\"}")]  // does not fit an int
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":2.5,\"eventDate\":\"2030-01-01\"}")]
    [InlineData("{\"customerId\":123456,\"gownId\":21,\"eventDate\":\"2030-01-01\"}")]               // a number where text is expected
    [InlineData("{\"customerId\":\"C000001\",\"gownId\":true,\"eventDate\":\"2030-01-01\"}")]
    public async Task A_rental_with_values_of_the_wrong_type_is_a_400(string json)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = app.Query(db => db.Rentals.Count());

        var response = await client.PostAsync("/api/Rentals", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(rentals, app.Query(db => db.Rentals.Count()));
    }

    [Theory]
    [InlineData("{\"name\":\"X\",\"color\":\"Red\",\"size\":\"big\",\"price\":100}")]
    [InlineData("{\"name\":\"X\",\"color\":\"Red\",\"size\":99999999999,\"price\":100}")]
    [InlineData("{\"name\":\"X\",\"color\":\"Red\",\"size\":38,\"price\":\"cheap\"}")]
    [InlineData("{\"name\":\"X\",\"color\":\"Red\",\"size\":38,\"price\":1e999}")]       // infinity
    [InlineData("{\"name\":\"X\",\"color\":\"Red\",\"size\":38,\"price\":3000000000}")]    // larger than an int
    [InlineData("{\"name\":\"X\",\"color\":\"Red\",\"size\":38,\"price\":-1e999}")]
    [InlineData("{\"name\":[\"X\"],\"color\":\"Red\",\"size\":38,\"price\":100}")]
    [InlineData("{\"name\":{\"a\":1},\"color\":\"Red\",\"size\":38,\"price\":100}")]
    public async Task A_gown_with_values_of_the_wrong_type_is_a_400(string json)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var models = app.Query(db => db.GownModels.Count());

        var response = await client.PostAsync("/api/Gowns", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(models, app.Query(db => db.GownModels.Count()));
    }

    [Theory]
    [InlineData("{\"customerId\":\"C000001\",\"eventDate\":\"2030-01-01\",\"gownIds\":\"21\"}")]
    [InlineData("{\"customerId\":\"C000001\",\"eventDate\":\"2030-01-01\",\"gownIds\":[\"a\"]}")]
    [InlineData("{\"customerId\":\"C000001\",\"eventDate\":\"2030-01-01\",\"gownIds\":[null]}")]
    [InlineData("{\"customerId\":\"C000001\",\"eventDate\":\"2030-01-01\",\"gownIds\":null}")]
    [InlineData("{\"customerId\":\"C000001\",\"eventDate\":\"2030-01-01\"}")]
    public async Task An_order_with_a_wrong_list_of_gowns_is_a_400(string json)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = app.Query(db => db.Rentals.Count());

        var response = await client.PostAsync("/api/Orders", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(rentals, app.Query(db => db.Rentals.Count()));
    }

    [Fact]
    public async Task Negative_and_zero_gown_ids_in_an_order_are_refused_with_nothing_saved()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = app.Query(db => db.Rentals.Count());

        var response = await client.PostAsJsonAsync("/api/Orders", new { customerId = "C000001", eventDate = Future(), gownIds = new[] { 21, -5, 0 } });

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound, response.StatusCode.ToString());
        Assert.Equal(rentals, app.Query(db => db.Rentals.Count()));
    }

    // ---- URLs

    [Theory]
    [InlineData("/api/Gowns/abc")]
    [InlineData("/api/Gowns/-1")]
    [InlineData("/api/Gowns/99999999999")]
    [InlineData("/api/Gowns/1.5")]
    [InlineData("/api/Gowns/models/abc")]
    [InlineData("/api/Rentals/abc")]
    [InlineData("/api/Rentals/99999999999")]
    [InlineData("/api/Appointment/abc")]
    [InlineData("/api/Appointment/getByDate/not-a-date")]
    [InlineData("/api/Appointment/getByDate/2026-02-30")]
    [InlineData("/api/Gowns/byDate")]
    [InlineData("/api/Gowns/byDate?date=garbage")]
    [InlineData("/api/Gowns/byDate?date=2026-13-45")]
    [InlineData("/api/Gowns/%E0%A4%A")]
    public async Task A_nonsense_address_is_a_4xx_never_a_500(string url)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.GetAsync(url);

        var status = (int)response.StatusCode;
        Assert.InRange(status, 400, 499);
    }

    [Theory]
    [InlineData("DELETE", "/api/Gowns/abc")]
    [InlineData("DELETE", "/api/Rentals/-1")]
    [InlineData("DELETE", "/api/Appointment/abc")]
    [InlineData("DELETE", "/api/Gowns")]
    [InlineData("PUT", "/api/Orders")]
    [InlineData("GET", "/api/Orders")]
    [InlineData("PATCH", "/api/Gowns/1")]
    public async Task The_wrong_verb_or_a_broken_id_on_a_write_address_is_a_4xx(string method, string url)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.InRange((int)response.StatusCode, 400, 499);
    }

    // ---- hostile or unusual text

    public static IEnumerable<object[]> StrangeTexts() => new[]
    {
        new object[] { "Robert'); DROP TABLE GownModel;--" },
        new object[] { "' OR 1=1 --" },
        new object[] { "<script>alert('x')</script>" },
        new object[] { "<img src=x onerror=alert(1)>" },
        new object[] { "שמלת ערב — כחולה ✨" },                  // Hebrew and a symbol
        new object[] { "فستان سهرة" },                           // Arabic (right to left)
        new object[] { "Évening Ğown ñ ü" },                      // accents
        new object[] { "🙂👗" },                                  // emoji (outside the basic plane)
        new object[] { "line one\nline two\ttabbed" },
        new object[] { "100% \\ / \" ' ` {} [] $ ^" },
    };

    [Theory]
    [MemberData(nameof(StrangeTexts))]
    public async Task Any_text_in_a_name_is_stored_exactly_as_typed_and_read_back_exactly(string text)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var created = await client.PostAsJsonAsync("/api/Gowns", new { name = text, color = "Red", description = text, size = 38, price = 100 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var gown = (await created.Content.ReadFromJsonAsync<ModelFullDetails>())!;
        var fetched = (await client.GetFromJsonAsync<ModelFullDetails>($"/api/Gowns/{gown.Id}"))!;

        Assert.Equal(text, fetched.Name);
        Assert.Equal(text, fetched.Description);
        Assert.Equal(16, app.Query(db => db.GownModels.Count())); // the table is still there, with one more row
    }

    [Theory]
    [MemberData(nameof(StrangeTexts))]
    public async Task Any_text_in_a_customer_name_is_stored_exactly_as_typed(string text)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var name = text.Length > 60 ? text[..60] : text;

        var response = await client.PostAsJsonAsync("/api/Rentals", new
        {
            customerId = "NEWBIE123", gownId = 21, eventDate = Future(),
            customerName = name, customerPhone = "0501234567",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(name.Trim(), app.Query(db => db.Customers.Find("NEWBIE123"))!.Name);
        Assert.Equal(15 + 1, app.Query(db => db.Customers.Count()));
    }

    [Fact]
    public async Task Control_characters_never_cause_a_server_error()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", new { name = "Gown\u0000Name\u0007", color = "Red", size = 38, price = 100 });

        Assert.True((int)response.StatusCode < 500, response.StatusCode.ToString());
    }

    [Fact]
    public async Task A_name_made_only_of_spaces_is_refused_and_a_name_with_spaces_around_it_is_trimmed()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var blank = await client.PostAsJsonAsync("/api/Gowns", new { name = "     ", color = "Red", size = 38, price = 100 });
        var padded = await client.PostAsJsonAsync("/api/Gowns", new { name = "   Aurora   ", color = "  Silver ", size = 38, price = 100 });

        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.Created, padded.StatusCode);
        var created = (await padded.Content.ReadFromJsonAsync<ModelFullDetails>())!;
        Assert.Equal("Aurora", created.Name);
        Assert.Equal("Silver", created.Color);
    }

    [Fact]
    public async Task Extra_unknown_fields_in_a_body_are_ignored_and_cannot_set_hidden_values()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", new
        {
            customerId = "C000001", gownId = 21, eventDate = Future(),
            rentalId = 777, rentalDate = "1999-01-01T00:00:00", isAdmin = true, role = "Secretary", id = 5,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<RentalFullDetails>())!;
        Assert.NotEqual(777, created.RentalId);
        Assert.True(created.RentalDate.Year >= DateTime.Now.Year);
    }

    [Fact]
    public async Task A_very_long_text_is_refused_with_a_400()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var huge = new string('x', 1_000_000);

        var gown = await client.PostAsJsonAsync("/api/Gowns", new { name = huge, color = "Red", size = 38, price = 100 });
        var rental = await client.PostAsJsonAsync("/api/Rentals", new { customerId = huge, gownId = 21, eventDate = Future() });

        Assert.Equal(HttpStatusCode.BadRequest, gown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rental.StatusCode);
    }

    [Fact]
    public async Task A_deeply_nested_JSON_body_is_refused_without_crashing_the_server()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var nested = string.Concat(Enumerable.Repeat("[", 2000)) + string.Concat(Enumerable.Repeat("]", 2000));

        var response = await client.PostAsync("/api/Gowns", Json("{\"name\":" + nested + "}"));
        var after = await client.GetAsync("/api/Gowns");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    // ---- the validation answer helps the person fix the problem

    [Fact]
    public async Task A_refused_form_says_which_field_is_wrong()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Gowns", new { name = "X", color = "Red", size = 0, price = 100 });
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Size", body.GetProperty("errors").EnumerateObject().Select(p => p.Name));
    }
}
