using System;
using ChromaMerge.Models.Color;

namespace ChromaMerge.Models.DeltaE;

/// <summary>
/// CIEDE2000 (ΔE00) 色差計算
/// 参照: http://www2.ece.rochester.edu/~gsharma/ciede2000/
/// </summary>
public static class Ciede2000
{
    private const double Deg2Rad = Math.PI / 180.0;
    private const double Rad2Deg = 180.0 / Math.PI;
    private const double Pow25To7 = 6103515625.0; // 25^7

    /// <summary>
    /// 2つの Lab 色間の CIEDE2000 色差を計算
    /// </summary>
    public static double Calculate(LabColor lab1, LabColor lab2)
    {
        // 重み係数 (標準値 = 1)
        const double kL = 1.0;
        const double kC = 1.0;
        const double kH = 1.0;

        double l1 = lab1.L, a1 = lab1.A, b1 = lab1.B;
        double l2 = lab2.L, a2 = lab2.A, b2 = lab2.B;

        // Step 1: C'とh'の計算
        double c1 = Math.Sqrt(a1 * a1 + b1 * b1);
        double c2 = Math.Sqrt(a2 * a2 + b2 * b2);
        double cBar = (c1 + c2) / 2.0;

        double cBar7 = Math.Pow(cBar, 7);
        double g = 0.5 * (1.0 - Math.Sqrt(cBar7 / (cBar7 + Pow25To7)));

        double a1Prime = a1 * (1.0 + g);
        double a2Prime = a2 * (1.0 + g);

        double c1Prime = Math.Sqrt(a1Prime * a1Prime + b1 * b1);
        double c2Prime = Math.Sqrt(a2Prime * a2Prime + b2 * b2);

        double h1Prime = ComputeHPrime(a1Prime, b1);
        double h2Prime = ComputeHPrime(a2Prime, b2);

        // Step 2: ΔL', ΔC', ΔH'の計算
        double deltaLPrime = l2 - l1;
        double deltaCPrime = c2Prime - c1Prime;
        double deltaHPrime = ComputeDeltaHPrime(c1Prime, c2Prime, h1Prime, h2Prime);

        // Step 3: CIEDE2000色差の計算
        double lBarPrime = (l1 + l2) / 2.0;
        double cBarPrime = (c1Prime + c2Prime) / 2.0;
        double hBarPrime = ComputeHBarPrime(c1Prime, c2Prime, h1Prime, h2Prime);

        double t = 1.0
            - 0.17 * Math.Cos((hBarPrime - 30.0) * Deg2Rad)
            + 0.24 * Math.Cos(2.0 * hBarPrime * Deg2Rad)
            + 0.32 * Math.Cos((3.0 * hBarPrime + 6.0) * Deg2Rad)
            - 0.20 * Math.Cos((4.0 * hBarPrime - 63.0) * Deg2Rad);

        double lBarPrime50 = lBarPrime - 50.0;
        double sl = 1.0 + (0.015 * lBarPrime50 * lBarPrime50) / Math.Sqrt(20.0 + lBarPrime50 * lBarPrime50);
        double sc = 1.0 + 0.045 * cBarPrime;
        double sh = 1.0 + 0.015 * cBarPrime * t;

        double deltaTheta = 30.0 * Math.Exp(-Math.Pow((hBarPrime - 275.0) / 25.0, 2));

        double cBarPrime7 = Math.Pow(cBarPrime, 7);
        double rc = 2.0 * Math.Sqrt(cBarPrime7 / (cBarPrime7 + Pow25To7));
        double rt = -rc * Math.Sin(2.0 * deltaTheta * Deg2Rad);

        double deltaL = deltaLPrime / (kL * sl);
        double deltaC = deltaCPrime / (kC * sc);
        double deltaH = deltaHPrime / (kH * sh);

        return Math.Sqrt(
            deltaL * deltaL +
            deltaC * deltaC +
            deltaH * deltaH +
            rt * deltaC * deltaH);
    }

    private static double ComputeHPrime(double aPrime, double b)
    {
        if (Math.Abs(aPrime) < 1e-10 && Math.Abs(b) < 1e-10)
            return 0.0;

        double h = Math.Atan2(b, aPrime) * Rad2Deg;
        return h >= 0 ? h : h + 360.0;
    }

    private static double ComputeDeltaHPrime(double c1Prime, double c2Prime, double h1Prime, double h2Prime)
    {
        // 非常に小さいchroma値は0として扱う（浮動小数点の丸め誤差による不安定性を回避）
        const double epsilon = 1e-10;
        if (c1Prime < epsilon || c2Prime < epsilon)
            return 0.0;

        double dhPrime = h2Prime - h1Prime;

        if (dhPrime > 180.0)
            dhPrime -= 360.0;
        else if (dhPrime < -180.0)
            dhPrime += 360.0;

        return 2.0 * Math.Sqrt(c1Prime * c2Prime) * Math.Sin(dhPrime * Deg2Rad / 2.0);
    }

    private static double ComputeHBarPrime(double c1Prime, double c2Prime, double h1Prime, double h2Prime)
    {
        // 非常に小さいchroma値は0として扱う（浮動小数点の丸め誤差による不安定性を回避）
        const double epsilon = 1e-10;
        if (c1Prime < epsilon || c2Prime < epsilon)
            return h1Prime + h2Prime;

        double hSum = h1Prime + h2Prime;
        double hDiff = Math.Abs(h1Prime - h2Prime);

        if (hDiff <= 180.0)
            return hSum / 2.0;

        return hSum < 360.0
            ? (hSum + 360.0) / 2.0
            : (hSum - 360.0) / 2.0;
    }
}
