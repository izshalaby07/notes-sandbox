namespace Notes.Api.Features.Notes;

public sealed record Note(Guid Id, string Title, string Body, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
