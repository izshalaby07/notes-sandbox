using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notes.Api.Tests.Support;

namespace Notes.Api.Tests.Features.Notes;

public class UpdateNoteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private const string ArabicTitle = "ملاحظة أولى";
    private const string ArabicBody = "نص الملاحظة";

    private readonly WebApplicationFactory<Program> _factory;

    public UpdateNoteTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient Client, StubTimeProvider Time) NewApp()
    {
        var time = new StubTimeProvider(FixedNow);
        var client = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<TimeProvider>(time))).CreateClient();
        return (client, time);
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

    private static async Task<Guid> CreateNote(HttpClient client, string title, string? body = "original")
    {
        var created = await client.PostAsJsonAsync("/notes", new { title, body });
        return (await ReadJson(created)).GetProperty("id").GetGuid();
    }

    private static async Task AssertUnchanged(HttpClient client, Guid id)
    {
        var json = await ReadJson(await client.GetAsync($"/notes/{id}"));
        Assert.Equal(ArabicTitle, json.GetProperty("title").GetString());
        Assert.Equal(ArabicBody, json.GetProperty("body").GetString());
        Assert.Equal(FixedNow, json.GetProperty("updatedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Put_ValidRequest_Returns200WithReplacedNote()
    {
        var (client, _) = NewApp();
        var id = await CreateNote(client, "Old");

        var response = await client.PutAsJsonAsync($"/notes/{id}", new { title = ArabicTitle, body = ArabicBody });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal(id, json.GetProperty("id").GetGuid());
        Assert.Equal(ArabicTitle, json.GetProperty("title").GetString());
        Assert.Equal(ArabicBody, json.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Put_ValidRequest_PersistsChange()
    {
        var (client, _) = NewApp();
        var id = await CreateNote(client, "Old");

        await client.PutAsJsonAsync($"/notes/{id}", new { title = ArabicTitle, body = ArabicBody });
        var json = await ReadJson(await client.GetAsync($"/notes/{id}"));

        Assert.Equal(ArabicTitle, json.GetProperty("title").GetString());
        Assert.Equal(ArabicBody, json.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Put_LaterTime_ChangesUpdatedAtButNotCreatedAt()
    {
        var (client, time) = NewApp();
        var id = await CreateNote(client, "Old");
        time.Advance(TimeSpan.FromMinutes(5));

        var json = await ReadJson(await client.PutAsJsonAsync($"/notes/{id}", new { title = "New", body = "x" }));

        Assert.Equal(FixedNow, json.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(FixedNow.AddMinutes(5), json.GetProperty("updatedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Put_NothingChanged_StillUpdatesUpdatedAt()
    {
        var (client, time) = NewApp();
        var id = await CreateNote(client, ArabicTitle, ArabicBody);
        time.Advance(TimeSpan.FromMinutes(5));

        var json = await ReadJson(await client.PutAsJsonAsync($"/notes/{id}", new { title = ArabicTitle, body = ArabicBody }));

        Assert.Equal(FixedNow.AddMinutes(5), json.GetProperty("updatedAt").GetDateTimeOffset());
        Assert.Equal(FixedNow, json.GetProperty("createdAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Put_WithoutBody_ClearsBody()
    {
        var (client, _) = NewApp();
        var id = await CreateNote(client, "Old", ArabicBody);

        var json = await ReadJson(await client.PutAsJsonAsync($"/notes/{id}", new { title = "New" }));

        Assert.Equal("", json.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Put_NullBody_ClearsBody()
    {
        var (client, _) = NewApp();
        var id = await CreateNote(client, "Old", ArabicBody);

        var json = await ReadJson(await client.PutAsJsonAsync($"/notes/{id}", new { title = "New", body = (string?)null }));

        Assert.Equal("", json.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Put_ClientSentServerFields_AreIgnored()
    {
        var (client, time) = NewApp();
        var id = await CreateNote(client, "Old");
        time.Advance(TimeSpan.FromMinutes(5));

        var json = await ReadJson(await client.PutAsJsonAsync($"/notes/{id}", new
        {
            id = Guid.NewGuid(),
            title = "New",
            createdAt = "2000-01-01T00:00:00Z",
            updatedAt = "2000-01-01T00:00:00Z",
        }));

        Assert.Equal(id, json.GetProperty("id").GetGuid());
        Assert.Equal(FixedNow, json.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(FixedNow.AddMinutes(5), json.GetProperty("updatedAt").GetDateTimeOffset());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Put_MissingOrBlankTitle_Returns400AndLeavesNoteUnchanged(string? title)
    {
        var (client, time) = NewApp();
        var id = await CreateNote(client, ArabicTitle, ArabicBody);
        time.Advance(TimeSpan.FromMinutes(5));

        var response = await client.PutAsJsonAsync($"/notes/{id}", new { title, body = "changed" });

        await AssertProblemDetails(response, HttpStatusCode.BadRequest);
        Assert.True((await ReadJson(response)).GetProperty("errors").TryGetProperty("title", out _));
        await AssertUnchanged(client, id);
    }

    [Fact]
    public async Task Put_TitleOver200_Returns400AndLeavesNoteUnchanged()
    {
        var (client, time) = NewApp();
        var id = await CreateNote(client, ArabicTitle, ArabicBody);
        time.Advance(TimeSpan.FromMinutes(5));

        var response = await client.PutAsJsonAsync($"/notes/{id}", new { title = new string('ع', 201) });

        await AssertProblemDetails(response, HttpStatusCode.BadRequest);
        Assert.True((await ReadJson(response)).GetProperty("errors").TryGetProperty("title", out _));
        await AssertUnchanged(client, id);
    }

    [Fact]
    public async Task Put_BodyOver10000_Returns400AndLeavesNoteUnchanged()
    {
        var (client, time) = NewApp();
        var id = await CreateNote(client, ArabicTitle, ArabicBody);
        time.Advance(TimeSpan.FromMinutes(5));

        var response = await client.PutAsJsonAsync($"/notes/{id}", new { title = "New", body = new string('ع', 10_001) });

        await AssertProblemDetails(response, HttpStatusCode.BadRequest);
        Assert.True((await ReadJson(response)).GetProperty("errors").TryGetProperty("body", out _));
        await AssertUnchanged(client, id);
    }

    [Fact]
    public async Task Put_UnknownId_Returns404ProblemDetails()
    {
        var (client, _) = NewApp();

        var response = await client.PutAsJsonAsync($"/notes/{Guid.NewGuid()}", new { title = "New" });

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_MalformedId_Returns404ProblemDetails()
    {
        var (client, _) = NewApp();

        var response = await client.PutAsJsonAsync("/notes/not-a-guid", new { title = "New" });

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_DeletedNote_Returns404AndDoesNotResurrect()
    {
        var (client, _) = NewApp();
        var id = await CreateNote(client, "Old");
        await client.DeleteAsync($"/notes/{id}");

        var response = await client.PutAsJsonAsync($"/notes/{id}", new { title = "New" });

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
        await AssertProblemDetails(await client.GetAsync($"/notes/{id}"), HttpStatusCode.NotFound);
    }
}
