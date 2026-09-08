using Cuga.Data.DataStruct.DTO.Recipe;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Extensions;

public static class UtilitiesRectExtension
{
    extension(Rect @this)
    {
        public RectD ToRectD() => new(@this.X, @this.Y, @this.Width, @this.Height);
    }
}