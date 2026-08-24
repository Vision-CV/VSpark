using Grpc.Core;

using VSpark.Incidents.Services;
using VSpark.Protos;

namespace VSpark.Incidents.Rpcs;

public class IncidentsGrpc(IncidentsManager incidentsManager) : IncidentService.IncidentServiceBase
{

}
