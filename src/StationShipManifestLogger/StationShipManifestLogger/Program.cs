using Azure.Storage.Queues;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StationShipManifestLogger.Common.Data;
using StationShipManifestLogger.Features.Docking;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ManifestLoggerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var azureStorageConnection = builder.Configuration.GetConnectionString("AzureStorageConnection")
    ?? throw new InvalidOperationException("AzureStorageConnection connection string is not set. Fix your configuration.");
builder.Services.AddSingleton(new QueueServiceClient(azureStorageConnection));
builder.Services.AddScoped<ShipManifestQueuePublisher>();
builder.Services.AddControllers();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ManifestSubmission", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("https://agreeable-moss-0ff2e0510.7.azurestaticapps.net")
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var app = builder.Build();
app.UseHttpsRedirection();

app.UseCors("DashboardPolicy");
app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ManifestLoggerDbContext>();
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
