using GRPC.CodeFirst.Net8Service.Services;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCodeFirstGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 500 * 1024 * 1024; // 500 MB
    options.MaxSendMessageSize = 500 * 1024 * 1024; // 500 MB
});
builder.Services.AddCodeFirstGrpcReflection();

var app = builder.Build();

app.MapGrpcService<GreeterServiceImpl>();
app.MapGrpcService<PersonServiceServiceImpl>();
app.MapCodeFirstGrpcReflectionService();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();