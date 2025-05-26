using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GRPC.CodeFirst.Shared;

[ProtoContract]
public sealed class HelloReply
{
    [ProtoMember(1)]
    public required string Message { get; set; }
}

[ProtoContract]
public sealed class HelloRequest
{
    [ProtoMember(1)]
    public required string Name { get; set; }
}

[Service]
public interface IGreeterService
{
    [Operation]
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);
}