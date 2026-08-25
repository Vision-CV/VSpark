using Microsoft.EntityFrameworkCore;

using VSpark.Incidents.Models.Entities;

namespace VSpark.Incidents.Persistence;

public class EventDbContext(DbContextOptions<EventDbContext> options) : DbContext(options)
{
    public DbSet<IncidentEntity> Incidents { get; set; }
}
