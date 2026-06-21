using Microsoft.EntityFrameworkCore;

namespace StationShipManifestLogger.Common.Data;

public class ManifestLoggerDbContext : DbContext
{
    public ManifestLoggerDbContext(DbContextOptions<ManifestLoggerDbContext> options) : base(options)
    {
    }

    public DbSet<ManifestAuditLog> ManifestAuditLogs => Set<ManifestAuditLog>();
}