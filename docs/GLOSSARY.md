# Glossary

Use these words in code, specs, tickets and chat. Add a term here before using a new one.
`/grill-with-docs` adds terms here as decisions are made.

| Term | Meaning | Not to be confused with |
| --- | --- | --- |
| Note | One piece of text a user saves, with a title and a body | Comment |
| Title | Short required heading of a note | Body |
| Body | The note's main text, optional | Title |
| Note store | The component that saves and loads notes | Database (notes are not persisted, see ADR-0002) |
| Search text | The text a client looks for inside note titles and bodies | Filter by tag (tags are out of scope) |
| Delete | Remove a note permanently; it cannot be read or restored afterwards | Archive, trash, soft delete |
