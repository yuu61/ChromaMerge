namespace ChromaMerge.Models.Color;

/// <summary>
/// CIE L*a*b* 色空間の色を表現するレコード
/// </summary>
/// <param name="L">明度 (0-100)</param>
/// <param name="A">赤-緑軸 (通常 -128 から +127)</param>
/// <param name="B">黄-青軸 (通常 -128 から +127)</param>
public readonly record struct LabColor(double L, double A, double B);
