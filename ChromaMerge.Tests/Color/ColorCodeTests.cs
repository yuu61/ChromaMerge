using Xunit;
using FluentAssertions;
using ChromaMerge.Models.Color;

namespace ChromaMerge.Tests.Color;

public class ColorCodeTests
{
    [Theory]
    // #RGB (3桁)
    [InlineData("#fff", 255, 255, 255, 255)]
    [InlineData("#FFF", 255, 255, 255, 255)]
    [InlineData("#000", 0, 0, 0, 255)]
    [InlineData("#f00", 255, 0, 0, 255)]
    [InlineData("#0f0", 0, 255, 0, 255)]
    [InlineData("#00f", 0, 0, 255, 255)]
    // #RGBA (4桁)
    [InlineData("#fff0", 255, 255, 255, 0)]
    [InlineData("#ffff", 255, 255, 255, 255)]
    [InlineData("#f008", 255, 0, 0, 136)] // 8 = 0x88
    // #RRGGBB (6桁)
    [InlineData("#ffffff", 255, 255, 255, 255)]
    [InlineData("#000000", 0, 0, 0, 255)]
    [InlineData("#ff0000", 255, 0, 0, 255)]
    [InlineData("#00ff00", 0, 255, 0, 255)]
    [InlineData("#0000ff", 0, 0, 255, 255)]
    [InlineData("#123456", 0x12, 0x34, 0x56, 255)]
    // #RRGGBBAA (8桁)
    [InlineData("#ffffff00", 255, 255, 255, 0)]
    [InlineData("#ffffffff", 255, 255, 255, 255)]
    [InlineData("#12345678", 0x12, 0x34, 0x56, 0x78)]
    public void Parse_AllFormats_ShouldParseCorrectly(string input, byte r, byte g, byte b, byte a)
    {
        var color = ColorCode.Parse(input);

        color.Should().Match<ColorCode>(c =>
            c.R == r && c.G == g && c.B == b && c.A == a);
    }

    [Theory]
    [InlineData("#fff", "#FFFFFFFF")]
    [InlineData("#ffffff", "#FFFFFFFF")]
    [InlineData("#FFF", "#FFFFFFFF")]
    [InlineData("#123456", "#123456FF")]
    [InlineData("#12345678", "#12345678")]
    public void Parse_ShouldNormalizeToUpperCase8Digit(string input, string expectedNormalized)
    {
        var color = ColorCode.Parse(input);

        color.Normalized.Should().Be(expectedNormalized);
    }

    [Fact]
    public void Parse_ShouldPreserveOriginal()
    {
        var color = ColorCode.Parse("#fff");

        color.Original.Should().Be("#fff");
    }

    [Theory]
    [InlineData("")]
    [InlineData("fff")]
    [InlineData("#ff")]
    [InlineData("#fffff")]
    [InlineData("#ffffffffffff")]
    [InlineData("#gggggg")]
    public void Parse_InvalidInput_ShouldThrow(string input)
    {
        var act = () => ColorCode.Parse(input);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_NullInput_ShouldThrow()
    {
        var act = () => ColorCode.Parse(null!);

        act.Should().Throw<Exception>(); // ArgumentNullException or FormatException
    }

    [Fact]
    public void TryParse_NullInput_ShouldReturnFalse()
    {
        var result = ColorCode.TryParse(null!, out var color);

        result.Should().BeFalse();
        color.Should().BeNull();
    }

    [Fact]
    public void TryParse_ValidInput_ShouldReturnTrue()
    {
        var result = ColorCode.TryParse("#fff", out var color);

        result.Should().BeTrue();
        color.Should().NotBeNull();
        color!.R.Should().Be(255);
    }

    [Fact]
    public void TryParse_InvalidInput_ShouldReturnFalse()
    {
        var result = ColorCode.TryParse("invalid", out var color);

        result.Should().BeFalse();
        color.Should().BeNull();
    }

    [Fact]
    public void Equality_SameNormalizedValue_ShouldBeEqual()
    {
        var color1 = ColorCode.Parse("#fff");
        var color2 = ColorCode.Parse("#ffffff");
        var color3 = ColorCode.Parse("#FFFFFF");

        color1.Should().Be(color2);
        color2.Should().Be(color3);
    }
}
