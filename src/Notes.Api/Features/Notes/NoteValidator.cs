namespace Notes.Api.Features.Notes;

/// <summary>All request validation rules for notes live here, not in the endpoints.</summary>
public static class NoteValidator
{
    public const int MaxTitleLength = 200;
    public const int MaxBodyLength = 10_000;

    /// <returns>Errors keyed by field name, or an empty dictionary when the request is valid.</returns>
    public static Dictionary<string, string[]> Validate(NoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (request.Title.Length > MaxTitleLength)
        {
            errors["title"] = [$"Title must be at most {MaxTitleLength} characters."];
        }

        if (request.Body != null && request.Body.Length > MaxBodyLength)
        {
            errors["body"] = [$"Body must be at most {MaxBodyLength} characters."];
        }

        return errors;
    }
}
