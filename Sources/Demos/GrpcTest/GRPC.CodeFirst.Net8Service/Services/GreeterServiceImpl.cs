using GRPC.CodeFirst.Shared;
using ProtoBuf.Grpc;

namespace GRPC.CodeFirst.Net8Service.Services;

public sealed class GreeterServiceImpl : IGreeterService
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
    {
        return Task.FromResult(new HelloReply
        {
            Message = $"Hello {request.Name}"
        });
    }
}