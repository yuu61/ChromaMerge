using Xunit;
using FluentAssertions;
using ChromaMerge.Models.Color;

namespace ChromaMerge.Tests.Color;

public class ColorConverterTests
{
    // 参考値は https://colormine.org/convert/rgb-to-lab などで検証

    [Fact]
    public void RgbToLab_White_ShouldReturnCorrectLab()
    {
        var lab = ColorConverter.RgbToLab(255, 255, 255);

        lab.L.Should().BeApproximately(100.0, 0.01);
        lab.A.Should().BeApproximately(0.0, 0.01);
        lab.B.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void RgbToLab_Black_ShouldReturnCorrectLab()
    {
        var lab = ColorConverter.RgbToLab(0, 0, 0);

        lab.L.Should().BeApproximately(0.0, 0.01);
        lab.A.Should().BeApproximately(0.0, 0.01);
        lab.B.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void RgbToLab_Red_ShouldReturnCorrectLab()
    {
        var lab = ColorConverter.RgbToLab(255, 0, 0);

        // 純粋な赤: L≈53.23, a≈80.11, b≈67.22
        lab.L.Should().BeApproximately(53.23, 0.5);
        lab.A.Should().BeApproximately(80.11, 0.5);
        lab.B.Should().BeApproximately(67.22, 0.5);
    }

    [Fact]
    public void RgbToLab_Green_ShouldReturnCorrectLab()
    {
        var lab = ColorConverter.RgbToLab(0, 255, 0);

        // 純粋な緑: L≈87.74, a≈-86.18, b≈83.18
        lab.L.Should().BeApproximately(87.74, 0.5);
        lab.A.Should().BeApproximately(-86.18, 0.5);
        lab.B.Should().BeApproximately(83.18, 0.5);
    }

    [Fact]
    public void RgbToLab_Blue_ShouldReturnCorrectLab()
    {
        var lab = ColorConverter.RgbToLab(0, 0, 255);

        // 純粋な青: L≈32.30, a≈79.20, b≈-107.86
        lab.L.Should().BeApproximately(32.30, 0.5);
        lab.A.Should().BeApproximately(79.20, 0.5);
        lab.B.Should().BeApproximately(-107.86, 0.5);
    }

    [Theory]
    [InlineData(128, 128, 128, 53.59, 0.0, 0.0)] // グレー
    [InlineData(255, 255, 0, 97.14, -21.56, 94.48)] // 黄色
    [InlineData(0, 255, 255, 91.11, -48.09, -14.13)] // シアン
    [InlineData(255, 0, 255, 60.32, 98.25, -60.84)] // マゼンタ
    public void RgbToLab_KnownColors_ShouldReturnCorrectLab(
        byte r, byte g, byte b,
        double expectedL, double expectedA, double expectedB)
    {
        var lab = ColorConverter.RgbToLab(r, g, b);

        lab.L.Should().BeApproximately(expectedL, 1.0);
        lab.A.Should().BeApproximately(expectedA, 1.0);
        lab.B.Should().BeApproximately(expectedB, 1.0);
    }

    [Fact]
    public void RgbToLab_FromColorCode_ShouldWork()
    {
        var color = ColorCode.Parse("#ff0000");
        var lab = ColorConverter.RgbToLab(color);

        lab.L.Should().BeApproximately(53.23, 0.5);
    }

    [Theory]
    [InlineData(0, 0, 0)]       // 最小値 (黒)
    [InlineData(255, 255, 255)] // 最大値 (白)
    [InlineData(1, 1, 1)]       // 最小に近い
    [InlineData(254, 254, 254)] // 最大に近い
    [InlineData(0, 255, 0)]     // 極端な緑
    [InlineData(255, 0, 255)]   // 極端なマゼンタ
    public void RgbToLab_BoundaryValues_ShouldNotThrow(byte r, byte g, byte b)
    {
        var act = () => ColorConverter.RgbToLab(r, g, b);

        act.Should().NotThrow();
    }

    [Fact]
    public void RgbToLab_AllValues_ShouldProduceValidLabRange()
    {
        // L は 0-100, a と b は通常 -128 から +127 の範囲
        // 浮動小数点誤差を考慮して少し余裕を持たせる
        for (int r = 0; r <= 255; r += 51) // 0, 51, 102, 153, 204, 255
        {
            for (int g = 0; g <= 255; g += 51)
            {
                for (int b = 0; b <= 255; b += 51)
                {
                    var lab = ColorConverter.RgbToLab((byte)r, (byte)g, (byte)b);

                    lab.L.Should().BeInRange(-0.01, 100.01);
                    lab.A.Should().BeInRange(-150.0, 150.0);
                    lab.B.Should().BeInRange(-150.0, 150.0);
                }
            }
        }
    }

    [Fact]
    public void RgbToLab_GrayscaleValues_ShouldHaveZeroChroma()
    {
        // グレースケールは a=0, b=0 になるべき
        for (int v = 0; v <= 255; v += 51)
        {
            var lab = ColorConverter.RgbToLab((byte)v, (byte)v, (byte)v);

            lab.A.Should().BeApproximately(0.0, 0.01);
            lab.B.Should().BeApproximately(0.0, 0.01);
        }
    }
}
