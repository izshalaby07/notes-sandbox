using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Notes.Api.Tests.Features.Notes;

public class DeleteNoteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DeleteNoteTests(WebApplicationFactory<Program> factory)
    {
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
    public async Task Delete_ExistingNote_Returns204NoContent()
    {
        var created = await _client.PostAsJsonAsync("/notes", new { title = "Groceries", body = "milk" });
        var createdJson = await ReadJson(created);
        var id = createdJson.GetProperty("id").GetGuid();

        var response = await _client.DeleteAsync($"/notes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(string.IsNullOrEmpty(await response.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Get_AfterDelete_Returns404ProblemDetails()
    {
        var created = await _client.PostAsJsonAsync("/notes", new { title = "Groceries" });
        var createdJson = await ReadJson(created);
        var id = createdJson.GetProperty("id").GetGuid();

        await _client.DeleteAsync($"/notes/{id}");
        var response = await _client.GetAsync($"/notes/{id}");

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AfterDelete_Returns404ProblemDetails()
    {
        var created = await _client.PostAsJsonAsync("/notes", new { title = "Groceries" });
        var createdJson = await ReadJson(created);
        var id = createdJson.GetProperty("id").GetGuid();

        await _client.DeleteAsync($"/notes/{id}");
        var response = await _client.DeleteAsync($"/notes/{id}");

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404ProblemDetails()
    {
        var response = await _client.DeleteAsync($"/notes/{Guid.NewGuid()}");

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_MalformedId_Returns404ProblemDetails()
    {
        var response = await _client.DeleteAsync("/notes/not-a-guid");

        await AssertProblemDetails(response, HttpStatusCode.NotFound);
    }
}
