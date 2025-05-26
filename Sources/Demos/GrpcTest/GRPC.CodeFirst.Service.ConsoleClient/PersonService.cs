using Grpc.Core;
using GRPC.CodeFirst.Shared;
using Newtonsoft.Json;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using System.Diagnostics;

// ReSharper disable LocalizableElement

namespace GRPC.CodeFirst.Service.ConsoleClient;

public static class PersonService
{
    public static async Task GetCallContextAsync(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        using var cancel = new CancellationTokenSource(100000);
        var options = new CallOptions(cancellationToken: cancel.Token);
        var person = await personService.GetCallContextAsync(new IntWrapper { Value = 111122233 }, new CallContext(options)).ConfigureAwait(false);
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static async Task GetCancellationTokenAsync(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        using var cancel = new CancellationTokenSource(100000);
        var person = await personService.GetCancellationTokenAsync(new IntWrapper { Value = 111122233 }, cancel.Token).ConfigureAwait(false);
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static async Task GetCancellationToken2Async(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        using var cancel = new CancellationTokenSource();
        var cancellationToken = cancel.Token;
        var personTask = personService.GetCancellationTokenAsync(new IntWrapper { Value = 111122233 }, cancellationToken);
        _ = Task.Run(() =>
        {
            Thread.Sleep(1000);
            cancel.Cancel();
        }, cancellationToken);
        var person = await personTask.ConfigureAwait(false);
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static void GetList(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        var person = personService.GetList();
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static void GetListByParam(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        var person = personService.GetListByParam(new TestObj { Success = true, Msg = "sdfa" });
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static void GetListByParam2(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        var person = personService.GetListByParam1Async().GetAwaiter().GetResult();
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static void GetListByParam3(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        var person = personService.GetListByParam2Async().GetAwaiter().GetResult();
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static void GetList1(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        var person = personService.GetList1();
        Console.WriteLine($"Result {JsonConvert.SerializeObject(person, Formatting.Indented)}");
    }

    public static void Test(ChannelBase channel)
    {
        GetList(channel);
        GetListByParam(channel);
        GetListByParam2(channel);
        GetListByParam3(channel);
        GetList1(channel);
    }

    public static void TestLoop(ChannelBase channel)
    {
        var personService = channel.CreateGrpcService<IPersonService>();
        foreach (var _ in Enumerable.Range(1, 1000))
        {
#if NET
            var timestamp = Stopwatch.GetTimestamp();
#else
            var stopWatch = Stopwatch.StartNew();
#endif
            var person = personService.Get(new ParamWrapper { Id = 111122233, IsLarge = false, IsRandom = true /*, Image = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test")))*/ });

            // 保存到图片
            // File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.jpg"), addressImage);
            Console.WriteLine(person.Id);
#if NET
            var elapsedTime = Stopwatch.GetElapsedTime(timestamp);
            Console.WriteLine($"running mean: {elapsedTime.Milliseconds:0.000} ms");
#else
            stopWatch.Stop();
            Console.WriteLine($"running mean: {stopWatch.Elapsed.Milliseconds:0.000} ms");
#endif
        }
    }
}