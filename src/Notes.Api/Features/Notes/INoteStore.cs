namespace Notes.Api.Features.Notes;

public interface INoteStore
{
    void Add(Note note);

    bool TryGet(Guid id, out Note? note);
}
