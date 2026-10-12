using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Algorithm.TemplateMatch;

public sealed partial class TemplateMatchSnapshot : ObservableObject, ICloneable<TemplateMatchSnapshot>, IAdaptIn<TemplateMatchSnapshot, TemplateMatchSnapshot>
{
    [ObservableProperty]
    public partial double Score { get; set; }

    [ObservableProperty]
    public partial double Angle { get; set; }

    [ObservableProperty]
    public partial Point MarkPoint { get; set; }

    [ObservableProperty]
    public partial Point Offset { get; set; }

    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [ObservableProperty]
    public partial string ResultImageFilePath { get; set; } = "";

    public TemplateMatchSnapshot Clone() => new TemplateMatchSnapshot().AdaptIn(this);

    public TemplateMatchSnapshot AdaptIn(TemplateMatchSnapshot obj)
    {
        Score = obj.Score;
        Angle = obj.Angle;
        MarkPoint = obj.MarkPoint;
        Offset = obj.Offset;
        IsOk = obj.IsOk;
        ResultImageFilePath = obj.ResultImageFilePath;

        return this;
    }
}