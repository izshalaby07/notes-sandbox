using System.Collections.Concurrent;

namespace Notes.Api.Features.Notes;

public sealed class InMemoryNoteStore : INoteStore
{
    private readonly ConcurrentDictionary<Guid, Note> _notes = new();

    public void Add(Note note) => _notes[note.Id] = note;

    public bool TryGet(Guid id, out Note? note) => _notes.TryGetValue(id, out note);

    public bool Remove(Guid id) => _notes.TryRemove(id, out _);

    public bool TryUpdate(Guid id, Func<Note, Note> update, out Note? updated)
    {
        while (_notes.TryGetValue(id, out var existing))
        {
            var candidate = update(existing);
            if (_notes.TryUpdate(id, candidate, existing))
            {
                updated = candidate;
                return true;
            }
        }

        updated = null;
        return false;
    }

    public (IReadOnlyList<Note> Items, int TotalCount) List(int page, int pageSize)
    {
        var ordered = _notes.Values
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .ToList();
        var offset = (long)(page - 1) * pageSize;
        var items = offset >= ordered.Count
            ? []
            : ordered.Skip((int)offset).Take(pageSize).ToList();
        return (items, ordered.Count);
    }
}
