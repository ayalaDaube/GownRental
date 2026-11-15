using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Server.Tests;

/// <summary>
/// The security promises of the API, written as rules and checked against everything the API exposes,
/// not against a hand-picked list of endpoints.
/// </summary>
public class SecurityTests : IClassFixture<ApiFactory>
{
    // The ONLY endpoints a visitor may call without signing in. Adding a public endpoint means
    // changing this list on purpose.
    private static readonly string[] PublicEndpoints =
    {
        "GET api/Appointment/getAvailableAppointments",
        "POST api/Appointment",
        "POST api/Auth/login",
    };

    private readonly ApiFactory factory;

    public SecurityTests(ApiFactory factory) => this.factory = factory;

    private record Route(string Method, string Pattern, string Url, bool IsPublic)
    {
        public string Key => $"{Method} {Pattern}";
    }

    private IReadOnlyList<Route> AllRoutes()
    {
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        var routes = new List<Route>();

        foreach (var endpoint in source.Endpoints.OfType<RouteEndpoint>())
        {
            if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() == null) continue; // only our controllers

            var pattern = endpoint.RoutePattern.RawText!;
            var url = "/" + Regex.Replace(pattern, @"\{(\w+)(:\w+)?\}", m => m.Groups[1].Value switch
            {
                "date" => "2030-01-01",
                _ => "1",
            });
            var isPublic = endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null;
            var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods;
            routes.AddRange(methods.Select(method => new Route(method, pattern, url, isPublic)));
        }

