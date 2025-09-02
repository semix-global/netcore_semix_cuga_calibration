using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Utilities;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Semix.CoreLib;
using System.IO;
using Core.Models.Models.Common.AODWaveform.Generates;

#if NET
// ReSharper disable once CheckNamespace
namespace Core.Services.Implements.GRPC;
#else
// ReSharper disable once CheckNamespace
namespace Core.Services.Implements.WCF;
#endif

public sealed partial class CalibrationLaserServiceImpl
{
    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GeneratePrescanAodWaveList(OpticsMagTypeEnum opticsMagTypeEnum, GeneratePrescanAODWaveformParam generatePrescanAODWaveformParam)
    {
        var sxExecuteRetByGetPrescanAODWaveProfileList = calibrationConfigService.GetPrescanAODWaveProfileList(opticsMagTypeEnum);
        if (sxExecuteRetByGetPrescanAODWaveProfileList.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRetByGetPrescanAODWaveProfileList.Msg, []);

        var result = new List<PrescanAODWaveformProfile>();

        foreach (var prescanAODWaveformProfile in sxExecuteRetByGetPrescanAODWaveProfileList.Anything)
        {
            var (isSuccess,
                aodWaveFilePath,
                aodWaveFlatnessLinearFrequencySignals,
                aodWaveFlatnessTotalFrequencySignals,
                aodWaveFlatnessAstigmatismCompensationSignals,
                aodWaveFlatnessSphericalAberrationCompensationSignals,
                aodWaveFlatnessSecondaryAstigmatismCompensationSignals,
                aodWaveFlatnessComaCompensationSignals,
                aodWaveFlatnessTrefoilCompensationSignals,
                aodWaveFlatnessQuadrafoilCompensationSignals,
                aodWaveFlatnessAlphaOrderCompensationSignals,
                aodWaveSignals,
                aodWaveSignalsFourier,
                aodWaveFrequencyAmplitudes,
                exception) = AODWaveformGenerator.GeneratePrescanAodWaveFile(
                generatePrescanAODWaveformParam.BandWidth,
                generatePrescanAODWaveformParam.CenterFrequency,
                generatePrescanAODWaveformParam.FlatnessTime,
                generatePrescanAODWaveformParam.FunctionMonotonicTypeEnum,
                generatePrescanAODWaveformParam.SampleRate,
                generatePrescanAODWaveformParam.Amplitude,
                Path.Combine(generatePrescanAODWaveformParam.AodWaveDirectory, EnumHelper.ToDescriptionString(prescanAODWaveformProfile.OpticsAODElectrodeEnum)),
                zeroSampleCount: generatePrescanAODWaveformParam.ZeroSampleCount,
                endpointSampleCount: generatePrescanAODWaveformParam.EndpointSampleCount,
                offsetFrequency: prescanAODWaveformProfile.OffsetFrequency,
                offsetFrequencyPeriodMultiple: prescanAODWaveformProfile.OffsetFrequencyPeriodMultiple,
                sincCoefficient: generatePrescanAODWaveformParam.SincCoefficient,
                astigmatismCompensationCoefficient: generatePrescanAODWaveformParam.AstigmatismCompensationCoefficient,
                sphericalAberrationCompensationCoefficient: generatePrescanAODWaveformParam.SphericalAberrationCompensationCoefficient,
                secondaryAstigmatismCompensationCoefficient: generatePrescanAODWaveformParam.SecondaryAstigmatismCompensationCoefficient,
                comaCompensationCoefficient: generatePrescanAODWaveformParam.ComaCompensationCoefficient,
                trefoilCompensationCoefficient: generatePrescanAODWaveformParam.TrefoilCompensationCoefficient,
                quadrafoilCompensationCoefficient: generatePrescanAODWaveformParam.QuadrafoilCompensationCoefficient,
                alphaOrder: generatePrescanAODWaveformParam.AlphaOrder,
                alphaOrderCoefficient: generatePrescanAODWaveformParam.AlphaOrderCoefficient,
                frequencyAmplitudes: generatePrescanAODWaveformParam.FrequencyAmplitudes,
                generateRetryTimes: generatePrescanAODWaveformParam.GenerateRetryTimes);

            if (isSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(GuardUtils.IsNotNullAndReturn(exception).Message, []);

            result.Add(AODWaveformProfileFactory.CreatePrescan(prescanAODWaveformProfile.OpticsAODElectrodeEnum, aodWaveFilePath));
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<PrescanAODWaveformProfile>>(result);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GenerateChirpAodWaveList(OpticsMagTypeEnum opticsMagTypeEnum, GenerateChirpAODWaveformParam generateChirpAODWaveformParam)
    {
        var sxExecuteRetByGetChirpAODWaveProfileList = calibrationConfigService.GetChirpAODWaveProfileList(opticsMagTypeEnum);
        if (sxExecuteRetByGetChirpAODWaveProfileList.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRetByGetChirpAODWaveProfileList.Msg, []);

        var result = new List<ChirpAODWaveformProfile>();

        foreach (var chirpAODWaveformProfile in sxExecuteRetByGetChirpAODWaveProfileList.Anything)
        {
            var (isSuccess,
                aodWaveFilePath,
                aodWaveFlatnessLinearFrequencySignals,
                aodWaveFlatnessTotalFrequencySignals,
                aodWaveFlatnessAstigmatismCompensationSignals,
                aodWaveFlatnessSphericalAberrationCompensationSignals,
                aodWaveFlatnessSecondaryAstigmatismCompensationSignals,
                aodWaveFlatnessComaCompensationSignals,
                aodWaveFlatnessTrefoilCompensationSignals,
                aodWaveFlatnessQuadrafoilCompensationSignals,
                aodWaveFlatnessAlphaOrderCompensationSignals,
                aodWaveSignals,
                aodWaveSignalsFourier,
                aodWaveFrequencyAmplitudes,
                exception) = AODWaveformGenerator.GenerateChirpAodWaveFile(
                generateChirpAODWaveformParam.BandWidth,
                generateChirpAODWaveformParam.CenterFrequency,
                generateChirpAODWaveformParam.SoundPackageLength,
                generateChirpAODWaveformParam.FunctionMonotonicTypeEnum,
                generateChirpAODWaveformParam.SampleRate,
                generateChirpAODWaveformParam.Amplitude,
                Path.Combine(generateChirpAODWaveformParam.AodWaveDirectory, EnumHelper.ToDescriptionString(chirpAODWaveformProfile.OpticsAODElectrodeEnum)),
                zeroSampleCount: generateChirpAODWaveformParam.ZeroSampleCount,
                endpointSampleCount: generateChirpAODWaveformParam.EndpointSampleCount,
                offsetFrequency: chirpAODWaveformProfile.OffsetFrequency,
                offsetFrequencyPeriodMultiple: chirpAODWaveformProfile.OffsetFrequencyPeriodMultiple,
                sincCoefficient: generateChirpAODWaveformParam.SincCoefficient,
                astigmatismCompensationCoefficient: generateChirpAODWaveformParam.AstigmatismCompensationCoefficient,
                sphericalAberrationCompensationCoefficient: generateChirpAODWaveformParam.SphericalAberrationCompensationCoefficient,
                secondaryAstigmatismCompensationCoefficient: generateChirpAODWaveformParam.SecondaryAstigmatismCompensationCoefficient,
                comaCompensationCoefficient: generateChirpAODWaveformParam.ComaCompensationCoefficient,
                trefoilCompensationCoefficient: generateChirpAODWaveformParam.TrefoilCompensationCoefficient,
                quadrafoilCompensationCoefficient: generateChirpAODWaveformParam.QuadrafoilCompensationCoefficient,
                alphaOrder: generateChirpAODWaveformParam.AlphaOrder,
                alphaOrderCoefficient: generateChirpAODWaveformParam.AlphaOrderCoefficient,
                frequencyAmplitudes: generateChirpAODWaveformParam.FrequencyAmplitudes,
                generateRetryTimes: generateChirpAODWaveformParam.GenerateRetryTimes);

            if (isSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(GuardUtils.IsNotNullAndReturn(exception).Message, []);

            result.Add(AODWaveformProfileFactory.CreateChirp(chirpAODWaveformProfile.OpticsAODElectrodeEnum, aodWaveFilePath));
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ChirpAODWaveformProfile>>(result);
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