using Microsoft.AspNetCore.Http.HttpResults;

namespace Notes.Api.Features.Notes;

public static class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/notes");
        notes.MapPost("/", Create);
        notes.MapGet("/", List);
        notes.MapGet("/{id:guid}", GetById).WithName("GetNoteById");
        notes.MapPut("/{id:guid}", Update);
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

    private static Results<Ok<NoteListResponse>, ValidationProblem> List(
        INoteStore store, int? page, int? pageSize)
    {
        var pageValue = page ?? 1;
        var pageSizeValue = pageSize ?? PagingValidator.DefaultPageSize;
        var errors = PagingValidator.Validate(pageValue, pageSizeValue);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var (items, totalCount) = store.List(pageValue, pageSizeValue);
        return TypedResults.Ok(new NoteListResponse(
            items.Select(NoteResponse.From).ToList(), pageValue, pageSizeValue, totalCount));
    }

    private static Results<Ok<NoteResponse>, ProblemHttpResult> GetById(Guid id, INoteStore store)
    {
        return store.TryGet(id, out var note)
            ? TypedResults.Ok(NoteResponse.From(note!))
            : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Note not found.");
    }

    private static Results<Ok<NoteResponse>, ValidationProblem, ProblemHttpResult> Update(
        Guid id, NoteRequest request, INoteStore store, TimeProvider time)
    {
        var errors = NoteValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var now = time.GetUtcNow();
        return store.TryUpdate(id, existing => existing with { Title = request.Title!, Body = request.Body ?? "", UpdatedAt = now }, out var note)
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
