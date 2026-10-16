using System.Collections.Concurrent;

namespace Notes.Api.Features.Notes;

public sealed class InMemoryNoteStore : INoteStore
{
    private readonly ConcurrentDictionary<Guid, Note> _notes = new();

    public void Add(Note note) => _notes[note.Id] = note;

    public bool TryGet(Guid id, out Note? note) => _notes.TryGetValue(id, out note);
}
