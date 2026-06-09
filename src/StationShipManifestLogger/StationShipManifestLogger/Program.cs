using Microsoft.EntityFrameworkCore;
using StationShipManifestLogger.Common.Data;

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

builder.Services.AddControllers();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHealthChecks();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
