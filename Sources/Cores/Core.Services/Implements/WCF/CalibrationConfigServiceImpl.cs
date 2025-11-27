using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;
using System.IO;
using System.Linq;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationConfigService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationConfigServiceImpl() : BaseService<ICgCalibrationService>, ICalibrationConfigService
{
    private IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum)>? _prescanChirpAODWaveConfigList;
    private IReadOnlyList<ProductivityInformation>? _productivityInformations;


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

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum)
    {
        var sxExecuteRet = GetPrescanChirpDarkFieldAodWaveProfileList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<PrescanAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is PrescanAODWaveformProfile && t.productivityInformation.OpticsMagType== productivityInformation.OpticsMagType&& t.opticsIncidentModeEnum == opticsIncidentModeEnum)
            .Select(t => t.AODWaveformProfile)
            .OfType<PrescanAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<PrescanAODWaveformProfile>>(result);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum)
    {
        var sxExecuteRet = GetPrescanChirpDarkFieldAodWaveProfileList();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ChirpAODWaveformProfile>>(sxExecuteRet.Msg, []);

        var result = sxExecuteRet.Anything
            .Where(t => t.AODWaveformProfile is ChirpAODWaveformProfile && t.productivityInformation.OpticsMagType == productivityInformation.OpticsMagType && t.opticsIncidentModeEnum == opticsIncidentModeEnum)
            .Select(t => t.AODWaveformProfile)
            .OfType<ChirpAODWaveformProfile>()
            .OrderBy(t => t.OpticsAODElectrodeEnum)
            .Select(t => t.Clone())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ChirpAODWaveformProfile>>(result);
    }

    public SxExecuteRet<bool> SetPrescanAODWaveProfiles(ProductivityInformation productivityInformation, string filePath, OpticsIncidentModeEnum opticsIncidentModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChirpAODWaveProfiles(ProductivityInformation productivityInformation, string filePath, OpticsIncidentModeEnum opticsIncidentModeEnum)
    {
        throw new NotImplementedException();
    }

    private SxExecuteRet<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum)>> GetPrescanChirpDarkFieldAodWaveProfileList()
    {
        if (_prescanChirpAODWaveConfigList is not null) return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigList);

        var sxExecuteRet = Invoke(() => Service!.GetAWGFilePath());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum)>>(sxExecuteRet.ErrorMsg, []);

        var result = sxExecuteRet.Anything.OrderBy(t => t.Id).ToList();

        Guard.IsTrue(result.Count > 0, "Prescan Chirp Config List is empty");
        Guard.IsTrue(result
            .Select(t => t.Id.ToOpticsAODElectrodeEnum())
            .OrderBy(t => t)
            .SequenceEqual(EnumHelper.Enums<OpticsAODElectrodeEnum>()
                .OrderBy(t => t)
                .ToList()
                .GetRange(0, result.Count)), "Id is not from 1 to ..");

        var getProductivityInformationSxExecuteRet = GetProductivityInformations();
        if (getProductivityInformationSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<(AbstractAODWaveformProfile AODWaveformProfile, ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum)>>(sxExecuteRet.ErrorMsg, []);
        var opticsMags = getProductivityInformationSxExecuteRet.Anything.DistinctBy(t=>t.OpticsMagType).ToList();

        var allOpticsIncidentModes = EnumHelper.Enums<OpticsIncidentModeEnum>();
        var list = new List<(AbstractAODWaveformProfile, ProductivityInformation, OpticsIncidentModeEnum)>();

        foreach (var t in result)
        {
            foreach (var nioiMode in allOpticsIncidentModes)
            {
                foreach (var productivityInfo in opticsMags)
                {
                    // 创建Prescan类型的配置
                    var prescanProfile = AODWaveformProfileFactory.CreatePrescan(
                        t.Id.ToOpticsAODElectrodeEnum(),
                        t.PrescanFilePaths.Single(tt => tt.Key == nioiMode.ToCgNIOITypeEnum())
                            .Value
                            .Single(tt => tt.Key == productivityInfo.AdaptTo().Mag.ToCgMagTypeEnum())
                            .Value);
                    list.Add((prescanProfile, productivityInfo, nioiMode));

                    // 创建Chirp类型的配置
                    var chirpProfile = AODWaveformProfileFactory.CreateChirp(
                        t.Id.ToOpticsAODElectrodeEnum(),
                        t.ChirpFilePaths.Single(tt => tt.Key == nioiMode.ToCgNIOITypeEnum())
                            .Value
                            .Single(tt => tt.Key == productivityInfo.AdaptTo().Mag.ToCgMagTypeEnum())
                            .Value);
                    list.Add((chirpProfile, productivityInfo, nioiMode));
                }
            }
        }
        _prescanChirpAODWaveConfigList = [..list];
        return SxExecuteRetHelper.CreateSuccess(_prescanChirpAODWaveConfigList);
    }

    public SxExecuteRet<(double XPixelSize, double YPixelSize)> GetProductivityPixelSize(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = Invoke(() => Service?.GetSpeedInfo(productivityInformation.AdaptTo().Mag));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, (-1d, -1d));

        var xPixelSize = sxExecuteRet.Anything
            .Speed
            .Single(t => t.Key == productivityInformation.AdaptTo().Speed.ToCgSpeedLevelType())
            .Value
            .XPixelSize;

        var yPixelSize = sxExecuteRet.Anything.YPixelSize;
        return SxExecuteRetHelper.CreateSuccess((xPixelSize, yPixelSize));
    }

    private SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations()
    {
        if (_productivityInformations is not null) return SxExecuteRetHelper.CreateSuccess(_productivityInformations);

        var sxExecuteRet = Invoke(() => Service?.GetProductivityInfos());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(sxExecuteRet.ErrorMsg, []);

        var productivityInformationList = new List<ProductivityInformation>();

        foreach (var c2MProductivityInfo in sxExecuteRet.Anything.Where(t => t.IsUsed))
        {
            var speedInfoSxExecuteRet = Invoke(() => Service?.GetSpeedInfo(c2MProductivityInfo.Mag));
            if (speedInfoSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            productivityInformationList.Add(ProductivityInformation.Default.Clone().AdaptIn(c2MProductivityInfo, speedInfoSxExecuteRet.Anything));
        }

        Guard.IsNotEmpty(productivityInformationList, "Productivity Information is empty");

        _productivityInformations = [.. productivityInformationList.OrderBy(t => t)];

        return SxExecuteRetHelper.CreateSuccess(_productivityInformations);
    }
}