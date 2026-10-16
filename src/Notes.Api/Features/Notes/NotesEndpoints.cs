using Microsoft.AspNetCore.Http.HttpResults;

namespace Notes.Api.Features.Notes;

public static class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/notes");
        notes.MapPost("/", Create);
        notes.MapGet("/{id:guid}", GetById).WithName("GetNoteById");
        notes.MapDelete("/{id:guid}", Delete);
        return app;
    }

    private static Results<Created<NoteResponse>, ValidationProblem> Create(
        NoteRequest request, INoteStore store, TimeProvider time)
    {
        var errors = NoteValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var now = time.GetUtcNow();
        var note = new Note(Guid.NewGuid(), request.Title!, request.Body ?? "", now, now);
        store.Add(note);
        return TypedResults.Created($"/notes/{note.Id}", NoteResponse.From(note));
    }

    private static Results<Ok<NoteResponse>, ProblemHttpResult> GetById(Guid id, INoteStore store)
    {
        return store.TryGet(id, out var note)
            ? TypedResults.Ok(NoteResponse.From(note!))
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Note not found.");
    }

    private static Results<NoContent, ProblemHttpResult> Delete(Guid id, INoteStore store)
    {
        return store.Remove(id)
            ? TypedResults.NoContent()
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Note not found.");
    }
}
