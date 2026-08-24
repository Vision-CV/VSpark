using System.ComponentModel.DataAnnotations;

namespace VSpark.Incidents.Models.Entities;

public class IncidentEntity
{
    [Key]
    public Guid Id { get; set; }

    public int Status { get; set; }

    public int Priority { get; set; }

    public int Type { get; set; }

    public string? Artifact { get; set; }

    public DateTime Timestamp { get; set; }
}
