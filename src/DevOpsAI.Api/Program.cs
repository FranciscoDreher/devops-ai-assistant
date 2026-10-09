using DevOpsAI.Application;
using DevOpsAI.Infrastructure.Prompts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IPromptProvider, FilePromptProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new { service = "DevOpsAI.Api", status = "ready" }));

app.Run();

public partial class Program
{
}
