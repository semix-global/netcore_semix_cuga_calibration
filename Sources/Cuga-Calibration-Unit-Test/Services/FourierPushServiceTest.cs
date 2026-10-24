#if NETFRAMEWORK
using AwesomeAssertions;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Optics;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace CugaCalibrationUnitTest.Services;

[Collection(HostCollection.Name)]
public sealed class FourierPushServiceTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Home_ShouldUseHardwareSpecificChannel3Motors(bool isGoToZero)
    {
        using var scope = new ServiceScope();

        var result = scope.Service.Home(3, isGoToZero);

        result.IsSuccess.Should().BeTrue();
#if B3
        scope.Native.HomedChannels.Should().Equal(FFCH.Ch3_X, FFCH.Ch3_Y);
#elif B4
        scope.Native.HomedChannels.Should().Equal(FFCH.Ch3_X);
#endif
        if (isGoToZero == false)
        {
            scope.Native.Calls.Should().OnlyContain(t => t == "SetFFHome");
            scope.Native.Moves.Should().BeEmpty();

            return;
        }

#if B3
        scope.Native.Moves.Should().Equal(
            ("FF_Move_RPOS_CH3", FFCH.Ch3_X, 0d),
            ("FF_Move_LPOS_CH3", FFCH.Ch3_X, 0d),
            ("FF_Move_RPOS_CH3", FFCH.Ch3_Y, 0d),
            ("FF_Move_LPOS_CH3", FFCH.Ch3_Y, 0d),
            ("FF_Move_PPOS_CH3", FFCH.Ch3_X, 0d));
#elif B4
        scope.Native.Moves.Should().Equal(
            ("FF_Move_RPOS_CH3", FFCH.Ch3_X, 0d),
            ("FF_Move_LPOS_CH3", FFCH.Ch3_X, 0d),
            ("FF_Move_RFPOS_CH3", FFCH.Ch3_X, 0d),
            ("FF_Move_DFPOS_CH3", FFCH.Ch3_X, 0d));
#endif
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void Home_ShouldKeepSideChannelHoming(int channelId, bool isGoToZero)
    {
        using var scope = new ServiceScope();

        var result = scope.Service.Home(channelId, isGoToZero);

        result.IsSuccess.Should().BeTrue();
        scope.Native.HomedChannels.Should().Equal(channelId == 1 ? FFCH.Ch1 : FFCH.Ch2);
        scope.Native.Calls.Should().Equal("SetFFHome");
        scope.Native.Moves.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Home_ShouldReturnHomingErrorWithoutMoving(int channelId)
    {
        using var scope = new ServiceScope();
        scope.Native.HomeErrorMessage = "回零失败";

        var result = scope.Service.Home(channelId, true);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Be(scope.Native.HomeErrorMessage);
        scope.Native.Calls.Should().Equal("SetFFHome");
        scope.Native.Moves.Should().BeEmpty();
    }

    [Fact]
    public void Configuration_ShouldMapLegacyReflectionAndDiffractionRanges()
    {
        using var scope = new ServiceScope();
        scope.Native.MotorConfiguration = new CgFFRangeModel
        {
            FFCH12_Range = new CgFF_CH12RangeModel { MinPos = -1, MaxPos = 54 },
            FFCH3_XRange = new CgFF_CH3RangeModel
            {
                LPOSMin = -21,
                LPOSMax = 21,
                PPOSMin = 0,
                PPOSMax = 60,
                RFPOSMin = -30,
                RFPOSMax = 45,
                DFPOSMin = -70,
                DFPOSMax = 80
            },
            FFCH3_YRange = new CgFF_CH3RangeModel { LPOSMin = -22, LPOSMax = 23 },
            FF_CH1_2RodNum = 46
        };

        var result = scope.Service.GetMotorConfiguration();

        result.IsSuccess.Should().BeTrue();
        result.Anything.SideRodTotalCount.Should().Be(46);
        result.Anything.SideMinMotorAbsoluteValue.Should().Be(-1d);
        result.Anything.SideMaxMotorAbsoluteValue.Should().Be(54d);
        result.Anything.CenterXMinMotorAbsoluteValue.Should().Be(-21d);
        result.Anything.CenterXMaxMotorAbsoluteValue.Should().Be(21d);
        result.Anything.CenterPushMinMotorAbsoluteValue.Should().Be(0d);
        result.Anything.CenterPushMaxMotorAbsoluteValue.Should().Be(60d);
        result.Anything.CenterYMinMotorAbsoluteValue.Should().Be(-22d);
        result.Anything.CenterYMaxMotorAbsoluteValue.Should().Be(23d);
        result.Anything.CenterReflectionPushMinMotorAbsoluteValue.Should().Be(-30d);
        result.Anything.CenterReflectionPushMaxMotorAbsoluteValue.Should().Be(45d);
        result.Anything.CenterDiffractionPushMinMotorAbsoluteValue.Should().Be(-70d);
        result.Anything.CenterDiffractionPushMaxMotorAbsoluteValue.Should().Be(80d);
        scope.Native.Calls.Should().Equal("GetFFTConfig");
    }

    [Fact]
    public void Configuration_ShouldReturnReadError()
    {
        using var scope = new ServiceScope();
        scope.Native.ConfigurationErrorMessage = "读取配置失败";

        var result = scope.Service.GetMotorConfiguration();

        result.IsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Be(scope.Native.ConfigurationErrorMessage);
    }

    [Fact]
    public void Configuration_ShouldRejectMissingConfiguration()
    {
        using var scope = new ServiceScope();

        var result = scope.Service.GetMotorConfiguration();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void LegacyPush_ShouldKeepUsingPPOSAndPACT()
    {
        using var scope = new ServiceScope();
        scope.Native.ActualPosition = 25d;

        var result = scope.Service.SetChannel3XPush(25d);

        result.IsSuccess.Should().BeTrue();
        scope.Native.Calls.Should().Equal("FF_Move_PPOS_CH3", "FF_GetPACT");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Move_ShouldUseDedicatedCommandAndReadback(bool isDiffraction)
    {
        using var scope = new ServiceScope();
        scope.Native.ActualPosition = 1.25;

        var result = Move(scope.Service, isDiffraction, 1.25);

        result.IsSuccess.Should().BeTrue();
        scope.Native.Calls.Should().Equal(GetMoveMethodName(isDiffraction), GetReadMethodName(isDiffraction));
        scope.Native.Channel.Should().Be(FFCH.Ch3_X);
        scope.Native.RequestedPosition.Should().Be(1.25);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Move_ShouldReturnCommandErrorWithoutReadingPosition(bool isDiffraction)
    {
        using var scope = new ServiceScope();
        scope.Native.MoveErrorMessage = "移动失败";

        var result = Move(scope.Service, isDiffraction, 1.25);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Be(scope.Native.MoveErrorMessage);
        scope.Native.Calls.Should().Equal(GetMoveMethodName(isDiffraction));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Move_ShouldReturnReadbackError(bool isDiffraction)
    {
        using var scope = new ServiceScope();
        scope.Native.ReadErrorMessage = "回读失败";

        var result = Move(scope.Service, isDiffraction, 1.25);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Be(scope.Native.ReadErrorMessage);
        scope.Native.Calls.Should().Equal(GetMoveMethodName(isDiffraction), GetReadMethodName(isDiffraction));
    }

    [Theory]
    [InlineData(false, 0.75)]
    [InlineData(true, 0.75)]
    [InlineData(false, double.NaN)]
    [InlineData(true, double.NaN)]
    [InlineData(false, double.PositiveInfinity)]
    [InlineData(true, double.PositiveInfinity)]
    public void Move_ShouldRejectInvalidOrFractionallyDifferentReadback(bool isDiffraction, double actualPosition)
    {
        using var scope = new ServiceScope();
        scope.Native.ActualPosition = actualPosition;

        var result = Move(scope.Service, isDiffraction, 0.25);

        result.IsSuccess.Should().BeFalse();
        scope.Native.Calls.Should().Equal(GetMoveMethodName(isDiffraction), GetReadMethodName(isDiffraction));
    }

    [Theory]
    [InlineData(false, double.NaN)]
    [InlineData(true, double.NaN)]
    [InlineData(false, double.PositiveInfinity)]
    [InlineData(true, double.NegativeInfinity)]
    public void Move_ShouldRejectNonFiniteTargetsBeforeCallingSDK(bool isDiffraction, double motorAbsoluteValue)
    {
        using var scope = new ServiceScope();

        var result = Move(scope.Service, isDiffraction, motorAbsoluteValue);

        result.IsSuccess.Should().BeFalse();
        scope.Native.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Move_ShouldReturnErrorWhenDisconnected(bool isDiffraction)
    {
        using var scope = new ServiceScope();
        scope.Disconnect();

        var result = Move(scope.Service, isDiffraction, 1.25);

        result.IsSuccess.Should().BeFalse();
        scope.Native.Calls.Should().BeEmpty();
    }

    private static SxExecuteRet<bool> Move(ICalibrationFourierService service, bool isDiffraction, double motorAbsoluteValue)
    {
        return isDiffraction
            ? service.SetChannel3XDiffractionPush(motorAbsoluteValue)
            : service.SetChannel3XReflectionPush(motorAbsoluteValue);
    }

    private static string GetMoveMethodName(bool isDiffraction) => isDiffraction ? "FF_Move_DFPOS_CH3" : "FF_Move_RFPOS_CH3";

    private static string GetReadMethodName(bool isDiffraction) => isDiffraction ? "GetDFPOS" : "GetRFPOS";

    private sealed class ServiceScope : IDisposable
    {
        private readonly PropertyInfo _serviceProperty;
        private readonly object? _originalService;

        public ICalibrationFourierService Service { get; }

        public NativeServiceProxy Native { get; }

        public ServiceScope()
        {
            var serviceType = typeof(ICalibrationFourierService).Assembly.GetType("Core.Services.Implements.WCF.CalibrationFourierServiceImpl") ?? throw new InvalidOperationException("未找到 WCF 傅里叶服务类型.");
            var baseType = serviceType.BaseType ?? throw new InvalidOperationException("未找到 WCF 服务基类.");
            _serviceProperty = baseType.GetProperty("Service", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException("未找到 WCF 服务属性.");
            _originalService = _serviceProperty.GetValue(null);
            Service = Activator.CreateInstance(serviceType) as ICalibrationFourierService ?? throw new InvalidOperationException("无法创建 WCF 傅里叶服务.");
            Native = new NativeServiceProxy(baseType.GenericTypeArguments.Single());
            _serviceProperty.SetValue(null, Native.GetTransparentProxy());
        }

        public void Disconnect() => _serviceProperty.SetValue(null, null);

        public void Dispose() => _serviceProperty.SetValue(null, _originalService);
    }

    private sealed class NativeServiceProxy(Type interfaceType) : RealProxy(interfaceType)
    {
        public List<string> Calls { get; } = [];

        public List<FFCH> HomedChannels { get; } = [];

        public List<(string MethodName, FFCH Channel, double Position)> Moves { get; } = [];

        public object? Channel { get; private set; }

        public double RequestedPosition { get; private set; }

        public double ActualPosition { get; set; }

        public string HomeErrorMessage { get; set; } = string.Empty;

        public string MoveErrorMessage { get; set; } = string.Empty;

        public string ReadErrorMessage { get; set; } = string.Empty;

        public CgFFRangeModel? MotorConfiguration { get; set; }

        public string ConfigurationErrorMessage { get; set; } = string.Empty;

        public override IMessage Invoke(IMessage message)
        {
            var call = message as IMethodCallMessage ?? throw new InvalidOperationException("调用消息类型无效.");
            Calls.Add(call.MethodName);
            object result;
            switch (call.MethodName)
            {
                case "SetFFHome":
                    HomedChannels.Add(call.Args[0] is FFCH homeChannel ? homeChannel : throw new InvalidOperationException("回零通道参数类型无效."));
                    result = new SxExecuteRet { Success = string.IsNullOrEmpty(HomeErrorMessage), Msg = HomeErrorMessage };

                    break;

                case "FF_Move_RPOS_CH3":
                case "FF_Move_LPOS_CH3":
                case "FF_Move_RFPOS_CH3":
                case "FF_Move_DFPOS_CH3":
                case "FF_Move_PPOS_CH3":
                    Channel = call.Args[0];
                    RequestedPosition = call.Args[1] is double position ? position : throw new InvalidOperationException("推杆位置参数类型无效.");
                    var moveChannel = call.Args[0] is FFCH channel ? channel : throw new InvalidOperationException("移动通道参数类型无效.");
                    Moves.Add((call.MethodName, moveChannel, RequestedPosition));
                    result = new SxExecuteRet { Success = string.IsNullOrEmpty(MoveErrorMessage), Msg = MoveErrorMessage };

                    break;

                case "GetRFPOS":
                case "GetDFPOS":
                case "FF_GetPACT":
                case "FF_GetRACT":
                case "FF_GetLACT":
                    result = new SxExecuteRet<double> { Success = string.IsNullOrEmpty(ReadErrorMessage), Msg = ReadErrorMessage, Anything = ActualPosition };

                    break;

                case "GetFFTConfig":
                    var configurationResult = new SxExecuteRet<CgFFRangeModel> { Success = string.IsNullOrEmpty(ConfigurationErrorMessage), Msg = ConfigurationErrorMessage };
                    if (MotorConfiguration is { } configuration) configurationResult.Anything = configuration;
                    result = configurationResult;

                    break;

                default:
                    throw new NotSupportedException(call.MethodName);
            }

            return new ReturnMessage(result, null, 0, call.LogicalCallContext, call);
        }
    }
}
#endif