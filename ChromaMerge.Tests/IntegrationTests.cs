using Xunit;
using FluentAssertions;
using ChromaMerge.Models.Color;
using ChromaMerge.Models.DeltaE;
using ChromaMerge.Models.Grouping;

namespace ChromaMerge.Tests;

/// <summary>
/// ColorCode → ColorConverter → Ciede2000 → UnionFind の統合テスト
/// </summary>
public class IntegrationTests
{
    #region Helper Methods

    /// <summary>
    /// 2つの HEX カラー間の ΔE00 を計算
    /// </summary>
    private static double CalculateDeltaE(string hex1, string hex2)
    {
        var lab1 = ColorConverter.RgbToLab(ColorCode.Parse(hex1));
        var lab2 = ColorConverter.RgbToLab(ColorCode.Parse(hex2));
        return Ciede2000.Calculate(lab1, lab2);
    }

    /// <summary>
    /// 色配列を ΔE00 閾値でグルーピング
    /// </summary>
    private static UnionFind GroupByThreshold(string[] hexColors, double threshold)
    {
        var labs = hexColors
            .Select(hex => ColorConverter.RgbToLab(ColorCode.Parse(hex)))
            .ToArray();

        var uf = new UnionFind(labs.Length);

        for (int i = 0; i < labs.Length; i++)
        {
            for (int j = i + 1; j < labs.Length; j++)
            {
                if (Ciede2000.Calculate(labs[i], labs[j]) <= threshold)
                {
                    uf.Union(i, j);
                }
            }
        }

        return uf;
    }

    #endregion

    [Fact]
    public void SimilarColors_ShouldHaveLowDeltaE()
    {
        var deltaE = CalculateDeltaE("#ff0000", "#fe0000");

        deltaE.Should().BeLessThan(1.0); // 知覚的にほぼ同じ
    }

    [Fact]
    public void DifferentColors_ShouldHaveHighDeltaE()
    {
        var deltaE = CalculateDeltaE("#ff0000", "#0000ff");

        deltaE.Should().BeGreaterThan(50.0); // 明確に異なる色
    }

    [Fact]
    public void SameColorDifferentFormat_ShouldHaveZeroDeltaE()
    {
        CalculateDeltaE("#fff", "#ffffff").Should().Be(0.0);
        CalculateDeltaE("#ffffff", "#FFFFFF").Should().Be(0.0);
    }

    [Theory]
    [InlineData("#ff0000", "#fe0000", 1.5)]    // 微差 → 近い
    [InlineData("#ff0000", "#f00000", 5.0)]    // 小差 → やや近い
    [InlineData("#ff0000", "#00ff00", 120.0)]  // 赤と緑 → 遠い
    public void DeltaE_VariousColorPairs_ShouldBeWithinExpectedRange(
        string hex1, string hex2, double maxExpected)
    {
        var deltaE = CalculateDeltaE(hex1, hex2);

        deltaE.Should().BeLessThan(maxExpected);
    }

    [Fact]
    public void ColorGrouping_SimilarColors_ShouldBeGrouped()
    {
        var colors = new[]
        {
            "#ff0000", // 0: 赤
            "#fe0101", // 1: ほぼ赤
            "#ff0102", // 2: ほぼ赤
            "#0000ff", // 3: 青 (別グループ)
            "#0001fe", // 4: ほぼ青
        };

        var uf = GroupByThreshold(colors, threshold: 3.0);

        // 赤系 (0,1,2) と青系 (3,4) の 2 グループ
        uf.GroupCount.Should().Be(2);
        uf.Connected(0, 1).Should().BeTrue();
        uf.Connected(0, 2).Should().BeTrue();
        uf.Connected(3, 4).Should().BeTrue();
        uf.Connected(0, 3).Should().BeFalse();
    }

    [Fact]
    public void FullPipeline_CssColorGrouping_Simulation()
    {
        var cssColors = new[]
        {
            "#333333", // 0: ダークグレー
            "#333",    // 1: 同じ (短縮形)
            "#343434", // 2: ほぼ同じ
            "#ffffff", // 3: 白
            "#fff",    // 4: 同じ (短縮形)
            "#fefefe", // 5: ほぼ白
            "#ff0000", // 6: 赤
            "#f00",    // 7: 同じ (短縮形)
        };

        var uf = GroupByThreshold(cssColors, threshold: 2.0);

        // 3 グループ: ダークグレー系、白系、赤系
        uf.GroupCount.Should().Be(3);

        // ダークグレー系
        uf.Connected(0, 1).Should().BeTrue();
        uf.Connected(0, 2).Should().BeTrue();

        // 白系
        uf.Connected(3, 4).Should().BeTrue();
        uf.Connected(3, 5).Should().BeTrue();

        // 赤系
        uf.Connected(6, 7).Should().BeTrue();
    }
}
