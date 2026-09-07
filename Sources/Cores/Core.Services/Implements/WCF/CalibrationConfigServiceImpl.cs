using CommunityToolkit.Diagnostics;
using Core.Models.Enums.HardwareType;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Config;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Config.Engines;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Exceptions;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton,
    IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationConfigServiceImpl(
    ISysUserRepository sysUserRepository,
    ISysUserService sysUserService) : BaseService<ICgCalibrationService>, ICalibrationConfigService
{
    private IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum
        OpticsIlluminationModeEnum, int OpticsMagType)>? _prescanChirpAODWaveConfigs;

    private HardwareStateConfig? _hardwareStateConfig;

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

    public async Task<SxExecuteRet<SysUserDTO>> LoginAsync(SysUserDTO user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Password))
            throw new LoginException("The account or password cannot be empty!");

        var sxExecuteRet = Invoke(() => Service!.UserCheck(user.UserName, user.Password));
        if (sxExecuteRet.IsSuccess == false) throw new LoginException(sxExecuteRet.Msg);

        var sysUser = await sysUserRepository
                          .Select
                          .Where(t => t.UserName == user.UserName)
                          .ToOneAsync(cancellationToken).ConfigureAwait(false) ??
                      throw new LoginException("The account or password is incorrect!");
        if (sysUser.IsDeleted || sysUser.IsEnabled == false)
            throw new LoginException("The account has been deactivated and login is prohibited!");

        var sysUserDto = await sysUserService.GetAsync(sysUser.Id, cancellationToken).ConfigureAwait(false) ??
                         throw new DbException();

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

    public SxExecuteRet<string> GetDeviceCUGAVersion()
    {
        var sxExecuteRet = Invoke(() => Service!.GetCugaVersion());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, string.Empty);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<string> GetCalibrationFilePath()
    {
        var sxExecuteRet = Invoke(() => Service!.GetCalibrationFilePath());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, string.Empty);

        return SxExecuteRetHelper.CreateSuccess($"{sxExecuteRet.Anything}.dat");
    }

    public SxExecuteRet<IReadOnlyList<SysUserDTO>> GetRegisteredUsersInformation()
    {
        var sxExecuteRet = Invoke(() => Service!.GetUserInfoData());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<SysUserDTO>>(sxExecuteRet.Msg, []);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<SysUserDTO>>([
            .. sxExecuteRet.Anything.Select(t => new SysUserDTO
            {
                Id = t.Id, UserName = t.UserName, Password = t.Password, NickName = t.UserName, Remark = t.UserName
            })
        ]);
    }

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(
        ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = GetPrescanChirpAODWaveConfigs();
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRet.Msg, []);

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

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(
        ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = GetPrescanChirpAODWaveConfigs();
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRet.Msg, []);

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

    public SxExecuteRet<bool> SetPrescanAODWaveformConfiguration(ProductivityInformation productivityInformation,
        string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator1.PrescanAODWaveformFileExtension,
            "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        var sxExecuteRet = Invoke(() => Service!.UpdataAodWavePathConfig(productivityInformation.AdaptTo().Mag,
            productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(), CgWaveType.Prescan, filePath));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChirpAODWaveformConfiguration(ProductivityInformation productivityInformation,
        string filePath)
    {
        Guard.IsEqualTo(Path.GetExtension(filePath), AODWaveformGenerator1.ChirpAODWaveformFileExtension,
            "File Extension is not valid.");
        Guard.IsTrue(File.Exists(filePath), "File is not exists.");

        var sxExecuteRet = Invoke(() => Service!.UpdataAodWavePathConfig(productivityInformation.AdaptTo().Mag,
            productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(), CgWaveType.Chirp, filePath));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double originalHeight, double StartYPixel, double EndYPixel)> GetDefaultImageYPixelHeight(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = Invoke(() => Service?.GetInfoByProductivity(productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(), productivityInformation.AdaptTo().Mag));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, (0d, 0d, 0d));

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    private SxExecuteRet<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum OpticsIlluminationModeEnum, int OpticsMagType)>> GetPrescanChirpAODWaveConfigs()
    {
        if (_prescanChirpAODWaveConfigs is not null)
            return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigs);

        var sxExecuteRet = Invoke(() => Service!.GetAWGFilePath());
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper
                .CreateError<
                    IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum
                        OpticsIlluminationModeEnum, int OpticsMagType)>>(sxExecuteRet.ErrorMsg, []);

        var cgElectrodeFileModels = sxExecuteRet.Anything.OrderBy(t => t.Id).ToList();

        Guard.IsTrue(cgElectrodeFileModels.Count > 0, "Prescan Chirp Config List is empty");
        Guard.IsTrue(cgElectrodeFileModels
            .Select(t => t.Id.ToOpticsAODElectrodeEnum())
            .OrderBy(t => t)
            .SequenceEqual(EnumHelper.Enums<OpticsAODElectrodeEnum>()
                .OrderBy(t => t)
                .ToList()
                .GetRange(0, cgElectrodeFileModels.Count)), "Id is not from 1 to ..");

        var results =
            new List<(AbstractAODWaveformProfile AODWaveformProfile, OpticsIlluminationModeEnum
                OpticsIlluminationModeEnum, int OpticsMagType)>();

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

    public SxExecuteRet<HardwareStateConfig> LoadHardwareConfigs()
    {
        if (_hardwareStateConfig is not null) return SxExecuteRetHelper.CreateSuccess(_hardwareStateConfig);
        var sxExecuteRet = Invoke(() => Service!.GetOpticConfig());
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, HardwareStateConfig.Default);

        var config = new CgOpticsConfig();
        var motorDict = new Dictionary<HardwareMotorTypeEnum, HardwareStateDTO>();
        var fourierDict = new Dictionary<HardwareFourierTypeEnum, HardwareStateDTO>();
        var clinderDict = new Dictionary<HardwareClinderTypeEnum, HardwareStateDTO>();

        // 转换 Motor
        foreach (var kv in config.UsedConfig.UsedConfig)
        {
            var motorTypeEnum = kv.Key.ToHardwareMotorTypeEnum();

            if (motorTypeEnum is null) continue;
            motorDict[(HardwareMotorTypeEnum)motorTypeEnum] = new HardwareStateDTO(kv.Value);
        }

        // 转换 FFT
        foreach (var kv in config.UsedConfig.UsedFFConfig)
        {
            var fourierTypeEnum = kv.Key.ToHardwareFourierTypeEnum();

            if (fourierTypeEnum is null) continue;
            fourierDict[(HardwareFourierTypeEnum)fourierTypeEnum] = new HardwareStateDTO(kv.Value);
        }

        // 转换 Clinder
        foreach (var kv in config.UsedConfig.UsedClinder)
        {
            var clinderTypeEnum = kv.Key.ToHardwareClinderTypeEnum();

            if (clinderTypeEnum is null) continue;
            clinderDict[(HardwareClinderTypeEnum)clinderTypeEnum] = new HardwareStateDTO(kv.Value);
        }

        _hardwareStateConfig = new HardwareStateConfig(motorDict, fourierDict, clinderDict);

        return SxExecuteRetHelper.CreateSuccess(_hardwareStateConfig);
    }
}