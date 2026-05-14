using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Recipe;
using Cuga.Data.DataStruct.Stage;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;
using Point = Net.Utilities.Models.Geometries.Point;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

[CacheVersion("1.0.0")]
public sealed partial class PupilSideChannelFlexibleApertureDTO : CalibrationDTOBase<PupilSideChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilSideChannelFlexibleAperture>
{
    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh1 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxEndPositionCh1 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh2 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxEndPositionCh2 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber1Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber2Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber1Ch2 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber2Ch2 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber1Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber2Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber1Ch2 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber2Ch2 { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<int> CgFFBoxRodWidthListCh1 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<int> CgFFBoxRodWidthListCh2 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxHeightRelationPercentListCh1 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxHeightRelationPercentListCh2 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Rect> CurrentImageRectListFirstCh1 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Rect> CurrentImageRectListFirstCh2 { get; set; } = [];

    [ObservableProperty]
    public partial double CgFFBoxAllRodsBeginPercentCh1 { get; set; }

    [ObservableProperty]
    public partial double CgFFBoxAllRodsBeginPercentCh2 { get; set; }

    #region Mapper

    public override PupilSideChannelFlexibleApertureDTO Clone() => new()
    {
        CgFFBoxBeginPositionCh1 = CgFFBoxBeginPositionCh1,
        CgFFBoxBeginPositionCh2 = CgFFBoxBeginPositionCh2,
        CgFFBoxEndPositionCh1 = CgFFBoxEndPositionCh1,
        CgFFBoxEndPositionCh2 = CgFFBoxEndPositionCh2,
        CgFFBoxBeginNumber1Ch1 = CgFFBoxBeginNumber1Ch1,
        CgFFBoxBeginNumber2Ch1 = CgFFBoxBeginNumber2Ch1,
        CgFFBoxBeginNumber1Ch2 = CgFFBoxBeginNumber1Ch2,
        CgFFBoxBeginNumber2Ch2 = CgFFBoxBeginNumber2Ch2,
        CgFFBoxEndNumber1Ch1 = CgFFBoxEndNumber1Ch1,
        CgFFBoxEndNumber2Ch1 = CgFFBoxEndNumber2Ch1,
        CgFFBoxEndNumber1Ch2 = CgFFBoxEndNumber1Ch2,
        CgFFBoxEndNumber2Ch2 = CgFFBoxEndNumber2Ch2,
        CgFFBoxRodWidthListCh1 = CgFFBoxRodWidthListCh1,
        CgFFBoxRodWidthListCh2 = CgFFBoxRodWidthListCh2,
        CgFFBoxHeightRelationPercentListCh1 = CgFFBoxHeightRelationPercentListCh1,
        CgFFBoxHeightRelationPercentListCh2 = CgFFBoxHeightRelationPercentListCh2,

        CurrentImageRectListFirstCh1 = CurrentImageRectListFirstCh1,
        CurrentImageRectListFirstCh2 = CurrentImageRectListFirstCh2,

        CgFFBoxAllRodsBeginPercentCh1 = CgFFBoxAllRodsBeginPercentCh1,
        CgFFBoxAllRodsBeginPercentCh2 = CgFFBoxAllRodsBeginPercentCh2,

        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationPupilSideChannelFlexibleAperture AdaptTo() => new()
    {
        CgFFBoxBeginPositionCh1 = new CgPoint((int)Math.Round(CgFFBoxBeginPositionCh1.X), (int)Math.Round(CgFFBoxBeginPositionCh1.Y)),
        CgFFBoxEndPositionCh1 = new CgPoint((int)Math.Round(CgFFBoxEndPositionCh1.X), (int)Math.Round(CgFFBoxEndPositionCh1.Y)),
        CgFFBoxBeginNumber1Ch1 = CgFFBoxBeginNumber1Ch1,
        CgFFBoxBeginNumber2Ch1 = CgFFBoxBeginNumber2Ch1,
        CgFFBoxEndNumber1Ch1 = CgFFBoxEndNumber1Ch1,
        CgFFBoxEndNumber2Ch1 = CgFFBoxEndNumber2Ch1,
        CgFFBoxRodWidthListCh1 = CgFFBoxRodWidthListCh1.ToList(),
        CgFFBoxHeightRelationPercentListCh1 = CgFFBoxHeightRelationPercentListCh1.ToList(),
        CurrentImageRectListFirstCh1 = CurrentImageRectListFirstCh1.Select(r => new RectD((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height)).ToList(),
        CgFFBoxAllRodsBeginPercentCh1 = CgFFBoxAllRodsBeginPercentCh1,

        CgFFBoxBeginPositionCh2 = new CgPoint((int)Math.Round(CgFFBoxBeginPositionCh2.X), (int)Math.Round(CgFFBoxBeginPositionCh2.Y)),
        CgFFBoxEndPositionCh2 = new CgPoint((int)Math.Round(CgFFBoxEndPositionCh2.X), (int)Math.Round(CgFFBoxEndPositionCh2.Y)),
        CgFFBoxBeginNumber1Ch2 = CgFFBoxBeginNumber1Ch2,
        CgFFBoxBeginNumber2Ch2 = CgFFBoxBeginNumber2Ch2,
        CgFFBoxEndNumber1Ch2 = CgFFBoxEndNumber1Ch2,
        CgFFBoxEndNumber2Ch2 = CgFFBoxEndNumber2Ch2,
        CgFFBoxRodWidthListCh2 = CgFFBoxRodWidthListCh2.ToList(),
        CgFFBoxHeightRelationPercentListCh2 = CgFFBoxHeightRelationPercentListCh2.ToList(),
        CurrentImageRectListFirstCh2 = CurrentImageRectListFirstCh2.Select(r => new RectD((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height)).ToList(),
        CgFFBoxAllRodsBeginPercentCh2 = CgFFBoxAllRodsBeginPercentCh2,

        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}