using AwesomeAssertions;
using Core.Utilities;
using Net.Utilities.Models.Geometries;
using Xunit;

namespace CugaCalibrationUnitTest;

public class FilterTest
{
    [Fact]
    public void Test()
    {
        var points = (Point[])
        [
            new Point(0, 0),
            new Point(266, 0),
            new Point(532, 0.396016344),
            new Point(798, 0.395132968),
            new Point(1064, 0),
            new Point(1330, 0),
            new Point(1596, 0),
            new Point(1862, 0),
            new Point(2128, 0),
            new Point(2394, 0),
            new Point(2660, 0),
            new Point(2926, 0),
            new Point(3192, 0),
            new Point(3458, 0),
            new Point(3724, 0),
            new Point(3990, 0),
            new Point(4912.61484, 0.86119434),
            new Point(4938.65564, 0.989656507),
            new Point(4938.655628, 0.98966226),
            new Point(5054, 0.62343939),
            new Point(5320, 0.617155185),
            new Point(5586, 0.618468753),
            new Point(5852, 0.618451252),
            new Point(6118, 0.612721474),
            new Point(6384, 0.618351373),
            new Point(6650, 0.611341189),
            new Point(6916, 0.603911208),
            new Point(7182, 0.463057004),
            new Point(7448, 0.460642657),
            new Point(7714, 0),
            new Point(7980, 0),
            new Point(8246, 0),
            new Point(8512, 0),
            new Point(8778, 0),
            new Point(9044, 0),
            new Point(9310, 0),
            new Point(9576, 0),
            new Point(9842, 0),
            new Point(10108, 0),
            new Point(10374, 0.339934673),
            new Point(10640, 0.392787915),
            new Point(10906, 0.392674702),
            new Point(11172, 0),
            new Point(11438, 0),
            new Point(11704, 0),
            new Point(11970, 0),
            new Point(12236, 0),
            new Point(12502, 0),
            new Point(12768, 0),
            new Point(13034, 0),
            new Point(13300, 0),
            new Point(13566, 0),
            new Point(13832, 0),
            new Point(14098, 0),
            new Point(14898.65624, 0.99189684),
            new Point(14898.65624, 0.991896681),
            new Point(14896, 0.613047676),
            new Point(15162, 0.620640639),
            new Point(15428, 0.608640109),
            new Point(15694, 0.608640109),
            new Point(15960, 0.618703339),
            new Point(16226, 0.612847119),
            new Point(16492, 0.613673668),
            new Point(16758, 0.613673668),
            new Point(17024, 0.442907523),
            new Point(17290, 0.460175818),
            new Point(17556, 0.459734147),
            new Point(17822, 0),
            new Point(18088, 0),
            new Point(18354, 0),
            new Point(18620, 0),
            new Point(18886, 0),
            new Point(19152, 0),
            new Point(19418, 0),
            new Point(19684, 0),
            new Point(19950, 0),
            new Point(20216, 0),
            new Point(20482, 0.399062006),
            new Point(20748, 0.398818621),
            new Point(21014, 0),
            new Point(21280, 0),
            new Point(21546, 0),
            new Point(21812, 0),
            new Point(22078, 0),
            new Point(22344, 0),
            new Point(22610, 0),
            new Point(22876, 0),
            new Point(23142, 0),
            new Point(23408, 0),
            new Point(23674, 0),
            new Point(23940, 0),
            new Point(24839.6321, 0.995478021),
            new Point(24839.63196, 0.995474164),
            new Point(24738, 0.804830854),
            new Point(25004, 0.602556537),
            new Point(25270, 0.612384953),
            new Point(25536, 0.614320764),
            new Point(25802, 0.624977301),
            new Point(26068, 0.624977301),
            new Point(26334, 0.608413754),
            new Point(26600, 0.607261432),
            new Point(26866, 0.594349502),
            new Point(27132, 0.472448824),
            new Point(27398, 0.4724488),
            new Point(27664, 0),
            new Point(27930, 0),
            new Point(28196, 0),
            new Point(28462, 0),
            new Point(28728, 0),
            new Point(28994, 0),
            new Point(29260, 0),
            new Point(29526, 0),
            new Point(29792, 0),
            new Point(30058, 0),
            new Point(30324, 0.37502503),
            new Point(30590, 0.375225846),
            new Point(30856, 0),
            new Point(31122, 0),
            new Point(31388, 0),
            new Point(31654, 0),
            new Point(31920, 0),
            new Point(32186, 0),
            new Point(32452, 0),
            new Point(32718, 0),
            new Point(32984, 0),
            new Point(33250, 0),
            new Point(33516, 0),
            new Point(33782, 0),
            new Point(34048, 0.673424977),
            new Point(34748.8396, 0.925536601),
            new Point(34748.84066, 0.92534521),
            new Point(34846, 0.587376949),
            new Point(35112, 0.564229653),
            new Point(35378, 0.550628981),
            new Point(35644, 0.600384003),
            new Point(35910, 0.600385727),
            new Point(36176, 0.608486474),
            new Point(36442, 0.608486474),
            new Point(36708, 0.449374461),
            new Point(36974, 0.456710477),
            new Point(37240, 0.457504489),
            new Point(37506, 0),
            new Point(37772, 0)
        ];

        var expectedMatchPoints1 = (Point[])
        [
            new Point(4938.655628, 0.98966226),
            new Point(14898.65624, 0.99189684),
            new Point(24738, 0.804830854),
            new Point(24839.6321, 0.995478021),
            new Point(34748.8396, 0.925536601)
        ];

        var matchPoints1 = Filter.NMS([..points.Where(t => t.Y > 0.7)], 50).Result;
        matchPoints1.Should()
            .BeEquivalentTo(expectedMatchPoints1, options => options.WithStrictOrdering());

        var expectedMatchPoints2 = (Point[])
        [
            new Point(4938.655628, 0.98966226),
            new Point(14898.65624, 0.99189684),
            new Point(24839.6321, 0.995478021)
        ];
        matchPoints1 = Filter.MAD([..matchPoints1.Select(t => t.Y)]).Indexes.Select(t => matchPoints1[t]).ToArray();

        matchPoints1.Should()
            .BeEquivalentTo(expectedMatchPoints2, options => options.WithStrictOrdering());

        var expectedResult = (double[])
        [
            expectedMatchPoints2[1].X - expectedMatchPoints2[0].X,
            expectedMatchPoints2[2].X - expectedMatchPoints2[1].X
        ];

        var result = Filter.MAD([..matchPoints1.Zip(matchPoints1.Skip(1), (prev, next) => next.X - prev.X)]).Result;

        result.Should()
            .BeEquivalentTo(expectedResult, options => options.WithStrictOrdering());
    }
}