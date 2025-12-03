using AwesomeAssertions;
using Core.Utilities;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Plottables;
using ScottPlot;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace CugaCalibrationUnitTest;

public sealed class BoltzmannTest
{
    [Fact]
    public void BoltzmannFitTest()
    {
        var x = Vector<double>.Build.Dense([
            -9.9,
            -9.7,
            -9.5,
            -9.3,
            -9.1,
            -8.9,
            -8.7,
            -8.5,
            -8.3,
            -8.1,
            -7.9,
            -7.7,
            -7.5,
            -7.3,
            -7.1,
            -6.9,
            -6.7,
            -6.5,
            -6.3,
            -6.1,
            -5.9,
            -5.7,
            -5.5,
            -5.3,
            -5.1,
            -4.9,
            -4.7,
            -4.5,
            -4.3,
            -4.1,
            -3.9,
            -3.7,
            -3.5,
            -3.3,
            -3.1,
            -2.9,
            -2.7,
            -2.5,
            -2.3,
            -2.1,
            -1.9,
            -1.7,
            -1.5,
            -1.3,
            -1.1,
            -0.9,
            -0.7,
            -0.5,
            -0.3,
            -0.1,
            0.1,
            0.3,
            0.5,
            0.7,
            0.9,
            1.1,
            1.3,
            1.5,
            1.7,
            1.9,
            2.1,
            2.3,
            2.5,
            2.7,
            2.9,
            3.1,
            3.3,
            3.5,
            3.7,
            3.9,
            4.1,
            4.3,
            4.5,
            4.7,
            4.9,
            5.1,
            5.3,
            5.5,
            5.7,
            5.9,
            6.1,
            6.3,
            6.5,
            6.7,
            6.9,
            7.1,
            7.3,
            7.5,
            7.7,
            7.9,
            8.1,
            8.3,
            8.5,
            8.7,
            8.9,
            9.1,
            9.3,
            9.5,
            9.7,
            9.9
        ]);
        var y = Vector<double>.Build.Dense([
            9.13657107,
            9.178798522,
            9.155088256,
            9.738515429,
            10.01068483,
            9.948332577,
            9.64953821,
            9.684991597,
            9.867677504,
            9.202326796,
            9.323056226,
            9.302901107,
            9.423914725,
            9.665668583,
            9.654596366,
            9.329990139,
            9.427243358,
            9.512309136,
            9.600345342,
            9.691032254,
            9.806591938,
            9.907886189,
            10.03739623,
            10.16076781,
            10.27931019,
            10.43589504,
            10.56336144,
            10.70501737,
            10.8853921,
            11.05153066,
            11.18115602,
            11.3737858,
            12.18095678,
            11.88268983,
            12.20930543,
            12.61969834,
            12.83630392,
            13.16079378,
            13.81991417,
            14.34507303,
            14.57080978,
            14.40931963,
            14.5937891,
            14.988615,
            14.64261329,
            14.93927086,
            15.04483497,
            15.24946501,
            15.63262194,
            15.72761616,
            15.56089283,
            15.71040301,
            16.02601862,
            16.15882022,
            16.29555766,
            16.41686689,
            16.56326716,
            16.95200502,
            17.05799677,
            17.15471986,
            17.29612173,
            17.39731294,
            17.52774491,
            17.64625623,
            18.16924947,
            18.3019156,
            18.387991,
            18.48050351,
            18.96888563,
            19.01726969,
            19.25167329,
            19.40822376,
            19.38318604,
            19.85250789,
            20.08806731,
            20.11591692,
            19.87481766,
            21.43801156,
            21.64146429,
            21.12045142,
            21.21855862,
            21.18629984,
            21.31347633,
            21.48660255,
            21.52285903,
            21.43400411,
            21.49507468,
            21.56830408,
            21.65763174,
            21.71395311,
            21.80384554,
            21.81377845,
            21.93139111,
            21.96809011,
            22.03339925,
            22.07694084,
            22.10318002,
            22.16879768,
            22.22731376,
            22.25295627
        ]);

        var (a1, a2, x0, dx, rSquared, yPredicted) = Boltzmann.BoltzmannFit(x, y);

        using var plot = new Plot();
        plot.Title("Boltzmann Fit");
        var scatterLine = ScatterLine.Empty;
        scatterLine.Update(
            "Origin Curve",
            [.. x.Index().Select(t => new Point(t.Item, y[t.Index]))],
            Colors.Blue);
        lock (plot.Sync) plot.PlottableList.Add(scatterLine);
        scatterLine = ScatterLine.Empty;
        scatterLine.Update(
            $"Fit Curve: y = {a2:0.######} + ({a1:0.######} - {a2:0.######}) / (1 + exp((x - {x0:0.######}) / {dx:0.######})) r^2 = {rSquared:0.######}",
            [.. x.Index().Select(t => new Point(t.Item, yPredicted[t.Index]))],
            Colors.Red);
        lock (plot.Sync) plot.PlottableList.Add(scatterLine);

        plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);

        var imageFullPath = Path.GetFullPath($"boltzmann_fit{Guid.NewGuid():N}.png");
        plot.SavePng(imageFullPath, 1920, 1080);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = imageFullPath,
            UseShellExecute = true
        });

        // File.Delete(imageFullPath);
        /*
         * % Guess Param: A1=22.252956, A2=9.136571, x0=-0.100000, dx=1.000000
         * % 可能存在局部最小值。
         * % lsqcurvefit 已停止，因为平方和相对于其初始值的最终变化小于函数容差值。
         * % Result: A1=8.114388, A2=23.196753, x0=0.201398, dx=3.473488, R2=0.991339
         */
        a1.Should().BeApproximately(8.114388, 1e-4);
        a2.Should().BeApproximately(23.196753, 1e-4);
        x0.Should().BeApproximately(0.201398, 1e-4);
        dx.Should().BeApproximately(3.473488, 1e-4);
        rSquared.Should().BeApproximately(0.991339, 1e-4);
    }
}