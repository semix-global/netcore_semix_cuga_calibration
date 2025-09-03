using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
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
    private IReadOnlyList<CgElectrodeFileModel>? _cgElectrodeFileModels;

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
        var sxExecuteRet = GetCgElectrodeFileModels();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRet.Msg, []);

        return SxExecuteRetHelper.CreateSuccess(opticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low =>
            [
                ..sxExecuteRet.Anything
                    .Select(t => AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanHighFilePath))
            ],
            OpticsMagTypeEnum.Middle =>
            [
                ..sxExecuteRet.Anything
                    .Select(t => AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanMidFilePath))
            ],
            OpticsMagTypeEnum.High =>
            [
                ..sxExecuteRet.Anything
                    .Select(t => AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanLowFilePath))
            ],
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<IReadOnlyList<PrescanAODWaveformProfile>>(nameof(opticsMagTypeEnum))
        });
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = GetCgElectrodeFileModels();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRet.Msg, []);

        return SxExecuteRetHelper.CreateSuccess(opticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low =>
            [
                ..sxExecuteRet.Anything
                    .Select(t => AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpHighFilePath))
            ],
            OpticsMagTypeEnum.Middle =>
            [
                ..sxExecuteRet.Anything
                    .Select(t => AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpMidFilePath))
            ],
            OpticsMagTypeEnum.High =>
            [
                ..sxExecuteRet.Anything
                    .Select(t => AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpLowFilePath))
            ],
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<IReadOnlyList<ChirpAODWaveformProfile>>(nameof(opticsMagTypeEnum))
        });
    }

    private SxExecuteRet<IReadOnlyList<CgElectrodeFileModel>> GetCgElectrodeFileModels()
    {
        if (_cgElectrodeFileModels is not null) return SxExecuteRetHelper.CreateSuccess(_cgElectrodeFileModels);

        var sxExecuteRet = Invoke(() => Service!.GetAWGFilePath());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<CgElectrodeFileModel>>(sxExecuteRet.ErrorMsg, []);

        var result = sxExecuteRet.Anything.OrderBy(t => t.Id).ToArray();

        Guard.IsTrue(result.Length > 0, "Prescan Chirp Config List is empty");
        Guard.IsTrue(result
            .Select(t => t.Id.ToOpticsAODElectrodeEnum())
            .OrderBy(t => t)
            .SequenceEqual(EnumHelper.Enums<OpticsAODElectrodeEnum>()
                .OrderBy(t => t)
                .ToList()
                .GetRange(0, result.Length)), "Id is not from 1 to ..");

        _cgElectrodeFileModels = result;

        return SxExecuteRetHelper.CreateSuccess(_cgElectrodeFileModels);
    }
}