namespace Notes.Api.Features.Notes;

public sealed record NoteRequest(string? Title, string? Body);

public sealed record NoteResponse(Guid Id, string Title, string Body, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static NoteResponse From(Note note) =>
        new(note.Id, note.Title, note.Body, note.CreatedAt, note.UpdatedAt);
}

public sealed record NoteListResponse(IReadOnlyList<NoteResponse> Items, int Page, int PageSize, int TotalCount);
