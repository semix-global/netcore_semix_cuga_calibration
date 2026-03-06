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
using System.IO;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Exceptions;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationConfigServiceImpl(
    ISysUserRepository sysUserRepository,
    ISysUserService sysUserService) : BaseService<ICgCalibrationService>, ICalibrationConfigService
{
    private IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum OpticsIlluminationModeEnum, int OpticsMagType)>? _prescanChirpAODWaveConfigs;

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

    public async Task<SxExecuteRet<SysUserDto>> LoginAsync(SysUserDto user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Password)) throw new LoginException("The account or password cannot be empty!");

        var sxExecuteRet = Invoke(() => Service!.UserCheck(user.UserName, user.Password));
        if (sxExecuteRet.IsSuccess == false) throw new LoginException(sxExecuteRet.Msg);

        var sysUser = await sysUserRepository
            .Select
            .Where(t => t.UserName == user.UserName)
            .ToOneAsync(cancellationToken).ConfigureAwait(false) ?? throw new LoginException("The account or password is incorrect!");
        if (sysUser.IsDeleted || sysUser.IsEnabled == false) throw new LoginException("The account has been deactivated and login is prohibited!");

        var sysUserDto = await sysUserService.GetAsync(sysUser.Id, cancellationToken).ConfigureAwait(false) ?? throw new DbException();

        sysUserDto.LoginDate = DateTime.Now;
        var isSuccess = await sysUserService.UpdateAsync(sysUserDto, cancellationToken).ConfigureAwait(false);
        if (isSuccess == false) throw new LoginException("Login time write failed");

        return SxExecuteRetHelper.CreateSuccess(sysUserDto);
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
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, string.Empty);

        return SxExecuteRetHelper.CreateSuccess($"{sxExecuteRet.Anything}.dat");
    }

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = GetPrescanChirpAODWaveConfigs();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is PrescanAODWaveformProfile
                        && t.OpticsIlluminationModeEnum == productivityInformation.OpticsIlluminationModeEnum
                        && t.OpticsMagType == productivityInformation.OpticsMagType)
            .Select(t => t.AODWaveformProfile)
            .OfType<PrescanAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<PrescanAODWaveformProfile>>(result);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = GetPrescanChirpAODWaveConfigs();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is ChirpAODWaveformProfile
                        && t.OpticsIlluminationModeEnum == productivityInformation.OpticsIlluminationModeEnum
                        && t.OpticsMagType == productivityInformation.OpticsMagType)
            .Select(t => t.AODWaveformProfile)
            .OfType<ChirpAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ChirpAODWaveformProfile>>(result);
    }

    public SxExecuteRet<bool> SetPrescanAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator1.PrescanAODWaveformFileExtension, "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        var sxExecuteRet = Invoke(() => Service!.UpdataAodWavePathConfig(productivityInformation.AdaptTo().Mag, productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(), CgWaveType.Prescan, filePath));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChirpAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator1.ChirpAODWaveformFileExtension, "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        var sxExecuteRet = Invoke(() => Service!.UpdataAodWavePathConfig(productivityInformation.AdaptTo().Mag, productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(), CgWaveType.Chirp, filePath));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    private SxExecuteRet<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum OpticsIlluminationModeEnum, int OpticsMagType)>> GetPrescanChirpAODWaveConfigs()
    {
        if (_prescanChirpAODWaveConfigs is not null) return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigs);

        var sxExecuteRet = Invoke(() => Service!.GetAWGFilePath());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum OpticsIlluminationModeEnum, int OpticsMagType)>>(sxExecuteRet.ErrorMsg, []);

        var cgElectrodeFileModels = sxExecuteRet.Anything.OrderBy(t => t.Id).ToList();

        Guard.IsTrue(cgElectrodeFileModels.Count > 0, "Prescan Chirp Config List is empty");
        Guard.IsTrue(cgElectrodeFileModels
            .Select(t => t.Id.ToOpticsAODElectrodeEnum())
            .OrderBy(t => t)
            .SequenceEqual(EnumHelper.Enums<OpticsAODElectrodeEnum>()
                .OrderBy(t => t)
                .ToList()
                .GetRange(0, cgElectrodeFileModels.Count)), "Id is not from 1 to ..");

        var results = new List<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum OpticsIlluminationModeEnum, int OpticsMagType)>();

        foreach (var cgElectrodeFileModel in cgElectrodeFileModels)
        {
            foreach (var opticsIlluminationModeEnumKvp in cgElectrodeFileModel.PrescanFilePaths)
            {
                var opticsIlluminationModeEnum = opticsIlluminationModeEnumKvp.Key.ToOpticsIlluminationModeEnum();
                //if (opticsIlluminationModeEnum is OpticsIlluminationModeEnum.NI) continue;
                foreach (var opticsMagTypeKvp in opticsIlluminationModeEnumKvp.Value)
                {
                    var opticsMagType = (int)opticsMagTypeKvp.Key.ToSxMagEnum();

                    var prescanAODWaveformProfile = AODWaveformProfileFactory.CreatePrescan(
                        cgElectrodeFileModel.Id.ToOpticsAODElectrodeEnum(),
                        opticsMagTypeKvp.Value);

                    results.Add((prescanAODWaveformProfile, opticsIlluminationModeEnum, opticsMagType));
                }
            }

            foreach (var opticsIlluminationModeEnumKvp in cgElectrodeFileModel.ChirpFilePaths)
            {
                var opticsIlluminationModeEnum = opticsIlluminationModeEnumKvp.Key.ToOpticsIlluminationModeEnum();
                //if (opticsIlluminationModeEnum is OpticsIlluminationModeEnum.NI) continue;
                foreach (var opticsMagTypeKvp in opticsIlluminationModeEnumKvp.Value)
                {
                    var opticsMagType = (int)opticsMagTypeKvp.Key.ToSxMagEnum();

                    var chirpAODWaveformProfile = AODWaveformProfileFactory.CreateChirp(
                        cgElectrodeFileModel.Id.ToOpticsAODElectrodeEnum(),
                        opticsMagTypeKvp.Value);

                    results.Add((chirpAODWaveformProfile, opticsIlluminationModeEnum, opticsMagType));
                }
            }
        }

        _prescanChirpAODWaveConfigs = results;

        return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigs);
    }
}