using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationConfigServiceImpl : BaseService<ICgCalibrationService>, ICalibrationConfigService
{
    private IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsMagTypeEnum OpticsMagTypeEnum)>? _prescanChirpAODWaveConfigList;

    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<string> GetDeviceCode()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadDeviceCode());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, string.Empty);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<string> GetCalibrationFilePath()
    {
        var sxExecuteRet = Invoke(() => Service!.GetCalibrationFilePath());
        return SxExecuteRetHelper.CreateSuccess($"{sxExecuteRet.Anything}.dat");
    }

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = GetPrescanChirpDarkFieldAodWaveProfileList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is PrescanAODWaveformProfile && t.OpticsMagTypeEnum == opticsMagTypeEnum)
            .Select(t => t.AODWaveformProfile)
            .OfType<PrescanAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<PrescanAODWaveformProfile>>(result);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = GetPrescanChirpDarkFieldAodWaveProfileList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is ChirpAODWaveformProfile && t.OpticsMagTypeEnum == opticsMagTypeEnum)
            .Select(t => t.AODWaveformProfile)
            .OfType<ChirpAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ChirpAODWaveformProfile>>(result);
    }

    private SxExecuteRet<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsMagTypeEnum OpticsMagTypeEnum)>> GetPrescanChirpDarkFieldAodWaveProfileList()
    {
        if (_prescanChirpAODWaveConfigList is not null) return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigList);

        var sxExecuteRet = Invoke(() => Service!.GetAWGFilePath());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsMagTypeEnum OpticsMagTypeEnum)>>(sxExecuteRet.ErrorMsg, []);

        var result = sxExecuteRet.Anything.OrderBy(t => t.Id).ToList();

        Guard.IsTrue(result.Count > 0, "Prescan Chirp Config List is empty");
        Guard.IsTrue(result
            .Select(t => t.Id.ToOpticsAODElectrodeEnum())
            .OrderBy(t => t)
            .SequenceEqual(EnumHelper.Enums<OpticsAODElectrodeEnum>()
                .OrderBy(t => t)
                .ToList()
                .GetRange(0, result.Count)), "Id is not from 1 to ..");

        _prescanChirpAODWaveConfigList = result
            .SelectMany<CgElectrodeFileModel, (AbstractAODWaveformProfile AODWaveformProfile, OpticsMagTypeEnum OpticsMagTypeEnum)>(t =>
            [
                (
                    AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanHighFilePath),
                    OpticsMagTypeEnum.High
                ),
                (
                    AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanMidFilePath),
                    OpticsMagTypeEnum.Middle
                ),
                (
                    AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanLowFilePath),
                    OpticsMagTypeEnum.Low
                ),
                (
                    AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpHighFilePath),
                    OpticsMagTypeEnum.High
                ),
                (
                    AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpMidFilePath),
                    OpticsMagTypeEnum.Middle
                ),
                (
                    AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpLowFilePath),
                    OpticsMagTypeEnum.Low
                )
            ])
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigList);
    }

    public SxExecuteRet<SwathSpeedInformation> GetSwathSpeedInformation(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = Invoke(() => Service?.GetSpeedInfo(productivityInformation.AdaptTo().Mag));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<SwathSpeedInformation>(sxExecuteRet.ErrorMsg, new());
        return SxExecuteRetHelper.CreateSuccess(new SwathSpeedInformation().AdaptIn(sxExecuteRet.Anything));
    }
}