using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Recipe;
using Cuga.Data.DataStruct.Stage;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class FourierSideChannelFlexibleApertureDTO : CalibrationDTOBase<FourierSideChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilSideChannelFlexibleAperture>, IDisposable
{
    public const int DefaultRodTotalCount = 46;

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTOItem Channel1Item { get; set; }

    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTOItem Channel2Item { get; set; }

    public FourierSideChannelFlexibleApertureDTO() : this(DefaultRodTotalCount)
    {
    }

    public FourierSideChannelFlexibleApertureDTO(int rodTotalCount)
    {
        _rodTotalCount = rodTotalCount;

        Channel1Item = new FourierSideChannelFlexibleApertureDTOItem(rodTotalCount) { ChannelId = 1 };
        Channel2Item = new FourierSideChannelFlexibleApertureDTOItem(rodTotalCount) { ChannelId = 2 };
    }

    #region Mapper

#pragma warning disable IDISP003

    public override FourierSideChannelFlexibleApertureDTO Clone() => new(_rodTotalCount)
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
        var channel1 = AdaptChannel(Channel1Item);
        var channel2 = AdaptChannel(Channel2Item);

        return new CalibrationPupilSideChannelFlexibleAperture
        {
            CgFFBoxBeginPositionCh1 = channel1.BeginPosition,
            CgFFBoxBeginPositionCh2 = channel2.BeginPosition,
            CgFFBoxEndPositionCh1 = channel1.EndPosition,
            CgFFBoxEndPositionCh2 = channel2.EndPosition,
            CgFFBoxBeginNumber1Ch1 = channel1.BeginOddNumber,
            CgFFBoxBeginNumber2Ch1 = channel1.BeginEvenNumber,
            CgFFBoxBeginNumber1Ch2 = channel2.BeginOddNumber,
            CgFFBoxBeginNumber2Ch2 = channel2.BeginEvenNumber,
            CgFFBoxEndNumber1Ch1 = channel1.EndOddNumber,
            CgFFBoxEndNumber2Ch1 = channel1.EndEvenNumber,
            CgFFBoxEndNumber1Ch2 = channel2.EndOddNumber,
            CgFFBoxEndNumber2Ch2 = channel2.EndEvenNumber,
            CgFFBoxRodWidthListCh1 = channel1.RodWidths,
            CgFFBoxRodWidthListCh2 = channel2.RodWidths,
            CgFFBoxHeightRelationPercentListCh1 = channel1.HeightRelationPercents,
            CgFFBoxHeightRelationPercentListCh2 = channel2.HeightRelationPercents,
            CurrentImageRectListFirstCh1 = channel1.RodRects,
            CurrentImageRectListFirstCh2 = channel2.RodRects,
            CgFFBoxAllRodsBeginPercentCh1 = channel1.AllRodsBeginPercent,
            CgFFBoxAllRodsBeginPercentCh2 = channel2.AllRodsBeginPercent,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper

    public object ToHtmlAnonymous() => new
    {
        Channel1Item = Channel1Item.ToHtmlAnonymous(),
        Channel2Item = Channel2Item.ToHtmlAnonymous()
    };

    public void Dispose()
    {
        Channel1Item.Dispose();
        Channel2Item.Dispose();
    }

    private static ChannelAdaptResult AdaptChannel(FourierSideChannelFlexibleApertureDTOItem channel)
    {
        FourierSideChannelFlexibleApertureDTOItem.Rod[] rods =
        [
            .. channel.OddItem.Step2Rods.Concat(channel.EvenItem.Step2Rods).OrderBy(t => t.Index)
        ];
        FourierSideChannelFlexibleApertureDTOItem.Rod[] visibleOddRods = [.. channel.OddItem.Step2Rods.Where(t => t.IsDeleted == false).OrderBy(t => t.Index)];
        FourierSideChannelFlexibleApertureDTOItem.Rod[] visibleEvenRods = [.. channel.EvenItem.Step2Rods.Where(t => t.IsDeleted == false).OrderBy(t => t.Index)];
        FourierSideChannelFlexibleApertureDTOItem.Rod[] visibleRods = [.. rods.Where(t => t.IsDeleted == false)];

        var beginRod = visibleRods.FirstOrDefault();
        var endRod = visibleRods.LastOrDefault();

        return new ChannelAdaptResult(
            beginRod is null ? Point.Origin.ToCgPoint() : new Point(beginRod.ImageROI.X, beginRod.ImageROI.Y).ToCgPoint(),
            endRod is null ? Point.Origin.ToCgPoint() : new Point(endRod.ImageROI.XMax, endRod.ImageROI.YMax).ToCgPoint(),
            visibleOddRods.Length == 0 ? 0 : visibleOddRods[0].Index + 1,
            visibleEvenRods.Length == 0 ? 0 : visibleEvenRods[0].Index + 1,
            visibleOddRods.Length == 0 ? 0 : visibleOddRods[^1].Index + 1,
            visibleEvenRods.Length == 0 ? 0 : visibleEvenRods[^1].Index + 1,
            [.. rods.Select(t => (int)Math.Round(t.ImageROI.Width))],
            [.. rods.Select(t => t.ImageROI.Height / 100d)],
            [.. rods.Select(t => t.ImageROI.ToRectD())],
            0d);
    }

    private readonly record struct ChannelAdaptResult(
        CgPoint BeginPosition,
        CgPoint EndPosition,
        int BeginOddNumber,
        int BeginEvenNumber,
        int EndOddNumber,
        int EndEvenNumber,
        List<int> RodWidths,
        List<double> HeightRelationPercents,
        List<RectD> RodRects,
        double AllRodsBeginPercent);
}
