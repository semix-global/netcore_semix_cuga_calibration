using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GRPC.CodeFirst.Shared;
using Newtonsoft.Json;
using ProtoBuf.Grpc.Client;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Windows;

namespace GRPC.CodeFirst.Service.WpfClient;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _localJson = string.Empty;

    [ObservableProperty]
    private string _remoteJson = string.Empty;

    [ObservableProperty]
    private byte[] _localImage = [];

    [ObservableProperty]
    private byte[] _remoteImage = [];

    [ObservableProperty]
    private byte[] _fpsImage = [];

    [ObservableProperty]
    private double _fps;

    private readonly Grpc.Core.ChannelBase _channel;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public MainWindowViewModel()
    {
#if NET
        _channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://127.0.0.1:5000", new Grpc.Net.Client.GrpcChannelOptions
        {
            MaxReceiveMessageSize = 200 * 1024 * 1024,
            MaxSendMessageSize = 200 * 1024 * 1024
        });
#else
        _channel = new Grpc.Core.Channel("127.0.0.1", 5000, Grpc.Core.ChannelCredentials.Insecure,
        [
            new Grpc.Core.ChannelOption(Grpc.Core.ChannelOptions.MaxSendMessageLength, 200 * 1024 * 1024),
            new Grpc.Core.ChannelOption(Grpc.Core.ChannelOptions.MaxReceiveMessageLength, 200 * 1024 * 1024)
        ]);
#endif
        var personService = _channel.CreateGrpcService<IPersonService>();

#pragma warning disable IDE0079
#pragma warning disable IDISP001
        var fpsMonitor = Observable.Interval(TimeSpan.FromMilliseconds(10)).Subscribe(_ =>
        {
#if NET
            var timestamp = Stopwatch.GetTimestamp();
#else
            var stopWatch = Stopwatch.StartNew();
#endif

            var newPerson = personService.Get(new ParamWrapper { IsRandom = true });

            FpsImage = newPerson.Address.Image;
#if NET
            var elapsedTime = Stopwatch.GetElapsedTime(timestamp);
            Fps = 1000d / elapsedTime.Milliseconds;
            Debug.WriteLine($"running mean: {elapsedTime.Milliseconds:0.000} ms");
#else
            stopWatch.Stop();
            Fps = 1000d / stopWatch.Elapsed.Milliseconds;
            Debug.WriteLine($"running mean: {stopWatch.Elapsed.Milliseconds:0.000} ms");
#endif
#pragma warning restore IDISP001
#pragma warning restore IDE0079
        });
        _cancellationTokenSource.Token.Register(fpsMonitor.Dispose);
    }

    [RelayCommand]
    private async Task ReadAsync()
    {
        LocalJson = string.Empty;
        LocalImage = [];
        RemoteJson = string.Empty;
        RemoteImage = [];
        var paramWrapper = new ParamWrapper
        {
            Id = 1122334455,
            Name = "TestTestTest",
            TagEnum = TagEnum.Tag1 | TagEnum.Tag3 | TagEnum.Tag2,
            DateTime = DateTime.Now,
            Guid = Ulid.NewUlid().ToGuid(),
            Image = null,
            IsLarge = false
        };

        await Task.Delay(500).ConfigureAwait(false);

        var person = new Person
        {
            Id = paramWrapper.Id.Value,
            Name = paramWrapper.Name,
            TagEnum = paramWrapper.TagEnum.Value,
            DateTime = paramWrapper.DateTime.Value,
            Guid = paramWrapper.Guid.Value,
            Address = new Address
            {
                Line1 = "Flat 1",
                Line2 = "The Meadows",
                Image = paramWrapper.Image ??
                        (paramWrapper.IsLarge
                            ? Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\TestLarge.txt")))
                            : Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Test.txt"))))
            }
        };

        LocalImage = person.Address.Image;
        LocalJson = JsonConvert.SerializeObject(person, Formatting.Indented);
        var personService = _channel.CreateGrpcService<IPersonService>();
        var newPerson = personService.Get(paramWrapper);

        RemoteImage = newPerson.Address.Image;
        RemoteJson = JsonConvert.SerializeObject(newPerson, Formatting.Indented);

        MessageBox.Show(string.Equals(LocalJson, RemoteJson).ToString());
    }

    [RelayCommand]
    private void Closed()
    {
        _cancellationTokenSource.Cancel();
    }
}