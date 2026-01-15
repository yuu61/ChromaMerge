using System;

namespace ChromaMerge.Models.Color;

/// <summary>
/// RGB と CIE L*a*b* 色空間間の変換を行う静的クラス
/// </summary>
public static class ColorConverter
{
    // D65 標準光源の参照白色点
    private const double RefX = 95.047;
    private const double RefY = 100.000;
    private const double RefZ = 108.883;

    /// <summary>
    /// RGB 値を CIE L*a*b* に変換
    /// </summary>
    public static LabColor RgbToLab(byte r, byte g, byte b)
    {
        var (x, y, z) = RgbToXyz(r, g, b);
        return XyzToLab(x, y, z);
    }

    /// <summary>
    /// ColorCode を CIE L*a*b* に変換
    /// </summary>
    public static LabColor RgbToLab(ColorCode color) =>
        RgbToLab(color.R, color.G, color.B);

    /// <summary>
    /// RGB を XYZ 色空間に変換 (D65 照明、sRGB)
    /// </summary>
    private static (double X, double Y, double Z) RgbToXyz(byte r, byte g, byte b)
    {
        // sRGB から線形 RGB への変換
        double rLinear = PivotRgb(r / 255.0);
        double gLinear = PivotRgb(g / 255.0);
        double bLinear = PivotRgb(b / 255.0);

        // sRGB → XYZ 変換行列 (D65)
        double x = rLinear * 41.24564 + gLinear * 35.75761 + bLinear * 18.04375;
        double y = rLinear * 21.26729 + gLinear * 71.51522 + bLinear * 7.21750;
        double z = rLinear * 1.93339 + gLinear * 11.91920 + bLinear * 95.03041;

        return (x, y, z);
    }

    /// <summary>
    /// XYZ を CIE L*a*b* に変換
    /// </summary>
    private static LabColor XyzToLab(double x, double y, double z)
    {
        double xr = PivotXyz(x / RefX);
        double yr = PivotXyz(y / RefY);
        double zr = PivotXyz(z / RefZ);

        double l = 116.0 * yr - 16.0;
        double a = 500.0 * (xr - yr);
        double b = 200.0 * (yr - zr);

        return new LabColor(l, a, b);
    }

    /// <summary>
    /// sRGB ガンマ補正の逆変換
    /// </summary>
    private static double PivotRgb(double n)
    {
        return n > 0.04045
            ? Math.Pow((n + 0.055) / 1.055, 2.4)
            : n / 12.92;
    }

    /// <summary>
    /// XYZ → Lab 変換用の関数
    /// </summary>
    private static double PivotXyz(double n)
    {
        const double epsilon = 216.0 / 24389.0; // 0.008856
        const double kappa = 24389.0 / 27.0;    // 903.3

        return n > epsilon
            ? Math.Cbrt(n)
            : (kappa * n + 16.0) / 116.0;
    }
}
