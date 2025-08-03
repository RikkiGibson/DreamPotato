
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Xna.Framework;

namespace DreamPotato.MonoGame;

public record class ColorPalette(string Name, Color Screen1, Color Screen0, Color Margin, Color Icon1, Color Icon0)
{
    public static ColorPalette White { get; } = new ColorPalette(
        Name: "White",
        Screen1: Color.Black,
        Screen0: Color.White,
        Margin: Color.LightGray,
        Icon1: Color.Black,
        Icon0: Color.DarkGray);

    public static ColorPalette DarkGreen { get; } = new ColorPalette(
        Name: "Dark Green",
        Screen1: new Color(0x23, 0x10, 0x24),
        Screen0: new Color(0x3e, 0x4e, 0x44),
        Margin: new Color(0x4d, 0x59, 0x4b),
        Icon1: new Color(0x23, 0x10, 0x24),
        Icon0: new Color(0x39, 0x46, 0x3c));

    public static ColorPalette VM2 { get; } = new ColorPalette(
        Name: "VM2",
        Screen1: Color.Black,
        Screen0: new Color(0x9f, 0xce, 0xde),
        Margin: new Color(0xb5, 0xd3, 0xdd),
        Icon1: Color.Black,
        Icon0: new Color(0x82, 0xa9, 0xb6));

    public static ImmutableArray<ColorPalette> AllPalettes { get; } = [White, DarkGreen, VM2];

    // array type used for imgui
    public static string[] AllPaletteNames = [.. AllPalettes.Select(palette => palette.Name)];
}
