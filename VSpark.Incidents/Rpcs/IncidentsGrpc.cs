using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using VSpark.Incidents.Services;
using VSpark.Protos;

namespace VSpark.Incidents.Rpcs;

public class IncidentsGrpc(IncidentsManager incidentsManager) : IncidentService.IncidentServiceBase
{
    public override Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, ServerCallContext context)
    {
        
        
        return base.CreateIncidentAsync(request, context);
    }

    public override async Task<Empty> UploadIncidentArtifact(IAsyncStreamReader<UploadIncidentArtifactRequest> requestStream, ServerCallContext context)
    {
        if (!await requestStream.MoveNext(context.CancellationToken))
            throw new RpcException(BadRequest());

        if (requestStream.Current.DataCase != UploadIncidentArtifactRequest.DataOneofCase.Guid)
            throw new RpcException(BadRequest());

        UploadIncidentArtifactRequest dataRequest = requestStream.Current;

        if (!Guid.TryParse(dataRequest.Guid, out _))
            throw new RpcException(BadRequest());

        using MemoryStream artifactStream = new();
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            if (requestStream.Current.DataCase != UploadIncidentArtifactRequest.DataOneofCase.File)
                throw new RpcException(BadRequest());

            artifactStream.Write(requestStream.Current.File.ToByteArray());
        }

        artifactStream.Position = 0;

        await incidentsManager.SaveArtifactAsync(dataRequest.Guid, artifactStream);

        return new Empty();
    }

    private Status BadRequest(string? comment = null) => new Status(StatusCode.InvalidArgument, comment == null ? string.Empty : comment);
}
