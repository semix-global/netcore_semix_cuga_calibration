using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Core.Models.Models.Common.Recipe.Wafer.WaferMap;
using CugaCalibration.Core.Models;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using Local.NoSQL.DB.Providers.Helper;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationRecipeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationRecipeServiceImpl(
    [FromKeyedServices(LiteDbConstantHelper.RecipeDbKey)]
    ICacheProvider cacheProvider,
    ILogger<CalibrationRecipeServiceImpl> logger,
    StageViewModel stageViewModel,
    ApplicationCookie applicationCookie) : ICalibrationRecipeService
{
    public WaferMapDataDto GetWaferMapData()
    {
        return applicationCookie.CalibrationRecipeDto is not null
            ? applicationCookie.CalibrationRecipeDto!.WaferDto.WaferMapDto.WaferMapData
            : throw new ArgumentNullException(nameof(applicationCookie.CalibrationRecipeDto));
    }

    public WaferMapDto GetWaferMap()
    {
        return applicationCookie.CalibrationRecipeDto is not null
            ? applicationCookie.CalibrationRecipeDto!.WaferDto.WaferMapDto
            : throw new ArgumentNullException(nameof(applicationCookie.CalibrationRecipeDto));
    }

    public ReticleMarkDto GetReticleMark()
    {
        return applicationCookie.CalibrationRecipeDto is not null
            ? applicationCookie.CalibrationRecipeDto.WaferDto.ReticleMarkDto
            : throw new ArgumentNullException(nameof(applicationCookie.CalibrationRecipeDto));
    }

    public bool GetWaferMapOffset(out Point offset)
    {
        try
        {
            // 对准缓存
            var alignmentCacheBrightField = cacheProvider.GetOrDefault<AlignmentCacheBrightField>();
            // 重新对准
            var alignmentResult = stageViewModel.Alignment(alignmentCacheBrightField.LowSite1, alignmentCacheBrightField.LowSite2,
                alignmentCacheBrightField.HighSite1, alignmentCacheBrightField.HighSite2,
                alignmentCacheBrightField.LowMag, alignmentCacheBrightField.HighMag,
                alignmentCacheBrightField.AlgorithmWaferTypeEnum);

            var waferDto = applicationCookie.CalibrationRecipeDto!.WaferDto;
            // 配方对准结果缓存
            var recipeAlignmentResult = waferDto.AlignmentResultDto;

            // 当前对准与配方对准结果的偏移量（waferMap偏移值）
            var offsetX = ((alignmentResult.MarkPoint1.X - recipeAlignmentResult!.MarkPoint1.X) +
                           (alignmentResult.MarkPoint2.X - recipeAlignmentResult!.MarkPoint2.X)) / 2;
            var offsetY = ((alignmentResult.MarkPoint1.Y - recipeAlignmentResult!.MarkPoint1.Y) +
                           (alignmentResult.MarkPoint2.Y - recipeAlignmentResult!.MarkPoint2.Y)) / 2;
            offset = new Point(offsetX, offsetY);

            return true;
        }
        catch (Exception ex)
        {
            offset = Point.Empty;
            logger.LogError(ex, "Get wafer map offset failed");
            return false;
        }
    }

    public bool GetCorrectWaferMapByOffset(bool isAutoAlignment)
    {
        try
        {
            var originalWaferDto = applicationCookie.CalibrationRecipeDto!.WaferDto;
            var waferCenterBrightFieldPosition = originalWaferDto.WaferCenterBrightFieldPosition!.Value;
            var waferDto = originalWaferDto.Clone();
            var offsetPosition = Point.Empty;
            if (isAutoAlignment && GetWaferMapOffset(out offsetPosition) == false)
                return false;

            waferDto.WaferMapDto.OriginDieDto.WaferPosition = originalWaferDto.WaferMapDto.OriginDieDto.WaferPosition
                                                              + waferCenterBrightFieldPosition
                                                              + offsetPosition;
            waferDto.WaferMapDto.OriginReticleDto.WaferPosition = originalWaferDto.WaferMapDto.OriginReticleDto.WaferPosition
                                                                  + waferCenterBrightFieldPosition
                                                                  + offsetPosition;
            waferDto.WaferMapDto.WaferMapDieDtoItemList.ForEach(row =>
            {
                row.ForEach(die =>
                {
                    die.WaferPosition = die.WaferPosition
                                        + waferCenterBrightFieldPosition
                                        + offsetPosition;
                });
            });
            waferDto.WaferMapDto.WaferMapReticleDieDtoItemList.ForEach(row =>
            {
                row.ForEach(reticleCell =>
                {
                    reticleCell.WaferPosition = reticleCell.WaferPosition
                                                + waferCenterBrightFieldPosition
                                                + offsetPosition;
                });
            });

            applicationCookie.CalibrationReviseRecipeDto = applicationCookie.CalibrationRecipeDto!.Clone();
            applicationCookie.CalibrationReviseRecipeDto.WaferDto = waferDto.Clone();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get correct wafer map by offset failed");
            return false;
        }
    }

    public bool GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            var reticleMaskDto = GetReticleMark();
            maskInfo = reticleMaskDto.MicrosocpeReticleMarkItemList
                                   .First(t => t.ReticleMaskTypeEnum == waferMaskType
                                               && (magnificationType is null || (t.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum == magnificationType && t.RecipeBrightFieldTemplateDto.TemplateFilePath != string.Empty))
                                               && (opticsMagType is null || (t.RecipeDarkFieldTemplateDto.OpticsMagTypeEnum == opticsMagType && t.RecipeDarkFieldTemplateDto.TemplateFilePath != string.Empty))
                                   );
            return true;
        }
        catch (Exception ex)
        {
            maskInfo = new ReticleMarkItemDto();
            logger.LogError(ex, "Get microscope reticle mask info failed");
            return false;
        }
    }

    public bool GetChuckReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            var reticleMaskDto = GetReticleMark();
            maskInfo = reticleMaskDto.ChuckReticleMarkItemList
                                    .First(t => t.ReticleMaskTypeEnum == waferMaskType
                                                && (magnificationType is null || (t.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum == magnificationType && t.RecipeBrightFieldTemplateDto.TemplateFilePath != string.Empty))
                                                && (opticsMagType is null || (t.RecipeDarkFieldTemplateDto.OpticsMagTypeEnum == opticsMagType && t.RecipeDarkFieldTemplateDto.TemplateFilePath != string.Empty))
                                    );
            return true;
        }
        catch (Exception ex)
        {
            maskInfo = new ReticleMarkItemDto();
            logger.LogError(ex, "Get chuck reticle mask info failed");
            return false;
        }
    }

    public bool GetLaserReticleMaskMachineInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, StageSpeedEnum? stageSpeedEnum, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            var reticleMaskDto = GetReticleMark();
            maskInfo = reticleMaskDto.LaserReticleMarkItemList
                .First(t => t.ReticleMaskTypeEnum == waferMaskType
                            && (magnificationType is null || (t.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum == magnificationType && t.RecipeBrightFieldTemplateDto.TemplateFilePath != string.Empty))
                            && (opticsMagType is null || (t.RecipeDarkFieldTemplateDto.OpticsMagTypeEnum == opticsMagType
                                                          && (stageSpeedEnum is null || t.RecipeDarkFieldTemplateDto.StageSpeedEnum == stageSpeedEnum)
                                                          && t.RecipeDarkFieldTemplateDto.TemplateFilePath != string.Empty))
                );

            return true;
        }
        catch (Exception ex)
        {
            maskInfo = new ReticleMarkItemDto();
            logger.LogError(ex, "Get laser reticle mask info failed");
            return false;
        }
    }

    public bool GetReticleMaskBrightFieldPosition(WaferMapDieItemDto reticleCellDto, ReticleMarkItemDto maskDto, out Point position)
    {
        try
        {
            var waferMapData = GetWaferMapData();
            var reticleHeight = waferMapData.ReticleHeight;
            var realReticleMaskBrightFieldPosition = maskDto.MaskWaferCellPosition + (reticleCellDto.WaferPosition - new Point(0, reticleHeight));
            position = realReticleMaskBrightFieldPosition;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get reticle mask bright field position failed");
            position = Point.Empty;
            return false;
        }
    }
}