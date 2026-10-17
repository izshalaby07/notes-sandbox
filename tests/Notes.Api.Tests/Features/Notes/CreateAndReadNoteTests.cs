using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notes.Api.Tests.Support;

namespace Notes.Api.Tests.Features.Notes;

public class CreateAndReadNoteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private const string ArabicTitle = "ملاحظة أولى";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateAndReadNoteTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.Clone();
    }

    private static async Task AssertProblemDetails(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await ReadJson(response);
        Assert.Equal((int)status, json.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Post_Notes_Returns201WithNoteAndLocation()
    {
        var response = await _client.PostAsJsonAsync("/notes", new { title = "Groceries", body = "milk" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await ReadJson(response);
        var id = note.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Groceries", note.GetProperty("title").GetString());
        Assert.Equal("milk", note.GetProperty("body").GetString());
        Assert.Equal($"/notes/{id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_LocationOfCreatedNote_ReturnsSameNote()
    {
        var created = await _client.PostAsJsonAsync("/notes", new { title = "Groceries", body = "milk" });
        var createdJson = await ReadJson(created);

        var response = await _client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await ReadJson(response);
        Assert.Equal(createdJson.ToString(), fetched.ToString());
    }

    [Fact]
    public async Task Post_ClientSentServerFields_AreIgnored()
    {
        var time = new StubTimeProvider(FixedNow);
        using var factory = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<TimeProvider>(time)));
        var client = factory.CreateClient();
        var clientId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/notes", new
        {
            id = clientId,
            title = "t",
            body = "b",
            createdAt = "2001-01-01T00:00:00Z",
            updatedAt = "2002-02-02T00:00:00Z",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await ReadJson(response);
        Assert.NotEqual(clientId, note.GetProperty("id").GetGuid());
        Assert.Equal(FixedNow, note.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(FixedNow, note.GetProperty("updatedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Post_WithStubTime_SetsCreatedAtAndUpdatedAtEqualToNow()
    {
        var time = new StubTimeProvider(FixedNow);
        using var factory = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<TimeProvider>(time)));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/notes", new { title = "t" });

        var note = await ReadJson(response);
        Assert.Equal(FixedNow, note.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(FixedNow, note.GetProperty("updatedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Post_WithoutBody_ReturnsEmptyStringBody()
    {
        var response = await _client.PostAsJsonAsync("/notes", new { title = "t" });

        var note = await ReadJson(response);
        Assert.Equal(JsonValueKind.String, note.GetProperty("body").ValueKind);
        Assert.Equal("", note.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Post_WithNullBody_ReturnsEmptyStringBody()
    {
        var response = await _client.PostAsJsonAsync("/notes", new { title = "t", body = (string?)null });

        var note = await ReadJson(response);
        Assert.Equal("", note.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Post_ArabicTitleWithSpaces_ReadsBackIdentical()
    {
        var title = "  " + ArabicTitle + "  ";
        var body = "نص الملاحظة ";
        var created = await _client.PostAsJsonAsync("/notes", new { title, body });

        var response = await _client.GetAsync(created.Headers.Location);

        var note = await ReadJson(response);
        Assert.Equal(title, note.GetProperty("title").GetString());
        Assert.Equal(body, note.GetProperty("body").GetString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"title\":null}")]
    [InlineData("{\"title\":\"\"}")]
    [InlineData("{\"title\":\"   \"}")]
    public async Task Post_MissingEmptyOrWhitespaceTitle_Returns400NamingTitle(string json)
    {
        var response = await _client.PostAsync("/notes", new StringContent(json, Encoding.UTF8, "application/json"));

        await AssertProblemDetails(response, HttpStatusCode.BadRequest);
        var problem = await ReadJson(response);
        Assert.True(problem.GetProperty("errors").TryGetProperty("title", out _));
    }

    [Fact]
    public async Task Post_TitleOf200Chars_Returns201()
    {
        var title = new string('a', 200);
        var response = await _client.PostAsJsonAsync("/notes", new { title });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_TitleOf201Chars_Returns400NamingTitle()
    {
        var title = new string('a', 201);
        var response = await _client.PostAsJsonAsync("/notes", new { title });

        await AssertProblemDetails(response, HttpStatusCode.BadRequest);
        var problem = await ReadJson(response);
        Assert.True(problem.GetProperty("errors").TryGetProperty("title", out _));
    }

    [Fact]
    public async Task Post_BodyOf10000Chars_Returns201()
    {
        var body = new string('ا', 10000);
        var response = await _client.PostAsJsonAsync("/notes", new { title = "t", body });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_BodyOf10001Chars_Returns400NamingBody()
    {
        var body = new string('ا', 10001);
        var response = await _client.PostAsJsonAsync("/notes", new { title = "t", body });

        await AssertProblemDetails(response, HttpStatusCode.BadRequest);
        var problem = await ReadJson(response);
        Assert.True(problem.GetProperty("errors").TryGetProperty("body", out _));
    }

    [Fact]
    public async Task Post_MalformedJson_Returns400ProblemDetails()
    {
        var response = await _client.PostAsync("/notes", new StringContent("{not json", Encoding.UTF8, "application/json"));

        await AssertProblemDetails(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/notes/{Guid.NewGuid()}");

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_MalformedId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/notes/not-a-guid");

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }
}
