using Google.Protobuf;

using System.Buffers;

using VSpark.Protos;

namespace VSpark.Orchestrator.Services.Rpcs;

public class IncidentsBridge(IncidentService.IncidentServiceClient grpcClient)
{
    // TODO: Abstractions + validation + domain models

    public async Task<GetIncidentResponse?> AddIncidentAsync(string guid, CancellationToken ct)
    {
        GetIncidentResponse response = await grpcClient.GetIncidentAsync(new GuidMessage { Guid = guid }, cancellationToken: ct);

        if (response.Success == 0)
            return null;

        return response;
    }

    public async Task<bool> TryChangeIncidentStatusAsync(string guid, int incidentStatus)
    {
        UniversalResponse response = await grpcClient.ChangeIncidentStatusAsync(new UpdateIncidentStatusRequest { Guid = guid, Status = (IncidentStatus)incidentStatus });

        if (response.Success == 0)
            return false;

        return true;
    }

    public async Task<bool> TryDeleteIncidentAsync(string guid)
    {
        UniversalResponse response = await grpcClient.DeleteIncidentAsync(new GuidMessage { Guid = guid });

        if (response.Success == 0)
            return false;

        return true;
    }

    public async Task<GuidMessage?> CreateIncidentAsync(int type, int priority, int areaId, Stream artifact, CancellationToken ct)
    {
        int bufferLen = 64 * 1024;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLen);

        try
        {
            CreateIncidentRequest request = new CreateIncidentRequest
            {
                Type = type,
                Priority = priority,
                AreaId = areaId
            };

            GuidMessage response = await grpcClient.CreateIncidentAsync(request, cancellationToken: ct);

            using var streamCall = grpcClient.UploadIncidentArtifact(cancellationToken: ct);

            await streamCall.RequestStream.WriteAsync(new UploadIncidentArtifactRequest { Guid = response.Guid });

            int bytesRead = 0;

            while ((bytesRead = await artifact.ReadAsync(buffer, 0, bufferLen)) > 0)
                await streamCall.RequestStream.WriteAsync(new UploadIncidentArtifactRequest { File = ByteString.CopyFrom(buffer, 0, bytesRead) });

            await streamCall.RequestStream.CompleteAsync();

            return response;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
