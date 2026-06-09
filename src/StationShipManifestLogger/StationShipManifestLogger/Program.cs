using Microsoft.EntityFrameworkCore;
using StationShipManifestLogger.Common.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<StationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddControllers();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StationDbContext>();
    // This outputs the exact absolute path the application is using to look for the database file
    Console.WriteLine($"====================================================");
    Console.WriteLine($"DATABASE PATH: {dbContext.Database.GetDbConnection().DataSource}");
    Console.WriteLine($"====================================================");
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
