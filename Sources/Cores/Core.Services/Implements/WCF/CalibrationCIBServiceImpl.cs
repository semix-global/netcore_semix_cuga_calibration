using System.IO;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationCIBServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService) : BaseService<ICgCalibrationService>, ICalibrationCIBService
{
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

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits, double maxLogGain)
    {
        var logGainMul128Bytes = new List<byte>();
        foreach (var compArray in logGainMul128U12Bits
                     .Select(t => (int)t)
                     .Select(BitConverter.GetBytes))
        {
            logGainMul128Bytes.Add(0);
            logGainMul128Bytes.Add(0);
            logGainMul128Bytes.Add(compArray[1]);
            logGainMul128Bytes.Add(compArray[0]);
        }

        var gainS16BitBytes = new List<byte>();
        foreach (var compArray in gainS16Bits
                     .Select(t => (int)t)
                     .Select(BitConverter.GetBytes))
        {
            gainS16BitBytes.Add(0);
            gainS16BitBytes.Add(0);
            gainS16BitBytes.Add(compArray[1]);
            gainS16BitBytes.Add(compArray[0]);
        }

        var sxExecuteRet = Invoke(() => Service?.SendCIBWave(logGainMul128Bytes, CgCIBWaveType.Sense, cibInformation.PMTId, cibInformation.ChannelId));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service?.SendCIBWave(gainS16BitBytes, CgCIBWaveType.IG, cibInformation.PMTId, cibInformation.ChannelId));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.MaxGain, [(Convert.ToInt32(maxLogGain), cibInformation.PMTId, cibInformation.ChannelId)]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetLightMatching(CIBInformation cibInformation, double digitalGainPlusMultiplicativeFactors)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.Digital_Gain_Multiplicative_Factors, [(Convert.ToInt32(digitalGainPlusMultiplicativeFactors), cibInformation.PMTId, cibInformation.ChannelId)]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDto>>> GetPMTValuesAsync(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        SxExecuteRet<List<M2CImgSysCollectImgDTO>> dfImgCalibrationRet;

        try
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(60));
            if (setWaitTimeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDto>>(setWaitTimeRet.ErrorMsg, []);

            dfImgCalibrationRet = Invoke(() => Service?.GetDFImgCalibration(new SxCollectImgParam
            {
                Type = SxCollectImgType.Using,
                Mag = productivityInformation.AdaptTo().Mag,
                Speed = productivityInformation.AdaptTo().Speed,
                NIOI = opticsIlluminationModeEnum.ToSxNIOIEnum(),
                CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                CollectMode = SxCollectMode.PW,
                PMTId = -1,
                Width = imageWidth,
                StartPoint = [position.ToSxPointD()],
                IsSingle = true,
                AF = isAutoFocus ? 0 : 1,
                IsForward = true,
                IsCalibration = true, /*为true时不下发波形*/
                ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
            }));
        }
        finally
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(30));
            if (setWaitTimeRet.IsSuccess == false) throw new CugaException(setWaitTimeRet.ErrorMsg);
        }

        if (dfImgCalibrationRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDto>>(dfImgCalibrationRet.ErrorMsg, []);

        var result = new DarkFieldImageDto[cibInformations.Count];

        await Task.WhenAll(cibInformations.Index().Select(t => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (index, cibInformation) = t;

            var m2CImgSysCollectImgDto = dfImgCalibrationRet.Anything.Single(tt => tt.PMTId == cibInformation.PMTId && tt.Channel == cibInformation.ChannelId);

            var rawBytes = File.ReadAllBytes(m2CImgSysCollectImgDto.Url);
            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(rawBytes);

            result[index] = new DarkFieldImageDto { Image = image, Matrix = matrix }.AdaptIn(m2CImgSysCollectImgDto);
        }, cancellationToken)));

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDto>>(result);
    }
}