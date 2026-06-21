using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using StationShipManifestLogger.Common.Data;
using StationShipManifestLogger.Features.Docking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var azureStorageConnection = builder.Configuration.GetConnectionString("AzureStorageConnection")
    ?? throw new InvalidOperationException("AzureStorageConnection connection string is not set. Fix your configuration.");
builder.Services.AddSingleton(new QueueServiceClient(azureStorageConnection));
builder.Services.AddScoped<ShipManifestQueuePublisher>();
builder.Services.AddControllers();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        policy.WithOrigins(
        "http://localhost:5173",
        "https://agreeable-moss-0ff2e0510.7.azurestaticapps.net"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("DashboardPolicy");

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
        var dbDir = "/home/data";
        if (!Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        db.Database.Migrate();
    }
}

app.Run();
