using System.Collections.Concurrent;

namespace Notes.Api.Features.Notes;

public sealed class InMemoryNoteStore : INoteStore
{
    private readonly ConcurrentDictionary<Guid, Note> _notes = new();

    public void Add(Note note) => _notes[note.Id] = note;

    public bool TryGet(Guid id, out Note? note) => _notes.TryGetValue(id, out note);

    public bool Remove(Guid id) => _notes.TryRemove(id, out _);

    public (IReadOnlyList<Note> Items, int TotalCount) List(int page, int pageSize)
    {
        var ordered = _notes.Values
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, ordered.Count);
    }
}
