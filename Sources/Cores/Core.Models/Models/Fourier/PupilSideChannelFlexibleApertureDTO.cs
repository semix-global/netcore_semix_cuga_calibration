using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Recipe;
using Cuga.Data.DataStruct.Stage;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;
using Point = Net.Utilities.Models.Geometries.Point;

namespace Core.Models.Models.Fourier;

[CacheVersion("1.0.0")]
public sealed partial class PupilSideChannelFlexibleApertureDTO : CalibrationDtoBase, ICloneable<PupilSideChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilSideChannelFlexibleAperture>
{
    [ObservableProperty]
    private Point _cgFFBoxBeginPositionCh1 = Point.Origin;

    [ObservableProperty]
    private Point _cgFFBoxEndPositionCh1 = Point.Origin;

    [ObservableProperty]
    private Point _cgFFBoxBeginPositionCh2 = Point.Origin;

    [ObservableProperty]
    private Point _cgFFBoxEndPositionCh2 = Point.Origin;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber1Ch1 = 0;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber2Ch1 = 0;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber1Ch2 = 0;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber2Ch2 = 0;

    [ObservableProperty]
    private int _cgFFBoxEndNumber1Ch1 = 0;

    [ObservableProperty]
    private int _cgFFBoxEndNumber2Ch1 = 0;

    [ObservableProperty]
    private int _cgFFBoxEndNumber1Ch2 = 0;

    [ObservableProperty]
    private int _cgFFBoxEndNumber2Ch2 = 0;

    [ObservableProperty]
    private ObservableCollection<int> _cgFFBoxRodWidthListCh1 = new ObservableCollection<int>();

    [ObservableProperty]
    private ObservableCollection<int> _cgFFBoxRodWidthListCh2 = new ObservableCollection<int>();

    [ObservableProperty]
    private ObservableCollection<int> _cgFFBoxHeightRelationPercentListCh1 = new ObservableCollection<int>();

    [ObservableProperty]
    private ObservableCollection<int> _cgFFBoxHeightRelationPercentListCh2 = new ObservableCollection<int>();

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectListFirstCh1 = new();

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectListFirstCh2 = new();

    [ObservableProperty]
    private double _cgFFBoxAllRodsBeginPercentCh1 = 0;

    [ObservableProperty]
    private double _cgFFBoxAllRodsBeginPercentCh2 = 0;

    #region Mapper

    public PupilSideChannelFlexibleApertureDTO Clone()
    {
        return new PupilSideChannelFlexibleApertureDTO
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
    }

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