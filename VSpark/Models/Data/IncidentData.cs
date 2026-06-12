using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using VSpark.Enums;

namespace VSpark.Models.Data;

public class IncidentData
{
    [Key]
    [JsonIgnore]
    public Guid Guid { get; set; }

    public int CamId { get; set; }

    public int RoomId { get; set; }

    [JsonIgnore]
    public IncidentStatus Status { get; set; }

    public IncidentType Type { get; set; }

    public IncidentPriority Priority { get; set; }

    [JsonIgnore]
    public DateTime Date { get; set; }

    [JsonIgnore]
    public string? Artifact { get; set; }
}