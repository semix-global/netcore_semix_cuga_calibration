using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed class FourierSideChannelFlexibleApertureDTO(int rodTotalCount, string channel1ImageFilePath, string channel2ImageFilePath) : CalibrationDTOBase<FourierSideChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilSideChannelFlexibleAperture>, IDisposable
{
    private const int DefaultRodTotalCount = 46;

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount = rodTotalCount;

    public FourierSideChannelFlexibleApertureDTOItem Channel1Item { get; private init; } = new(rodTotalCount, 1, channel1ImageFilePath);

    public FourierSideChannelFlexibleApertureDTOItem Channel2Item { get; private init; } = new(rodTotalCount, 2, channel2ImageFilePath);

    public FourierSideChannelFlexibleApertureDTO() : this(DefaultRodTotalCount, string.Empty, string.Empty)
    {
    }

    #region Mapper

#pragma warning disable IDISP003

    public override FourierSideChannelFlexibleApertureDTO Clone() => new(_rodTotalCount, channel1ImageFilePath, channel2ImageFilePath)
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
        var channel1FirstRod = Channel1Item.RodResults.First(t => t.IsDeleted == false);
        var channel1LastRod = Channel1Item.RodResults.Last(t => t.IsDeleted == false);
        var channel2FirstRod = Channel2Item.RodResults.First(t => t.IsDeleted == false);
        var channel2LastRod = Channel2Item.RodResults.Last(t => t.IsDeleted == false);

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
            CgFFBoxRodWidthListCh1 = [.. Channel1Item.RodResults.Select(t => ((RectI)t.MinImageROI).Width)],
            CgFFBoxRodWidthListCh2 = [.. Channel2Item.RodResults.Select(t => ((RectI)t.MinImageROI).Width)],
            CgFFBoxHeightRelationPercentListCh1 = [.. Channel1Item.RodResults.Select(t => t.PixelSize)],
            CgFFBoxHeightRelationPercentListCh2 = [.. Channel2Item.RodResults.Select(t => t.PixelSize)],
            CurrentImageRectListFirstCh1 = [.. Channel1Item.RodResults.Select(t => t.MinImageROI.ToRectD())],
            CurrentImageRectListFirstCh2 = [.. Channel2Item.RodResults.Select(t => t.MinImageROI.ToRectD())],
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