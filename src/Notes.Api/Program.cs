using Notes.Api.Features.Health;
using Notes.Api.Features.Notes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<INoteStore, InMemoryNoteStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStatusCodePages();

app.MapHealthEndpoints();
app.MapNotesEndpoints();

app.Run();

public partial class Program;
