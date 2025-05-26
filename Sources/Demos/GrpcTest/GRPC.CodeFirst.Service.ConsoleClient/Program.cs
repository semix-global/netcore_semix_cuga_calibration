using grpc.reflection.v1alpha;
using GRPC.CodeFirst.Service.ConsoleClient;
using GRPC.CodeFirst.Shared;
using Net.Utilities.Helper.File;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

// ReSharper disable LocalizableElement

var generator = new SchemaGenerator
{
    ProtoSyntax = ProtoSyntax.Proto3
};

DirectoryHelper.CreateDirectoryIfNotExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proto"));

FileHelper.DeleteFileIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proto\\GreeterService.proto"));
FileHelper.DeleteFileIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proto\\ServerReflection.proto"));
FileHelper.DeleteFileIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proto\\PersonService.proto"));

File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proto\\GreeterService.proto"), generator.GetSchema<IGreeterService>());
File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proto\\ServerReflection.proto"), generator.GetSchema<IServerReflection>());
File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proto\\PersonService.proto"), generator.GetSchema<IPersonService>());

#if NET
var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://127.0.0.1:5000", new Grpc.Net.Client.GrpcChannelOptions
{
    MaxReceiveMessageSize = 500 * 1024 * 1024,
    MaxSendMessageSize = 500 * 1024 * 1024
});
#else
var channel = new Grpc.Core.Channel("127.0.0.1", 5000, Grpc.Core.ChannelCredentials.Insecure,
[
    new Grpc.Core.ChannelOption(Grpc.Core.ChannelOptions.MaxSendMessageLength, 500 * 1024 * 1024),
    new Grpc.Core.ChannelOption(Grpc.Core.ChannelOptions.MaxReceiveMessageLength, 500 * 1024 * 1024)
]);
#endif

PersonService.TestLoop(channel);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();