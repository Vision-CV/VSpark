using VSpark.Incidents.Enums;

namespace VSpark.Incidents.Models.Dtos;

public record IncidentDto(Guid Guid, IncidentType Type, IncidentStatus Status, IncidentPriority Priority);

public record NewIncidentDto(int Type, int Priority);
