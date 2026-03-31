using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Recipe.Models;
using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
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
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider cacheProvider,
    ILogger<CalibrationRecipeServiceImpl> logger,
    StageViewModel stageViewModel) : ICalibrationRecipeService
{
    public bool GetWaferMapOffset(WaferDto waferDto, out Point offset)
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

    public CalibrationRecipeDTO GetCorrectWaferMapByOffset(CalibrationRecipeDTO calibrationRecipeDTO, bool isAutoAlignment)
    {
        var originalWaferDto = calibrationRecipeDTO.WaferDto;
        originalWaferDto.WaferMapDataToWaferMapCanvasDocument();
        var waferDto = originalWaferDto.Clone();
        waferDto.WaferMapDataToWaferMapCanvasDocument();
        var offsetPosition = Point.Origin;
        if (isAutoAlignment && GetWaferMapOffset(waferDto, out offsetPosition) == false)
            throw new Exception("Get Wafer map offset failed");
        var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
        var offset = (Vector)new Point(xDirection * offsetPosition.X, yDirection * offsetPosition.Y);

        waferDto.WaferMapCanvasDocument.DieBuilder.OriginalDiePoint = originalWaferDto.WaferMapCanvasDocument.DieBuilder.OriginalDiePoint
                                                                      + offset;
        waferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint = originalWaferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint
                                                                          + offset;
        waferDto.WaferMapCanvasDocumentToWaferMapData();

        var reviseRecipeDto = calibrationRecipeDTO.Clone();
        reviseRecipeDto.WaferDto = waferDto.Clone();
        reviseRecipeDto.WaferDto.WaferMapDataToWaferMapCanvasDocument();

        return reviseRecipeDto;
    }

    public bool GetMicroscopeReticleMaskInfo(ReticleMarkDto reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? productivityInformation, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            maskInfo = reticleMarkDto.MicrosocpeReticleMarkItemList
                .First(t => t.ReticleMaskTypeEnum == waferMaskType
                            && (microscopeLensInformation is null || (t.RecipeBrightFieldTemplateDto.MicroscopeLensInformation == microscopeLensInformation && t.RecipeBrightFieldTemplateDto.TemplateFilePath != string.Empty))
                            && (productivityInformation is null || (t.RecipeDarkFieldTemplateDto.ProductivityInformation == productivityInformation && t.RecipeDarkFieldTemplateDto.TemplateFilePath != string.Empty))
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

    public bool GetChuckReticleMaskInfo(ReticleMarkDto reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? productivityInformation, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            // todo
            maskInfo = new ReticleMarkItemDto();

            return true;
        }
        catch (Exception ex)
        {
            maskInfo = new ReticleMarkItemDto();
            logger.LogError(ex, "Get laser reticle mask info failed");
            return false;
        }
    }

    public bool GetLaserReticleMaskMachineInfo(ReticleMarkDto reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? productivityInformation, out ReticleMarkItemDto maskInfo)
    {
        try
        {
            // todo
            maskInfo = new ReticleMarkItemDto();

            return true;
        }
        catch (Exception ex)
        {
            maskInfo = new ReticleMarkItemDto();
            logger.LogError(ex, "Get laser reticle mask info failed");
            return false;
        }
    }

    public bool GetDieMaskBrightFieldPosition(WaferMapCanvasDocument waferMapCanvasDocument, WaferMapDie waferMapDie, ReticleMarkItemDto maskDto, out Point position)
    {
        try
        {
            var waferPosition = waferMapDie.Rect.Point;
            var diePitchHeight = waferMapCanvasDocument.DieBuilder.DiePitchSize.Height;
            var scribeSize = waferMapCanvasDocument.DieBuilder.DieScribeSize;
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

    public bool GetReticleMaskBrightFieldPosition(WaferMapCanvasDocument waferMapCanvasDocument, WaferMapReticle waferMapReticle, ReticleMarkItemDto maskDto, out Point position)
    {
        try
        {
            var waferPosition = waferMapReticle.Rect.Point;
            var diePitchHeight = waferMapCanvasDocument.ReticleBuilder.DiePitchSize.Height;
            var scribeSize = waferMapCanvasDocument.ReticleBuilder.DieScribeSize;
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