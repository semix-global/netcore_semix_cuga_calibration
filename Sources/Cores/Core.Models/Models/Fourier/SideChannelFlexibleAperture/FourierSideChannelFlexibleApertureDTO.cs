using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Microsoft.Extensions.Logging;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed class FourierSideChannelFlexibleApertureDTO : CalibrationDTOBase<FourierSideChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilSideChannelFlexibleAperture>, IDisposable
{
    internal const int DefaultRodTotalCount = 46;

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    public FourierSideChannelFlexibleApertureDTOItem Channel1Item { get; private init; }

    public FourierSideChannelFlexibleApertureDTOItem Channel2Item { get; private init; }

    public FourierSideChannelFlexibleApertureDTO(int rodTotalCount, string channel1ImageFilePath, string channel2ImageFilePath)
    {
        _rodTotalCount = rodTotalCount;
        Channel1Item = new FourierSideChannelFlexibleApertureDTOItem(rodTotalCount, 1, channel1ImageFilePath);
        Channel2Item = new FourierSideChannelFlexibleApertureDTOItem(rodTotalCount, 2, channel2ImageFilePath);
    }

    public FourierSideChannelFlexibleApertureDTO() : this(DefaultRodTotalCount, string.Empty, string.Empty)
    {
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierSideChannelFlexibleApertureDTO(
        [Newtonsoft.Json.JsonProperty(nameof(_rodTotalCount))]
        int rodTotalCount,
        FourierSideChannelFlexibleApertureDTOItem channel1Item,
        FourierSideChannelFlexibleApertureDTOItem channel2Item)
    {
        _rodTotalCount = rodTotalCount;
        Channel1Item = channel1Item;
        Channel2Item = channel2Item;
    }

    #region Mapper

#pragma warning disable IDISP003

    public override FourierSideChannelFlexibleApertureDTO Clone() => new(_rodTotalCount, Channel1Item.ChannelImageFilePath, Channel2Item.ChannelImageFilePath)
    {
        Channel1Item = Channel1Item.Clone(),
        Channel2Item = Channel2Item.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    public CalibrationPupilSideChannelFlexibleAperture AdaptTo()
    {
        FourierSideChannelFlexibleApertureDTOItem.RodResult[] channel1ItemRodResults = [.. Channel1Item.RodResults.OrderBy(t => t.Index)];
        FourierSideChannelFlexibleApertureDTOItem.RodResult[] channel2ItemRodResults = [.. Channel2Item.RodResults.OrderBy(t => t.Index)];

        var channel1FirstRod = channel1ItemRodResults.FirstOrDefault(t => t.IsDeleted == false);
        var channel1LastRod = channel1ItemRodResults.LastOrDefault(t => t.IsDeleted == false);
        var channel2FirstRod = channel2ItemRodResults.FirstOrDefault(t => t.IsDeleted == false);
        var channel2LastRod = channel2ItemRodResults.LastOrDefault(t => t.IsDeleted == false);

        if (channel1ItemRodResults.Length != _rodTotalCount
            || channel2ItemRodResults.Length != _rodTotalCount
            || channel1FirstRod is null
            || channel1LastRod is null
            || channel2FirstRod is null
            || channel2LastRod is null)
        {
            if (IsCalibrated)
            {
                var logger = HostApplication.GetRequiredService<ILogger<FourierSideChannelFlexibleApertureDTO>>();

                if (channel1ItemRodResults.Length != _rodTotalCount) logger.LogError("Channel 1 rod count is not equal to {@RodTotalCount}.", _rodTotalCount);
                if (channel2ItemRodResults.Length != _rodTotalCount) logger.LogError("Channel 2 rod count is not equal to {@RodTotalCount}.", _rodTotalCount);
                if (channel1FirstRod is null) logger.LogError("Channel 1 first rod is null.");
                if (channel1LastRod is null) logger.LogError("Channel 1 last rod is null.");
                if (channel2FirstRod is null) logger.LogError("Channel 2 first rod is null.");
                if (channel2LastRod is null) logger.LogError("Channel 2 last rod is null.");
            }

            return new CalibrationPupilSideChannelFlexibleAperture
            {
                IsCalibrated = false,
                IsVerified = false,
                IsRequiredCalibrate = IsRequiredSelfCheck
            };
        }

        return new CalibrationPupilSideChannelFlexibleAperture
        {
            CgFFBoxBeginPositionCh1 = channel1FirstRod.MinImageROI.Point.ToCgPoint(),
            CgFFBoxBeginPositionCh2 = channel2FirstRod.MinImageROI.Point.ToCgPoint(),
            CgFFBoxEndPositionCh1 = channel1LastRod.MinImageROI.Point.ToCgPoint(),
            CgFFBoxEndPositionCh2 = channel2LastRod.MinImageROI.Point.ToCgPoint(),
            CgFFBoxBeginNumber1Ch1 = channel1FirstRod.Index,
            CgFFBoxBeginNumber2Ch1 = channel1FirstRod.Index,
            CgFFBoxBeginNumber1Ch2 = channel2FirstRod.Index,
            CgFFBoxBeginNumber2Ch2 = channel2FirstRod.Index,
            CgFFBoxEndNumber1Ch1 = channel1LastRod.Index,
            CgFFBoxEndNumber2Ch1 = channel1LastRod.Index,
            CgFFBoxEndNumber1Ch2 = channel2LastRod.Index,
            CgFFBoxEndNumber2Ch2 = channel2LastRod.Index,
            CgFFBoxRodWidthListCh1 = [.. channel1ItemRodResults.Select(t => ((RectI)t.MinImageROI).Width)],
            CgFFBoxRodWidthListCh2 = [.. channel2ItemRodResults.Select(t => ((RectI)t.MinImageROI).Width)],
            CgFFBoxHeightRelationPercentListCh1 = [.. channel1ItemRodResults.Select(t => t.PixelSize)],
            CgFFBoxHeightRelationPercentListCh2 = [.. channel2ItemRodResults.Select(t => t.PixelSize)],
            CurrentImageRectListFirstCh1 = [.. channel1ItemRodResults.Select(t => t.MinImageROI.ToRectD())],
            CurrentImageRectListFirstCh2 = [.. channel2ItemRodResults.Select(t => t.MinImageROI.ToRectD())],
            CgFFBoxAllRodsBeginPercentCh1 = Channel1Item.MinMotorAbsoluteValue,
            CgFFBoxAllRodsBeginPercentCh2 = Channel2Item.MinMotorAbsoluteValue,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper

    public void Dispose()
    {
        Channel1Item.Dispose();
        Channel2Item.Dispose();
    }
}