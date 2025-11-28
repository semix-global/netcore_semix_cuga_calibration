using System.IO;
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
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationConfigServiceImpl : BaseService<ICgCalibrationService>, ICalibrationConfigService
{
    private IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, int OpticsMagType)>? _prescanChirpAODWaveConfigList;

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

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = GetPrescanChirpDarkFieldAodWaveProfileList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is PrescanAODWaveformProfile && t.OpticsMagType == productivityInformation.OpticsMagType)
            .Select(t => t.AODWaveformProfile)
            .OfType<PrescanAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<PrescanAODWaveformProfile>>(result);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = GetPrescanChirpDarkFieldAodWaveProfileList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is ChirpAODWaveformProfile && t.OpticsMagType == productivityInformation.OpticsMagType)
            .Select(t => t.AODWaveformProfile)
            .OfType<ChirpAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ChirpAODWaveformProfile>>(result);
    }

    public SxExecuteRet<bool> SetPrescanAODWaveProfiles(ProductivityInformation productivityInformation, string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator.PrescanAODWaveformFileExtension, "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        /*var sxExecuteRet = Invoke(() => Service?.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);*/

        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChirpAODWaveProfiles(ProductivityInformation productivityInformation, string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator.ChirpAODWaveformFileExtension, "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        /*var sxExecuteRet = Invoke(() => Service?.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);*/

        throw new NotImplementedException();
    }

    private SxExecuteRet<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, int OpticsMagType)>> GetPrescanChirpDarkFieldAodWaveProfileList()
    {
        if (_prescanChirpAODWaveConfigList is not null) return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigList);

        var sxExecuteRet = Invoke(() => Service!.GetAWGFilePath());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, int OpticsMagType)>>(sxExecuteRet.ErrorMsg, []);

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
            .SelectMany<CgElectrodeFileModel, (AbstractAODWaveformProfile AODWaveformProfile, int OpticsMagType)>(t =>
            [
                (
                    AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanHighFilePath),
                    (int)SxMAGEnum.High
                ),
                (
                    AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanMidFilePath),
                    (int)SxMAGEnum.Mid
                ),
                (
                    AODWaveformProfileFactory.CreatePrescan(t.Id.ToOpticsAODElectrodeEnum(), t.PrescanLowFilePath),
                    (int)SxMAGEnum.Low
                ),
                (
                    AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpHighFilePath),
                    (int)SxMAGEnum.High
                ),
                (
                    AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpMidFilePath),
                    (int)SxMAGEnum.Mid
                ),
                (
                    AODWaveformProfileFactory.CreateChirp(t.Id.ToOpticsAODElectrodeEnum(), t.ChirpLowFilePath),
                    (int)SxMAGEnum.Low
                )
            ])
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigList);
    }
}