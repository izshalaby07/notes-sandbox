namespace Notes.Api.Features.Notes;

public interface INoteStore
{
    void Add(Note note);

    bool TryGet(Guid id, out Note? note);

    bool Remove(Guid id);

    (IReadOnlyList<Note> Items, int TotalCount) List(int page, int pageSize);
}
