using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Models;
using Semix.CoreLib;

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
}