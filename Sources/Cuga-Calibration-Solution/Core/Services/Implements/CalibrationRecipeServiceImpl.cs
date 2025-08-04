using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Core.Models.Models.Pattern;
using CugaCalibration.Core.Models;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using Local.NoSQL.DB.Providers.Helper;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Documents;
using Net.Utilities.WaferMap.WPF.Drawables;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationRecipeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationRecipeServiceImpl(
    [FromKeyedServices(LiteDbConstantHelper.RecipeDbKey)]
    ICacheProvider cacheProvider,
    ILogger<CalibrationRecipeServiceImpl> logger,
    StageViewModel stageViewModel,
    ApplicationCookie applicationCookie) : ICalibrationRecipeService
{
    public WaferMapCanvasDocument GetWaferMapCanvasDocument()
    {
        return applicationCookie.CalibrationRecipeDto is not null
            ? applicationCookie.CalibrationRecipeDto!.WaferDto.WaferMapCanvasDocument
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
                           (alignmentResult.MarkPoint2.X - recipeAlignmentResult.MarkPoint2.X)) / 2;
            var offsetY = ((alignmentResult.MarkPoint1.Y - recipeAlignmentResult.MarkPoint1.Y) +
                           (alignmentResult.MarkPoint2.Y - recipeAlignmentResult.MarkPoint2.Y)) / 2;
            offset = new Point(offsetX, offsetY);

            return true;
        }
        catch (Exception ex)
        {
            offset = Point.Origin;
            logger.LogError(ex, "Get wafer map offset failed");
            return false;
        }
    }

    public bool GetCorrectWaferMapByOffset(bool isAutoAlignment)
    {
        try
        {
            var originalWaferDto = applicationCookie.CalibrationRecipeDto!.WaferDto;
            originalWaferDto.WaferMapDataToWaferMapCanvasDocument();
            var waferCenterBrightFieldPosition = originalWaferDto.WaferCenterWaferPosition!.Value;
            var waferDto = originalWaferDto.Clone();
            waferDto.WaferMapDataToWaferMapCanvasDocument();
            var offsetPosition = Point.Origin;
            if (isAutoAlignment && GetWaferMapOffset(out offsetPosition) == false)
                return false;
            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            var offset =/* (Vector)waferCenterBrightFieldPosition +*/ (Vector)new Point(xDirection * offsetPosition.X, yDirection * offsetPosition.Y);

            waferDto.WaferMapCanvasDocument.DieBuilder.OriginalDiePoint = originalWaferDto.WaferMapCanvasDocument.DieBuilder.OriginalDiePoint
                                                                          + offset;
            waferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint = originalWaferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint
                                                                              + offset;
            waferDto.WaferMapCanvasDocumentToWaferMapData();

            applicationCookie.CalibrationReviseRecipeDto = applicationCookie.CalibrationRecipeDto!.Clone();
            applicationCookie.CalibrationReviseRecipeDto.WaferDto = waferDto.Clone();
            applicationCookie.CalibrationReviseRecipeDto.WaferDto.WaferMapDataToWaferMapCanvasDocument();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get correct wafer map by offset failed");
            return false;
        }
    }

    public bool GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationInfo? magnificationInfo, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            var reticleMaskDto = GetReticleMark();
            maskInfo = reticleMaskDto.MicrosocpeReticleMarkItemList
                .First(t => t.ReticleMaskTypeEnum == waferMaskType
                            && (magnificationInfo is null || (t.RecipeBrightFieldTemplateDto.MicroscopeMagnificationInfo == magnificationInfo && t.RecipeBrightFieldTemplateDto.TemplateFilePath != string.Empty))
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

    public bool GetChuckReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationInfo? magnificationInfo, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            var reticleMaskDto = GetReticleMark();
            maskInfo = reticleMaskDto.ChuckReticleMarkItemList
                .First(t => t.ReticleMaskTypeEnum == waferMaskType
                            && (magnificationInfo is null || (t.RecipeBrightFieldTemplateDto.MicroscopeMagnificationInfo == magnificationInfo && t.RecipeBrightFieldTemplateDto.TemplateFilePath != string.Empty))
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

    public bool GetLaserReticleMaskMachineInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationInfo? magnificationInfo, OpticsMagTypeEnum? opticsMagType, StageSpeedEnum? stageSpeedEnum, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            var reticleMaskDto = GetReticleMark();
            maskInfo = reticleMaskDto.LaserReticleMarkItemList
                .First(t => t.ReticleMaskTypeEnum == waferMaskType
                            && (magnificationInfo is null || (t.RecipeBrightFieldTemplateDto.MicroscopeMagnificationInfo == magnificationInfo && t.RecipeBrightFieldTemplateDto.TemplateFilePath != string.Empty))
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

    public bool GetDieMaskBrightFieldPosition(WaferMapDie waferMapDie, ReticleMarkItemDto maskDto, out Point position)
    {
        try
        {
            var waferPosition = waferMapDie.Rect.Point;
            var waferMapDocument = GetWaferMapCanvasDocument();
            var diePitchHeight = waferMapDocument.DieBuilder.DiePitchSize.Height;
            var scribeSize = waferMapDocument.DieBuilder.DieScribeSize;
            var realReticleMaskBrightFieldPosition = maskDto.MaskWaferCellPosition
                                                     + ((Vector)waferPosition
                                                        - (Vector)new Point(0, (diePitchHeight + scribeSize.Height)));

            position = realReticleMaskBrightFieldPosition;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get die mask bright field position failed");
            position = Point.Origin;
            return false;
        }
    }

    public bool GetReticleMaskBrightFieldPosition(WaferMapReticle waferMapReticle, ReticleMarkItemDto maskDto, out Point position)
    {
        try
        {
            var waferPosition = waferMapReticle.Rect.Point;
            var waferMapDocument = GetWaferMapCanvasDocument();
            var diePitchHeight = waferMapDocument.ReticleBuilder.DiePitchSize.Height;
            var scribeSize = waferMapDocument.ReticleBuilder.DieScribeSize;
            var realReticleMaskBrightFieldPosition = maskDto.MaskWaferCellPosition
                                                     + ((Vector)waferPosition
                                                     - (Vector)new Point(0, (diePitchHeight + scribeSize.Height)));
            position = realReticleMaskBrightFieldPosition;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get reticle mask bright field position failed");
            position = Point.Origin;
            return false;
        }
    }
}