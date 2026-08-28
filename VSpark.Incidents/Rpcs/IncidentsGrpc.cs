using Grpc.Core;

using VSpark.Incidents.Models.Adapters;
using VSpark.Incidents.Models.Dtos;
using VSpark.Incidents.Services;
using VSpark.Protos;

namespace VSpark.Incidents.Rpcs;

public class IncidentsGrpc(IncidentsManager incidentsManager) : IncidentService.IncidentServiceBase
{
    // TODO: Validation & DRY integration required.

    public override async Task<GuidMessage> CreateIncidentAsync(CreateIncidentRequest request, ServerCallContext context)
    {
        NewIncidentDto incidentDto = new NewIncidentDto((int)request.Type, (int)request.Priority);

        OpResult operationResult = await incidentsManager.AddIncidentAsync(incidentDto);

        return new GuidMessage { Guid = operationResult.Response };
    }

    public override async Task<UniversalResponse> UploadIncidentArtifactAsync(IAsyncStreamReader<UploadIncidentArtifactRequest> requestStream, ServerCallContext context)
    {
        if (!await requestStream.MoveNext(context.CancellationToken))
            return UniversalBadRequest();

        if (requestStream.Current.DataCase != UploadIncidentArtifactRequest.DataOneofCase.Guid)
            return UniversalBadRequest();
               
        UploadIncidentArtifactRequest dataRequest = requestStream.Current;

        if (!Guid.TryParse(dataRequest.Guid, out _))
            return UniversalBadRequest();

        using MemoryStream artifactStream = new();
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            if (requestStream.Current.DataCase != UploadIncidentArtifactRequest.DataOneofCase.File)
                return UniversalBadRequest();

            artifactStream.Write(requestStream.Current.File.ToByteArray());
        }

        artifactStream.Position = 0;

        await incidentsManager.SaveArtifactAsync(dataRequest.Guid, artifactStream);

        UniversalResponse response = new() { Success = 1 };

        return response;
    }

    public override async Task<UniversalResponse> ChangeIncidentStatusAsync(UpdateIncidentStatusRequest request, ServerCallContext context)
    {
        if (!Enum.IsDefined(typeof(Enums.IncidentStatus), request.Status) || !Enum.IsDefined(typeof(Protos.IncidentStatus), request.Status))
            return UniversalBadRequest();

        Enums.IncidentStatus status = (Enums.IncidentStatus)request.Status;

        if (string.IsNullOrEmpty(request.Guid) || !Guid.TryParse(request.Guid, out Guid guid))
            return UniversalBadRequest();

        OpResult opResult = await incidentsManager.ChangeIncidentStatus(guid, status);

        if (!opResult.IsSuccess)
            return new UniversalResponse { Status = opResult.Status, Success = 0 };

        return new UniversalResponse { Status = 200, Success = 1 };
    }

    public override async Task<UniversalResponse> DeleteIncidentAsync(GuidMessage request, ServerCallContext context)
    {
        UniversalResponse response = new();

        if (string.IsNullOrEmpty(request.Guid) || !Guid.TryParse(request.Guid, out Guid guid))
            return UniversalBadRequest();

        OpResult opResult = await incidentsManager.DeleteIncidentAsync(guid);

        if (!opResult.IsSuccess)
        {
            response.Status = opResult.Status;

            return response;
        }

        return response;
    }

    public override async Task<GetIncidentResponse> GetIncidentAsync(GuidMessage request, ServerCallContext context)
    {
        GetIncidentResponse response = new();

        if (string.IsNullOrEmpty(request.Guid) || !Guid.TryParse(request.Guid, out Guid guid))
        {
            response.Success = 0;

            return response;
        }

        IncidentDto? incident = await incidentsManager.GetIncidentAsync(guid);

        if (incident == null)
        {
            response.Success = 0;

            return response;
        }

        response.Success = 1;

        response.Guid = incident.Guid.ToString();
        response.Type = (IncidentType)incident.Type;
        response.Status = (IncidentStatus)incident.Status;
        response.Priority = (IncidentPriority)incident.Priority;

        return response;
    }

    private UniversalResponse UniversalBadRequest() => new UniversalResponse { Status = 400, Success = 0 };
}
