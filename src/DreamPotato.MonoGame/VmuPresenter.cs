
namespace DreamPotato.MonoGame;

using System.Diagnostics;

using DreamPotato.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Exposes a <see cref="Vmu"/> and drawing methods.
/// </summary>
class VmuPresenter(Vmu Vmu, IconTextures IconTextures, GraphicsDeviceManager Graphics)
{
    private readonly Color[] _vmuScreenData = new Color[Display.ScreenWidth * Display.ScreenHeight];
    internal required ColorPalette ColorPalette { get; set; }

    internal readonly Vmu Vmu = Vmu;
    internal readonly IconTextures IconTextures = IconTextures;
    internal readonly GraphicsDeviceManager Graphics = Graphics;
    internal readonly SpriteBatch _spriteBatch = new SpriteBatch(Graphics.GraphicsDevice);
    internal readonly Texture2D _vmuScreenTexture = new Texture2D(Graphics.GraphicsDevice, Display.ScreenWidth, Display.ScreenHeight);
    internal readonly Texture2D _vmuBorderTexture = new Texture2D(Graphics.GraphicsDevice, 1, 1);
    internal readonly Texture2D _vmuMarginTexture = new Texture2D(Graphics.GraphicsDevice, 1, 1);

    // TODO: eventually, there should be UI to permit a non-constant scale.
    private const int VmuScale = 6;
    private const int ScaledWidth = Display.ScreenWidth * VmuScale;
    private const int ScaledHeight = Display.ScreenHeight * VmuScale;

    private const int IconSize = 64;

    private const int MenuBarHeight = 20;
    private const int TopMargin = VmuScale * 2 + MenuBarHeight;

    private const int SideMargin = VmuScale * 3;
    private const int BorderMargin = SideMargin;
    private const int BottomMargin = VmuScale * 12;

    internal const int TotalScreenWidth = ScaledWidth + SideMargin * 2 + BorderMargin * 2;
    internal const int TotalScreenHeight = ScaledHeight + TopMargin + BottomMargin + BorderMargin * 2;

    internal void Draw()
    {
        var screenData = Vmu.Display.GetBytes();
        int i = 0;
        foreach (byte b in screenData)
        {
            _vmuScreenData[i++] = ReadColor(b, 7);
            _vmuScreenData[i++] = ReadColor(b, 6);
            _vmuScreenData[i++] = ReadColor(b, 5);
            _vmuScreenData[i++] = ReadColor(b, 4);
            _vmuScreenData[i++] = ReadColor(b, 3);
            _vmuScreenData[i++] = ReadColor(b, 2);
            _vmuScreenData[i++] = ReadColor(b, 1);
            _vmuScreenData[i++] = ReadColor(b, 0);
        }
        _vmuScreenTexture.SetData(_vmuScreenData);

        // Use nearest neighbor scaling
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var borderColor = Vmu.Color;
        _vmuBorderTexture.SetData([new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a)]);
        _spriteBatch.Draw(_vmuBorderTexture, destinationRectangle: new Rectangle(x: 0, y: 0, width: TotalScreenWidth, height: TotalScreenHeight), color: Color.LightGray);

        _vmuMarginTexture.SetData([ColorPalette.Margin]);
        _spriteBatch.Draw(_vmuMarginTexture, destinationRectangle: new Rectangle(x: BorderMargin, y: BorderMargin, width: TotalScreenWidth - BorderMargin * 2, height: TotalScreenHeight - BorderMargin * 2), color: Color.White);

        var vmuIsEjected = Vmu.IsEjected;
        var screenSize = new Point(x: ScaledWidth, y: ScaledHeight);
        var screenRectangle = vmuIsEjected
            ? new Rectangle(new Point(x: SideMargin + BorderMargin, y: TopMargin + BorderMargin), screenSize)
            : new Rectangle(location: new Point(x: SideMargin + BorderMargin, y: TopMargin + BorderMargin + IconSize), screenSize);

        _spriteBatch.Draw(
            _vmuScreenTexture,
            destinationRectangle: screenRectangle,
            sourceRectangle: null,
            color: Color.White,
            rotation: 0,
            origin: default,
            effects: vmuIsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
            layerDepth: 0);

        // Draw icons
        int iconsYPos = vmuIsEjected ? TopMargin + BorderMargin + ScaledHeight + VmuScale / 2 : TopMargin + BorderMargin - VmuScale / 2;
        const int iconSpacing = VmuScale * 2;
        var icons = Vmu.Display.GetIcons();

        var displayOn = Vmu._cpu.SFRs.Vccr.DisplayControl;
        if (displayOn)
        {
            drawIcon(IconTextures.IconFileTexture, ordinal: 0, enabled: (icons & Icons.File) != 0);
            drawIcon(IconTextures.IconGameTexture, ordinal: 1, enabled: (icons & Icons.Game) != 0);
            drawIcon(IconTextures.IconClockTexture, ordinal: 2, enabled: (icons & Icons.Clock) != 0);
            drawIcon(IconTextures.IconIOTexture, ordinal: 3, enabled: (icons & Icons.Flash) != 0);
        }
        else
        {
            drawIcon(IconTextures.IconSleepTexture, ordinal: 3, enabled: true);
        }

        void drawIcon(Texture2D texture, int ordinal, bool enabled)
        {
            const int maxPosition = 3;
            Debug.Assert(ordinal is >= 0 and <= maxPosition);

            var iconColor = enabled ? ColorPalette.Icon1 : ColorPalette.Icon0;
            var iconSize = new Point(IconSize);
            var iconRectangle = vmuIsEjected
                ? new Rectangle(location: new Point(x: SideMargin + BorderMargin + iconSpacing * ordinal + IconSize * ordinal, y: iconsYPos), iconSize)
                : new Rectangle(location: new Point(x: SideMargin + BorderMargin + iconSpacing * (maxPosition - ordinal) + IconSize * (maxPosition - ordinal), y: iconsYPos), iconSize);

            _spriteBatch.Draw(
                texture,
                destinationRectangle: iconRectangle,
                sourceRectangle: null,
                color: iconColor,
                rotation: 0,
                origin: default,
                effects: vmuIsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
                layerDepth: 0);
        }

        _spriteBatch.End();

        Color ReadColor(byte b, byte bitAddress)
        {
            return BitHelpers.ReadBit(b, bitAddress) ? ColorPalette.Screen1 : ColorPalette.Screen0;
        }
    }
}