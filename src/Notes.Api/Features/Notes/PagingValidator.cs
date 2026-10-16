namespace Notes.Api.Features.Notes;

/// <summary>Validation rules for the <c>page</c> and <c>pageSize</c> query parameters.</summary>
public static class PagingValidator
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <returns>Errors keyed by parameter name, or an empty dictionary when the values are valid.</returns>
    public static Dictionary<string, string[]> Validate(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();

        if (page < 1)
        {
            errors["page"] = ["Page must be at least 1."];
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            errors["pageSize"] = [$"Page size must be between 1 and {MaxPageSize}."];
        }

        return errors;
    }
}
