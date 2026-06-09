using Microsoft.EntityFrameworkCore;

namespace StationShipManifestLogger.Common.Data;

public class StationDbContext : DbContext
{
    public StationDbContext(DbContextOptions<StationDbContext> options) : base(options)
    {
    }

    public DbSet<ManifestAuditLog> ManifestAuditLogs => Set<ManifestAuditLog>();
}