using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;

namespace Core.Models.Models.Common.StageMap.Graphics;

public sealed class StageMapDieBuilder : WaferMapDieBuilder
{
    protected override bool IsCheckInWafer(Circle circle, Rect rect)
    {
        return circle.GetBoundingRect().Contains(rect.Point);
    }
}