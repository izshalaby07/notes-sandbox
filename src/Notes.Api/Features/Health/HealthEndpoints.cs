using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Notes.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", GetHealth).WithName("GetHealth");
        return app;
    }

    private static IResult GetHealth()
    {
        return TypedResults.Ok(new HealthResponse("ok"));
    }
}

public sealed record HealthResponse(string Status);
