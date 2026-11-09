using Core.Algorithm.Implements.Halcon;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.BestFocus;
using Core.Algorithm.Models.BestFocus;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.DarkField;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace Core.Algorithm.Implements.Mock.Halcon;

/// <summary>
/// Halcon Best Focus 引擎 Mock：固定 raw 资产替换输入后走真实引擎（原 CalibrationAlgorithmServiceMockImpl.GetBestFocus 行为）
/// </summary>
public sealed class HalconBestFocusEngineMock : IBestFocusEngine
{
    private readonly HalconBestFocusEngine _engine = new(NullLogger<HalconBestFocusEngine>.Instance);

    public AlgorithmResultRet<BestFocus> Excute(DarkFieldImageDTO darkFieldImage, BestFocusEngineParam param, Guid guid)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw");
        using var mockDarkFieldImage = new DarkFieldImageDTO
        {
            RawImageFilePath = filePath,
            RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog,
            IsKeepRawImageCIBProfileModeEnum = false
        };

        return _engine.Excute(mockDarkFieldImage, param, guid);
    }
}
