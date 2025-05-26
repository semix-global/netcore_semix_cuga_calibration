using Grpc.Core;
using GRPC.CodeFirst.Net8Service.Services;
using ProtoBuf.Grpc.Server;

// ReSharper disable LocalizableElement

const int port = 5000;
var server = new Server([
    new ChannelOption(ChannelOptions.MaxSendMessageLength, 500 * 1024 * 1024),
    new ChannelOption(ChannelOptions.MaxReceiveMessageLength, 500 * 1024 * 1024)
])
{
    Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
};
server.Services.AddCodeFirst(new GreeterServiceImpl(), log: Console.Out);
server.Services.AddCodeFirst(new PersonServiceServiceImpl(), log: Console.Out);
server.Start();

Console.WriteLine($"Server listening on port {port}");
Console.ReadKey();

await server.ShutdownAsync().ConfigureAwait(false);