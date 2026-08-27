using Grpc.Core;

namespace VSpark.Incidents.Models.Adapters;

public class OpResult
{
    public bool IsSuccess = false;

    public int Status = 200;

    public string? Response;

    public RpcException? Exception;

    public static OpResult Success(string? comment = null) => new OpResult { IsSuccess = true, Response = comment };

    public static OpResult NotFound(string? comment = null) => BuildResult(StatusCode.NotFound, comment);

    public static OpResult BadRequest(string? comment = null) => BuildResult(StatusCode.InvalidArgument, comment);

    public static OpResult InternalError(string? comment = null) => BuildResult(StatusCode.Internal, comment);

    private static OpResult BuildResult(StatusCode code, string? comment = null) => new OpResult { Exception = new RpcException(new Status(code, comment == null ? string.Empty : comment)), Status = (int)code };
}
