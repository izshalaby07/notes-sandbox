namespace Notes.Api.Features.Notes;

/// <summary>All request validation rules for notes live here, not in the endpoints.</summary>
public static class NoteValidator
{
    /// <returns>Errors keyed by field name, or an empty dictionary when the request is valid.</returns>
    public static Dictionary<string, string[]> Validate(NoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }

        return errors;
    }
}
