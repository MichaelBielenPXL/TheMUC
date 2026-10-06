using Microsoft.EntityFrameworkCore;

namespace MucBackend.History;

public class MucDbContext(DbContextOptions<MucDbContext> options) : DbContext(options)
{
    public DbSet<ImportRecord> Imports => Set<ImportRecord>();
}
