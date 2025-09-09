using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Utilities;
using Net.Utilities.Models;
using Semix.CoreLib;
using System.IO;

#if NET
// ReSharper disable once CheckNamespace
namespace Core.Services.Implements.GRPC;
#else
// ReSharper disable once CheckNamespace
namespace Core.Services.Implements.WCF;
#endif

public sealed partial class CalibrationLaserServiceImpl
{
    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GeneratePrescanAodWaves(GeneratePrescanAODWaveformParam generatePrescanAODWaveformParam)
    {
        var sxExecuteRetByGetPrescanAODWaveProfiles = calibrationConfigService.GetPrescanAODWaveProfiles(generatePrescanAODWaveformParam.OpticsMagTypeEnum);
        if (sxExecuteRetByGetPrescanAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRetByGetPrescanAODWaveProfiles.Msg, []);

        generatePrescanAODWaveformParam.ElectrodeConfigurations =
        [
            .. sxExecuteRetByGetPrescanAODWaveProfiles.Anything
                .Select(t => new GenerateAODWaveformElectrodeConfiguration().AdaptIn(t))
        ];

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(generatePrescanAODWaveformParam.AdaptTo(), cancellationTokenSource.Token);
        var results = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);

        return aodWaveformResult.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess(results)
            : SxExecuteRetHelper.CreateError(GuardUtils.IsNotNullAndReturn(exception).Message, results);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GenerateChirpAodWaves(GenerateChirpAODWaveformParam generateChirpAODWaveformParam)
    {
        var sxExecuteRetByGetChirpAODWaveProfiles = calibrationConfigService.GetChirpAODWaveProfiles(generateChirpAODWaveformParam.OpticsMagTypeEnum);
        if (sxExecuteRetByGetChirpAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRetByGetChirpAODWaveProfiles.Msg, []);

        generateChirpAODWaveformParam.ElectrodeConfigurations =
        [
            .. sxExecuteRetByGetChirpAODWaveProfiles.Anything
                .Select(t => new GenerateAODWaveformElectrodeConfiguration().AdaptIn(t))
        ];

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(generateChirpAODWaveformParam.AdaptTo(), cancellationTokenSource.Token);
        var results = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);

        return aodWaveformResult.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess(results)
            : SxExecuteRetHelper.CreateError(GuardUtils.IsNotNullAndReturn(exception).Message, results);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByCustomFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var chirpAodWaveDto = ResolveAodFileNameToDarkFieldChirpAodWaveDto(fileName).Clone();
        chirpAodWaveDto.IncrementChirpAodFilePath = filePath;

        var resultString = File.ReadAllLines(filePath).Select(t => t.Trim()).Where(t => string.IsNullOrWhiteSpace(t) == false).ToList();
        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) throw new ArgumentException("filePath value error.");

        chirpAodWaveDto.ChirpAodWaveList = [.. resultString.Select(str => Convert.ToInt16(str, 16))];

        var result = new List<byte>();
        foreach (var compArray in chirpAodWaveDto.ChirpAodWaveList.Select(BitConverter.GetBytes))
        {
            result.Add(compArray[1]);
            result.Add(compArray[0]);
        }

        chirpAodWaveDto.ChirpAodWaveByteList = result;
        //chirpAodWaveDto.OriRegNum=(short)chirpAodWaveDto.ChirpAodWaveList.Count;
        return SxExecuteRetHelper.CreateSuccess(chirpAodWaveDto);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> GetChirpAodByChangeRateFromFile(DarkFieldChirpAodWaveDto currentDarkFieldChirpAodWaveDto, double rateChange)
    {
        var chirpAodWaveDto = new DarkFieldChirpAodWaveDto
        {
            SoundPackageLength = currentDarkFieldChirpAodWaveDto.SoundPackageLength,
            CenterFrequency = currentDarkFieldChirpAodWaveDto.CenterFrequency,
            ZeroNum = currentDarkFieldChirpAodWaveDto.ZeroNum,
            RateChange = rateChange,
            IncrementChirpAodFilePath = currentDarkFieldChirpAodWaveDto.IncrementChirpAodFilePath
        };

        var files = Directory.GetFiles(Path.GetDirectoryName(currentDarkFieldChirpAodWaveDto.IncrementChirpAodFilePath)!, "*.txt", SearchOption.AllDirectories);

        var aodFilePath = files
            .Select(t => (FilePath: t, Result: ResolveAodFileNameToDarkFieldChirpAodWaveDto(Path.GetFileName(t))))
            .Where(t => Math.Abs(t.Result.RateChange - rateChange) <= 0.00001 //  0.00001为防止double精度丢失的问题对比不一致
                        && t.Result.SoundPackageLength - currentDarkFieldChirpAodWaveDto.SoundPackageLength == 0
                        && t.Result.CenterFrequency - chirpAodWaveDto.CenterFrequency <= 0.01) // 0.01为防止double精度丢失的问题对比不一致
            /*&& ((t.Result.BandWidthHigh- t.Result.BandWidthLow)/2+ t.Result.BandWidthLow- chirpAodWaveDto.CenterFrequency)<=0.01)*/
            .Select(t => t.FilePath)
            .FirstOrDefault();

        return aodFilePath is not null
            ? ReadChirpAodByCustomFile(aodFilePath)
            : SxExecuteRetHelper.CreateError("Chirp Aod File Is Not Exists Current Input Params", chirpAodWaveDto);
    }

    private static DarkFieldChirpAodWaveDto ResolveAodFileNameToDarkFieldChirpAodWaveDto(string fileName)
    {
        var chirpAodWaveDto = new DarkFieldChirpAodWaveDto();
        var strAry = fileName.Split('_');
        if (strAry is null || strAry.Length < 11) throw new ArgumentException("Chirp Aod File Path is Error.");
        var soundPackageLength = double.Parse(strAry[1].Replace("mm", string.Empty));
        var bandWidthHigh = double.Parse(strAry[3].Replace("Mhz", string.Empty));
        var bandWidthLow = double.Parse(strAry[4].Replace("Mhz", string.Empty));
        chirpAodWaveDto.OriRegNum = short.Parse(strAry.Last().Split('$')[2]);
        chirpAodWaveDto.BandWidthHigh = bandWidthHigh;
        chirpAodWaveDto.BandWidthLow = bandWidthLow;
        chirpAodWaveDto.SoundPackageLength = soundPackageLength;
        chirpAodWaveDto.BandWidth = bandWidthHigh - bandWidthLow;
        chirpAodWaveDto.CenterFrequency = bandWidthLow + chirpAodWaveDto.BandWidth / 2;
        return chirpAodWaveDto;
    }
}