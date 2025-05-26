using Core.Models.Models.Common.StageMap;
using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Models;
using Xunit;

namespace CugaCalibrationUnitTest;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var map = new StageMapDto().AdaptTo();
        var cgErrorMap = map.ToCgErrorMap();
        Assert.NotNull(cgErrorMap);

        var stageMap = new StageMapDto(10, 10, 10, 20);
        stageMap.GenerateByStartPosition(new Point(0, 0), new Point(0, 0), 1000);
        stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath = nameof(stageMap);
        stageMap.RealMatrix[1][1] = new Point(-1, -2);
        stageMap.ErrorMatrix[1][1] = new Point(-3, -4);
        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        var stageMapClone = stageMap.Clone();
        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(-3, -4));

        var adaptTo = stageMap.AdaptTo();
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, -3);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, -4);

        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, -3);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, -4);
    }

    [Fact]
    public void Test2()
    {
        #region Test1

        var stageMap = new StageMapDto(10, 10, 10, 20);
        stageMap.GenerateByStartPosition(new Point(0, 0), new Point(0, 0), 1000);
        stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath = nameof(stageMap);
        stageMap.RealMatrix[1][1] = new Point(-1, -2);
        stageMap.ErrorMatrix[1][1] = new Point(-3, -4);
        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        var stageMapClone = stageMap.Clone();
        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(-3, -4));

        var adaptTo = stageMap.AdaptTo();
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, -3);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, -4);

        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, -3);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, -4);

        #endregion Test1

        stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath = nameof(stageMapClone);
        stageMapClone.IdealStageMapItemMatrix[1][1].Point = new Point(1, 2);
        stageMapClone.RealMatrix[1][1] = new Point(3, 4);
        stageMapClone.ErrorMatrix[1][1] = new Point(5, 6);
        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMapClone));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(1, 2));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(3, 4));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(5, 6));

        adaptTo.ErrorMatrix[1][1] = new CgPoint(11, 12);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, 11);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, 12);

        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMapClone));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(1, 2));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(3, 4));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(5, 6));

        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, 11);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, 12);
    }

    [Fact]
    public void Test3()
    {
        #region Test2

        #region Test1

        var stageMap = new StageMapDto(10, 10, 10, 20);
        stageMap.GenerateByStartPosition(new Point(0, 0), new Point(0, 0), 1000);
        stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath = nameof(stageMap);
        stageMap.RealMatrix[1][1] = new Point(-1, -2);
        stageMap.ErrorMatrix[1][1] = new Point(-3, -4);
        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        #region Test3

        stageMap.ErrorMatrix[2][2] = stageMap.IdealStageMapItemMatrix[2][2].Point;
        Assert.Equal(stageMap.ErrorMatrix[2][2], new Point(40, 20));
        stageMap.ErrorMatrix[2][2] = new Point(-11, -22);
        Assert.Equal(stageMap.ErrorMatrix[2][2], new Point(-11, -22));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[2][2].Point, new Point(40, 20));

        var p = stageMap.IdealStageMapItemMatrix[2][2].Point;
        Assert.Equal(p, new Point(40, 20));
        p = new Point(-33, -44);
        Assert.Equal(p, new Point(-33, -44));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[2][2].Point, new Point(40, 20));

        #endregion Test3

        var stageMapClone = stageMap.Clone();
        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(-3, -4));

        var adaptTo = stageMap.AdaptTo();
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, -3);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, -4);

        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, -3);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, -4);

        #endregion Test1

        stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath = nameof(stageMapClone);
        stageMapClone.IdealStageMapItemMatrix[1][1].Point = new Point(1, 2);
        stageMapClone.RealMatrix[1][1] = new Point(3, 4);
        stageMapClone.ErrorMatrix[1][1] = new Point(5, 6);
        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMapClone));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(1, 2));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(3, 4));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(5, 6));

        adaptTo.ErrorMatrix[1][1] = new CgPoint(11, 12);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, 11);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, 12);

        Assert.True(stageMap.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMap));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[1][1].Point, new Point(20, 10));
        Assert.Equal(stageMap.RealMatrix[1][1], new Point(-1, -2));
        Assert.Equal(stageMap.ErrorMatrix[1][1], new Point(-3, -4));

        Assert.True(stageMapClone.IdealStageMapItemMatrix[1][1].TemplateImageFilePath == nameof(stageMapClone));
        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[1][1].Point, new Point(1, 2));
        Assert.Equal(stageMapClone.RealMatrix[1][1], new Point(3, 4));
        Assert.Equal(stageMapClone.ErrorMatrix[1][1], new Point(5, 6));

        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.X, 11);
        Assert.Equal(adaptTo.ErrorMatrix[1][1]?.Y, 12);

        #endregion Test2

        stageMapClone.IdealStageMapItemMatrix[2][2].Point = new Point(1234, 12341);
        stageMapClone.ErrorMatrix[2][2] = new Point(1234, 12342);
        adaptTo.ErrorMatrix[2][2] = new CgPoint(12342, 123424);

        Assert.Equal(stageMapClone.IdealStageMapItemMatrix[2][2].Point, new Point(1234, 12341));
        Assert.Equal(stageMapClone.ErrorMatrix[2][2], new Point(1234, 12342));
        Assert.Equal(adaptTo.ErrorMatrix[2][2]?.X, 12342);
        Assert.Equal(adaptTo.ErrorMatrix[2][2]?.Y, 123424);

        Assert.Equal(p, new Point(-33, -44));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[2][2].Point, new Point(40, 20));

        Assert.Equal(stageMap.ErrorMatrix[2][2], new Point(-11, -22));
        Assert.Equal(stageMap.IdealStageMapItemMatrix[2][2].Point, new Point(40, 20));
    }
}