        return routes;
    }

    private static HttpRequestMessage Request(Route route, string? token = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(route.Method), route.Url);
        if (route.Method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }
        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    [Fact]
    public void The_API_exposes_only_the_endpoints_we_expect_to_be_public()
    {
        var actualPublic = AllRoutes().Where(r => r.IsPublic).Select(r => r.Key).OrderBy(k => k).ToArray();

        Assert.Equal(PublicEndpoints.OrderBy(k => k).ToArray(), actualPublic);
    }

    [Fact]
    public void The_route_discovery_actually_finds_the_API()
    {
        // Guards the guard: if discovery silently found nothing, the security tests below would pass for no reason.
        var routes = AllRoutes();

        Assert.True(routes.Count >= 25, $"only {routes.Count} routes were discovered");
        Assert.Contains(routes, r => r.Key == "DELETE api/Gowns/{id:int}");
        Assert.Contains(routes, r => r.Key == "POST api/Orders");
    }

    [Fact]
    public async Task Every_endpoint_that_is_not_public_rejects_a_request_without_a_token()
    {
        var client = factory.CreateClient();
        var failures = new List<string>();

        foreach (var route in AllRoutes().Where(r => !r.IsPublic))
        {
            var response = await client.SendAsync(Request(route));
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                failures.Add($"{route.Key} answered {(int)response.StatusCode} without a token");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public async Task Every_endpoint_that_is_not_public_rejects_a_signed_in_user_who_is_not_a_secretary()
    {
        var client = factory.CreateClient();
        var token = TestTokens.Create(role: "Customer");
        var failures = new List<string>();

        foreach (var route in AllRoutes().Where(r => !r.IsPublic))
        {
            var response = await client.SendAsync(Request(route, token));
            if (response.StatusCode != HttpStatusCode.Forbidden)
            {
                failures.Add($"{route.Key} answered {(int)response.StatusCode} to a Customer");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public async Task The_public_endpoints_do_not_demand_a_token()
    {
        var client = factory.CreateClient();

        foreach (var route in AllRoutes().Where(r => r.IsPublic))
        {
            var response = await client.SendAsync(Request(route));

            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    // ---- tokens that must never be accepted

    public static IEnumerable<object[]> BadTokens() => new[]
    {
        new object[] { "signed with another key", TestTokens.Create(key: "another-signing-key-another-signing-key-0123456789") },
        new object[] { "wrong issuer", TestTokens.Create(issuer: "SomeoneElse") },
        new object[] { "wrong audience", TestTokens.Create(audience: "SomeoneElse.Client") },
        new object[] { "expired", TestTokens.Create(expiresUtc: DateTime.UtcNow.AddMinutes(-10), notBeforeUtc: DateTime.UtcNow.AddHours(-1)) },
        new object[] { "not valid yet", TestTokens.Create(notBeforeUtc: DateTime.UtcNow.AddHours(1), expiresUtc: DateTime.UtcNow.AddHours(2)) },
        new object[] { "no expiry at all", TestTokens.Create(includeExpiry: false) },
        new object[] { "not signed (alg none)", TestTokens.Unsigned() },
        new object[] { "garbage", "this.is.not-a-token" },
        new object[] { "empty-looking", "..." },
    };

    [Theory]
    [MemberData(nameof(BadTokens))]
    public async Task A_bad_token_is_rejected(string why, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Gowns");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await factory.CreateClient().SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"a token that is {why} was answered {(int)response.StatusCode}");
    }

    [Fact]
    public async Task A_token_in_the_wrong_scheme_is_not_a_login()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Gowns");
        request.Headers.TryAddWithoutValidation("Authorization", "Basic " + TestTokens.Create());

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_sent_in_the_URL_instead_of_the_header_is_ignored()
    {
        var response = await factory.CreateClient().GetAsync("/api/Gowns?access_token=" + TestTokens.Create());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Changing_the_role_inside_a_valid_token_breaks_the_signature()
    {
        var token = TestTokens.Create(role: "Customer");
        var parts = token.Split('.');
        var payload = System.Text.Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1]))
            .Replace("\"Customer\"", "\"Secretary\"");
        var forged = $"{parts[0]}.{Base64UrlEncoder.Encode(payload)}.{parts[2]}";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Gowns");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", forged);
        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- what the API gives away

    [Fact]
    public async Task The_login_answer_and_the_me_answer_never_contain_a_password_hash()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Auth/login",
            new { username = ApiFactory.SecretaryUsername, password = ApiFactory.SecretaryPassword });
        var loginText = await login.Content.ReadAsStringAsync();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(System.Text.Json.JsonDocument.Parse(loginText).RootElement.GetProperty("token").GetString());
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.RawData);
        var meText = await (await client.SendAsync(meRequest)).Content.ReadAsStringAsync();

        foreach (var text in new[] { loginText, meText })
        {
            Assert.DoesNotContain("passwordHash", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AQAAAA", text); // how ASP.NET password hashes start
        }
        Assert.DoesNotContain(ApiFactory.SecretaryPassword, token.RawData);
        Assert.DoesNotContain(token.Claims, c => c.Value.Contains(ApiFactory.SecretaryPassword));
    }

    [Fact]
    public async Task Error_answers_never_show_stack_traces_or_exception_names()
    {
        var client = await factory.SignedInClientAsync();

        var texts = new[]
        {
            await (await client.GetAsync("/api/Rentals/999999")).Content.ReadAsStringAsync(),
            await (await client.DeleteAsync("/api/Gowns/999999")).Content.ReadAsStringAsync(),
            await (await client.PostAsync("/api/Gowns", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync(),
        };

        foreach (var text in texts)
        {
            Assert.DoesNotContain("   at ", text);
            Assert.DoesNotContain("Exception", text);
            Assert.DoesNotContain("System.", text);
            Assert.DoesNotContain("Microsoft.", text);
        }
    }

    // ---- browsers

    [Fact]
    public async Task Only_the_configured_web_app_may_read_answers_from_other_origins()
    {
        var client = factory.CreateClient();

        HttpRequestMessage Preflight(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/Gowns");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "authorization");
            return request;
        }

        var allowed = await client.SendAsync(Preflight("http://localhost:3000"));
        var stranger = await client.SendAsync(Preflight("https://evil.example.com"));

        Assert.Equal("http://localhost:3000", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(stranger.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
