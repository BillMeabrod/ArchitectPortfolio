using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using StationShipManifestLogger.Common.Data;
using StationShipManifestLogger.Features.Docking;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<StationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<StationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSqlConnection")));
}

builder.Services.AddSingleton(new QueueServiceClient(
    builder.Configuration.GetConnectionString("AzureStorageConnection")));
builder.Services.AddScoped<ShipManifestQueuePublisher>();
builder.Services.AddControllers();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHealthChecks();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StationDbContext>();
    if (app.Environment.IsDevelopment())
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

app.Run();
