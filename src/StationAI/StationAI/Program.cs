using Azure.Storage.Blobs;
using StationAI.Adapters.Outbound;
using StationAI.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(new BlobServiceClient(
    builder.Configuration.GetConnectionString("BlobStorageConnection")));
builder.Services.AddScoped<IRulesRepository, RulesBlobStorageAdapter>();
builder.Services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
