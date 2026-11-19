using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Documents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<RunbookDocumentOptions>(
    builder.Configuration.GetSection(RunbookDocumentOptions.SectionName));
builder.Services.AddSingleton<RunbookDocumentValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program
{
}
