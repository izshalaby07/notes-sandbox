# Backend conventions

Read before changing server code.

## Shape

One minimal-API project. Each feature has a folder `Features/<Feature>/` with an `Endpoints` static class exposing `Map<Feature>Endpoints(this IEndpointRouteBuilder app)`, called from `Program.cs`. Endpoints stay thin: validate, call the store, map the result. No controllers, no MediatR, no repository-over-repository layers.

## Do / instead of

| Do | Instead of |
| --- | --- |
| `app.MapGroup("/notes")` with typed results (`Results<Ok<NoteResponse>, NotFound>`) | untyped `IResult` everywhere |
| Request/response `record`s next to the endpoints | returning the stored entity directly |
| Register services in `Program.cs` with the narrowest lifetime that works | static state |
| Inject `TimeProvider` for "now" | `DateTimeOffset.UtcNow` in logic |

## Example

```csharp
public static class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/notes");
        notes.MapGet("/{id:guid}", GetById);
        return app;
    }
}
```
