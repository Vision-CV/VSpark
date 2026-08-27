using Grpc.Core;

using System.Net;

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
        UniversalResponse response = new();

        // DRY
        if (!await requestStream.MoveNext(context.CancellationToken))
        {
            response.Status = (int)HttpStatusCode.BadRequest;

            return response;
        }

        if (requestStream.Current.DataCase != UploadIncidentArtifactRequest.DataOneofCase.Guid)
        {
            response.Status = (int)HttpStatusCode.BadRequest;

            return response;
        }

        UploadIncidentArtifactRequest dataRequest = requestStream.Current;

        if (!Guid.TryParse(dataRequest.Guid, out _))
            return response;

        using MemoryStream artifactStream = new();
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            // DRY
            if (requestStream.Current.DataCase != UploadIncidentArtifactRequest.DataOneofCase.File)
            {
                response.Status = (int)HttpStatusCode.BadRequest;

                return response;
            }

            artifactStream.Write(requestStream.Current.File.ToByteArray());
        }

        artifactStream.Position = 0;

        await incidentsManager.SaveArtifactAsync(dataRequest.Guid, artifactStream);

        response.Success = 1;

        return response;
    }

    public override async Task<UniversalResponse> ChangeIncidentStatusAsync(UpdateIncidentStatusRequest request, ServerCallContext context)
    {
        // Unsafe
        Enums.IncidentStatus status = (Enums.IncidentStatus)request.Status;

        OpResult opResult = await incidentsManager.ChangeIncidentStatus(Guid.Parse(request.Guid), status);

        if (!opResult.IsSuccess)
            return new UniversalResponse { Status = opResult.Status, Success = 0 };

        return new UniversalResponse { Status = 200, Success = 1 };
    }

    public override async Task<UniversalResponse> DeleteIncidentAsync(GuidMessage request, ServerCallContext context)
    {
        // Unsafe
        OpResult opResult = await incidentsManager.DeleteIncidentAsync(Guid.Parse(request.Guid));

        if (!opResult.IsSuccess)
            return new UniversalResponse { Status = opResult.Status, Success = 0 };

        return new UniversalResponse { Status = 200, Success = 1 };
    }

    public override async Task<GetIncidentResponse> GetIncidentAsync(GuidMessage request, ServerCallContext context)
    {
        // Unsafe
        IncidentDto? incident = await incidentsManager.GetIncidentAsync(Guid.Parse(request.Guid));

        GetIncidentResponse response = new();

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
}
