using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ChromaMerge.Models.Color;

/// <summary>
/// CSS カラーコードを表現する不変レコード
/// </summary>
public sealed partial record ColorCode
{
    /// <summary>元の文字列 (#fff, #ffffff 等)</summary>
    public string Original { get; }

    /// <summary>正規化形式 (#RRGGBBAA)</summary>
    public string Normalized { get; }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    private ColorCode(string original, byte r, byte g, byte b, byte a)
    {
        Original = original;
        R = r;
        G = g;
        B = b;
        A = a;
        Normalized = $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }

    /// <summary>
    /// カラーコード文字列をパースして ColorCode を生成
    /// </summary>
    /// <param name="input">#RGB, #RGBA, #RRGGBB, #RRGGBBAA 形式の文字列</param>
    /// <exception cref="FormatException">不正な形式の場合</exception>
    public static ColorCode Parse(string input)
    {
        if (!TryParse(input, out var result) || result is null)
        {
            throw new FormatException($"Invalid color code format: '{input}'");
        }
        return result;
    }

    /// <summary>
    /// カラーコード文字列のパースを試行
    /// </summary>
    public static bool TryParse(string input, [NotNullWhen(true)] out ColorCode? result)
    {
        result = null;

        if (string.IsNullOrEmpty(input) || input[0] != '#')
            return false;

        var hex = input[1..];

        if (!HexPattern().IsMatch(hex))
            return false;

        byte r, g, b, a;

        switch (hex.Length)
        {
            case 3: // #RGB
                (r, g, b) = ParseShortRgb(hex);
                a = 255;
                break;

            case 4: // #RGBA
                (r, g, b) = ParseShortRgb(hex);
                a = ParseSingleHex(hex[3]);
                break;

            case 6: // #RRGGBB
                (r, g, b) = ParseLongRgb(hex);
                a = 255;
                break;

            case 8: // #RRGGBBAA
                (r, g, b) = ParseLongRgb(hex);
                a = ParseDoubleHex(hex[6..8]);
                break;

            default:
                return false;
        }

        result = new ColorCode(input, r, g, b, a);
        return true;
    }

    private static (byte R, byte G, byte B) ParseShortRgb(string hex) =>
        (ParseSingleHex(hex[0]), ParseSingleHex(hex[1]), ParseSingleHex(hex[2]));

    private static (byte R, byte G, byte B) ParseLongRgb(string hex) =>
        (ParseDoubleHex(hex[0..2]), ParseDoubleHex(hex[2..4]), ParseDoubleHex(hex[4..6]));

    private static byte ParseSingleHex(char c)
    {
        var value = Convert.ToByte(c.ToString(), 16);
        return (byte)(value * 17); // 0xF -> 0xFF, 0x8 -> 0x88
    }

    private static byte ParseDoubleHex(string hex) =>
        Convert.ToByte(hex, 16);

    [GeneratedRegex("^[0-9A-Fa-f]+$")]
    private static partial Regex HexPattern();

    public bool Equals(ColorCode? other) =>
        other is not null && Normalized == other.Normalized;

    public override int GetHashCode() => Normalized.GetHashCode();
}
