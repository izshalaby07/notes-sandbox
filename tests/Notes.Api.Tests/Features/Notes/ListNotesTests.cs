using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notes.Api.Tests.Support;

namespace Notes.Api.Tests.Features.Notes;

public class ListNotesTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static (WebApplicationFactory<Program> Factory, HttpClient Client, StubTimeProvider Time) NewApp()
    {
        var time = new StubTimeProvider(FixedNow);
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<TimeProvider>(time)));
        return (factory, factory.CreateClient(), time);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.Clone();
    }

    private static async Task<Guid> Create(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/notes", new { title });
        return (await ReadJson(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task List_EmptyStore_ReturnsEmptyEnvelopeWithDefaults()
    {
        var (factory, client, _) = NewApp();
        using var _f = factory;

        var response = await client.GetAsync("/notes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal(0, json.GetProperty("items").GetArrayLength());
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(20, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(0, json.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task List_DifferentTimestamps_NewestFirst()
    {
        var (factory, client, time) = NewApp();
        using var _f = factory;
        await Create(client, "first");
        time.Advance(TimeSpan.FromMinutes(1));
        await Create(client, "ملاحظة أولى");
        time.Advance(TimeSpan.FromMinutes(1));
        await Create(client, "third");

        var json = await ReadJson(await client.GetAsync("/notes"));

        var titles = json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString());
        Assert.Equal(["third", "ملاحظة أولى", "first"], titles);
        Assert.Equal(3, json.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task List_EqualTimestamps_OrderedByIdDescending()
    {
        var (factory, client, _) = NewApp();
        using var _f = factory;
        var ids = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(await Create(client, $"note {i}"));
        }
        var expected = ids.OrderBy(id => id, Comparer<Guid>.Create((a, b) => b.CompareTo(a))).ToList();

        var json = await ReadJson(await client.GetAsync("/notes"));

        var actual = json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task List_PageSizeAndPage_ReturnsThatSlice()
    {
        var (factory, client, time) = NewApp();
        using var _f = factory;
        for (var i = 0; i < 5; i++)
        {
            await Create(client, $"note {i}");
            time.Advance(TimeSpan.FromMinutes(1));
        }

        var json = await ReadJson(await client.GetAsync("/notes?page=2&pageSize=2"));

        var titles = json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString());
        Assert.Equal(["note 2", "note 1"], titles);
        Assert.Equal(2, json.GetProperty("page").GetInt32());
        Assert.Equal(2, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(5, json.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task List_PastLastPage_ReturnsEmptyItemsAndRealTotal()
    {
        var (factory, client, _) = NewApp();
        using var _f = factory;
        await Create(client, "only");

        var response = await client.GetAsync("/notes?page=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal(0, json.GetProperty("items").GetArrayLength());
        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task List_PageSize100_Returns200()
    {
        var (factory, client, _) = NewApp();
        using var _f = factory;

        var response = await client.GetAsync("/notes?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(100, (await ReadJson(response)).GetProperty("pageSize").GetInt32());
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    public async Task List_OutOfRangeParameter_Returns400NamingField(string query, string field)
    {
        var (factory, client, _) = NewApp();
        using var _f = factory;

        var response = await client.GetAsync($"/notes?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await ReadJson(response);
        Assert.True(json.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("page=abc")]
    [InlineData("pageSize=abc")]
    public async Task List_NonNumericParameter_Returns400ProblemDetails(string query)
    {
        var (factory, client, _) = NewApp();
        using var _f = factory;

        var response = await client.GetAsync($"/notes?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, (await ReadJson(response)).GetProperty("status").GetInt32());
    }
}
