
namespace DreamPotato.MonoGame;

using System;
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

    private Matrix _spriteTransformMatrix;

    private const int MinVmuScale = 3;
    private const int ScaledWidth = Display.ScreenWidth * MinVmuScale;
    private const int ScaledHeight = Display.ScreenHeight * MinVmuScale;

    private const int IconSize = 32;

    private const int TopMargin = MinVmuScale * 2;

    private const int SideMargin = MinVmuScale * 3;
    private const int BottomMargin = MinVmuScale * 12;

    internal const int TotalContentWidth = ScaledWidth + SideMargin * 2;
    internal const int TotalContentHeight = ScaledHeight + TopMargin + BottomMargin;

    internal void Draw(SpriteBatch _spriteBatch)
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

        // Use nearest neighbor scaling for the screen content
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _spriteTransformMatrix);

        var vmuIsEjected = Vmu.IsEjected;
        var screenSize = new Point(x: ScaledWidth, y: ScaledHeight);
        var screenRectangle = vmuIsEjected
            ? new Rectangle(new Point(x: SideMargin, y: TopMargin), screenSize)
            : new Rectangle(location: new Point(x: SideMargin, y: TopMargin + IconSize), screenSize);

        _spriteBatch.Draw(
            _vmuScreenTexture,
            destinationRectangle: screenRectangle,
            sourceRectangle: null,
            color: Color.White,
            rotation: 0,
            origin: default,
            effects: vmuIsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
            layerDepth: 0);
        _spriteBatch.End();

        // Draw icons
        // If we are at an even multiple of the base screen size, or, if the screen is just huge, use nearest neighbor scaling
        var iconsSamplerState = Math.Floor(_spriteTransformMatrix.Down.Y) == _spriteTransformMatrix.Down.Y || _spriteTransformMatrix.Down.Y < -3
            ? SamplerState.PointClamp
            : SamplerState.LinearWrap;
        _spriteBatch.Begin(samplerState: iconsSamplerState, transformMatrix: _spriteTransformMatrix);
        int iconsYPos = vmuIsEjected ? TopMargin + ScaledHeight + MinVmuScale / 2 : TopMargin - MinVmuScale / 2;
        const int iconSpacing = MinVmuScale * 2;
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
                ? new Rectangle(location: new Point(x: SideMargin + iconSpacing * ordinal + IconSize * ordinal, y: iconsYPos), iconSize)
                : new Rectangle(location: new Point(x: SideMargin + iconSpacing * (maxPosition - ordinal) + IconSize * (maxPosition - ordinal), y: iconsYPos), iconSize);

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

    /// <summary>Update internal transform matrix in order to draw the VMU within 'targetRectangle'.</summary>
    internal void UpdateScaleMatrix(Rectangle targetRectangle, bool preserveAspectRatio)
    {
        // Apply transforms:
        // - scale based on window size
        // - translate horizontally to ensure our content is centered

        // MinVmuScale is 3, and this is the base scale used for drawing.
        // But, we want to support 4x, 5x, etc. as the user resizes the window.
        // Therefore, get a scale based on the screen size, and round it down to the nearest 1/3 on each axis.
        float widthScale = (float)Math.Floor(
            (float)targetRectangle.Width / TotalContentWidth * MinVmuScale) / MinVmuScale;
        float heightScale = (float)Math.Floor(
            (float)targetRectangle.Height / TotalContentHeight * MinVmuScale) / MinVmuScale;

        float minScale = Math.Min(widthScale, heightScale);
        Matrix scaleTransform = preserveAspectRatio
            ? Matrix.CreateScale(minScale, minScale, 1)
            : Matrix.CreateScale(widthScale, heightScale, 1);
        _spriteTransformMatrix = scaleTransform * getTranslationTransform();

        Matrix getTranslationTransform()
        {
            float scale = preserveAspectRatio ? minScale : widthScale;
            float idealWidth = scale * TotalContentWidth;

            // Center the content horizontally, by shifting it over
            // |--------| actual
            // __|----|__ ideal
            // We are calculating the quantity denoted by '__' in the above sketch
            // Since we're using nearest neighbor scaling, we need to round to nearest integer, to keep it from looking blocky.
            var xPosition = targetRectangle.X + (float)Math.Round((targetRectangle.Width - idealWidth) / 2);
            return Matrix.CreateTranslation(xPosition, yPosition: targetRectangle.Y, zPosition: 0);
        }
    }
}