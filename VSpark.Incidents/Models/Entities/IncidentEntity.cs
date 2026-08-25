using System.ComponentModel.DataAnnotations;

using VSpark.Incidents.Enums;

namespace VSpark.Incidents.Models.Entities;

public class IncidentEntity
{
    [Key]
    public Guid Id { get; set; }

    public IncidentStatus Status { get; set; }

    public IncidentPriority Priority { get; set; }

    public IncidentType Type { get; set; }

    public DateTime Timestamp { get; set; }
}
