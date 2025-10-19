
namespace DreamPotato.MonoGame;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using DreamPotato.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

/// <summary>
/// Exposes a <see cref="Vmu"/> and drawing methods.
/// </summary>
class VmuPresenter
{
    private readonly Game1 _game1;
    private Configuration Configuration => _game1.Configuration;
    private ColorPalette ColorPalette => _game1.ColorPalette;
    private bool Paused => _game1.Paused;
    private bool IsFastForwarding => _game1.IsFastForwarding;

    private readonly Color[] _vmuScreenData = new Color[Display.ScreenWidth * Display.ScreenHeight];
    private readonly DynamicSoundEffectInstance _dynamicSound;
    internal ButtonChecker ButtonChecker { get; private set; }

    internal readonly Vmu Vmu;
    internal readonly IconTextures IconTextures;
    internal readonly GraphicsDeviceManager Graphics;
    internal readonly Texture2D _vmuScreenTexture;
    internal readonly Texture2D _vmuBorderTexture;
    internal readonly Texture2D _vmuMarginTexture;

    private Matrix _spriteTransformMatrix;
    private int SleepHeldFrameCount;

    private const int MinVmuScale = 3;
    private const int ScaledWidth = Display.ScreenWidth * MinVmuScale;
    private const int ScaledHeight = Display.ScreenHeight * MinVmuScale;

    private const int IconSize = 32;

    private const int TopMargin = MinVmuScale * 2;

    private const int SideMargin = MinVmuScale * 3;
    private const int BottomMargin = MinVmuScale * 12;

    internal const int TotalContentWidth = ScaledWidth + SideMargin * 2;
    internal const int TotalContentHeight = ScaledHeight + TopMargin + BottomMargin;

    public VmuPresenter(Game1 game1, Vmu vmu, IconTextures iconTextures, GraphicsDeviceManager graphics, InputMappings inputMappings)
    {
        _game1 = game1;
        Vmu = vmu;
        IconTextures = iconTextures;
        Graphics = graphics;
        UpdateButtonChecker(inputMappings);

        _vmuScreenTexture = new Texture2D(graphics.GraphicsDevice, Display.ScreenWidth, Display.ScreenHeight);
        _vmuBorderTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
        _vmuMarginTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);

        _dynamicSound = new DynamicSoundEffectInstance(Audio.SampleRate, AudioChannels.Mono);
        _dynamicSound.Play();
        vmu.Audio.AudioBufferReady += Audio_BufferReady;
    }

    [MemberNotNull(nameof(MonoGame.ButtonChecker))]
    internal void UpdateButtonChecker(InputMappings inputMappings)
    {
        ButtonChecker = new ButtonChecker(inputMappings);
    }

    internal void Update(GameTime gameTime, KeyboardState previousKeys, GamePadState previousGamepad, KeyboardState keyboard, GamePadState gamepad)
    {
        var newP3 = new Core.SFRs.P3()
        {
            Up = !ButtonChecker.IsPressed(VmuButton.Up, keyboard, gamepad),
            Down = !ButtonChecker.IsPressed(VmuButton.Down, keyboard, gamepad),
            Left = !ButtonChecker.IsPressed(VmuButton.Left, keyboard, gamepad),
            Right = !ButtonChecker.IsPressed(VmuButton.Right, keyboard, gamepad),
            ButtonA = !ButtonChecker.IsPressed(VmuButton.A, keyboard, gamepad),
            ButtonB = !ButtonChecker.IsPressed(VmuButton.B, keyboard, gamepad),
            ButtonSleep = !ButtonChecker.IsPressed(VmuButton.Sleep, keyboard, gamepad),
            ButtonMode = !ButtonChecker.IsPressed(VmuButton.Mode, keyboard, gamepad),
        };

        // Holding sleep can be used to toggle insert/eject
        const int SleepToggleInsertEjectFrameCount = 60; // 1 second
        if (!newP3.ButtonSleep)
        {
            if (SleepHeldFrameCount != -1)
            {
                // Sleep button held and frame counter not in post-toggle position
                SleepHeldFrameCount++;
            }
        }
        else
        {
            // Sleep button up. Reset sleep counter.
            SleepHeldFrameCount = 0;
        }

        if (SleepHeldFrameCount >= SleepToggleInsertEjectFrameCount
            || ButtonChecker.IsNewlyPressed(VmuButton.InsertEject, previousKeys, keyboard, previousGamepad, gamepad))
        {
            // Do not toggle insert/eject via sleep until sleep button is released and re-pressed
            SleepHeldFrameCount = -1;

            Vmu.DockOrEject();
        }

        // Let any button press wake the VMU from sleep
        if (Configuration.AnyButtonWakesFromSleep && !Vmu._cpu.SFRs.Vccr.DisplayControl && (byte)newP3 != 0xff)
        {
            newP3 = newP3 with { ButtonSleep = false };
        }

        Vmu._cpu.SFRs.P3 = newP3;

        var rate = Paused ? 0 :
            IsFastForwarding ? gameTime.ElapsedGameTime.Ticks * 2 :
            gameTime.ElapsedGameTime.Ticks;

        Vmu._cpu.Run(rate);
    }

    internal void Draw(SpriteBatch spriteBatch)
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
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _spriteTransformMatrix);

        var vmuIsEjected = !Vmu.IsDocked;
        var screenSize = new Point(x: ScaledWidth, y: ScaledHeight);
        var screenRectangle = vmuIsEjected
            ? new Rectangle(new Point(x: SideMargin, y: TopMargin), screenSize)
            : new Rectangle(location: new Point(x: SideMargin, y: TopMargin + IconSize), screenSize);

        spriteBatch.Draw(
            _vmuScreenTexture,
            destinationRectangle: screenRectangle,
            sourceRectangle: null,
            color: Color.White,
            rotation: 0,
            origin: default,
            effects: vmuIsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
            layerDepth: 0);
        spriteBatch.End();

        // Draw icons
        // If we are at an even multiple of the base screen size, or, if the screen is just huge, use nearest neighbor scaling
        var iconsSamplerState = Math.Floor(_spriteTransformMatrix.Down.Y) == _spriteTransformMatrix.Down.Y || _spriteTransformMatrix.Down.Y < -3
            ? SamplerState.PointClamp
            : SamplerState.LinearWrap;
        spriteBatch.Begin(samplerState: iconsSamplerState, transformMatrix: _spriteTransformMatrix);
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

            spriteBatch.Draw(
                texture,
                destinationRectangle: iconRectangle,
                sourceRectangle: null,
                color: iconColor,
                rotation: 0,
                origin: default,
                effects: vmuIsEjected ? SpriteEffects.None : (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),
                layerDepth: 0);
        }

        spriteBatch.End();

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
            // Center the content horizontally, by shifting it over
            // |--------| actual
            // __|----|__ ideal
            // We are calculating the quantity denoted by '__' in the above sketch
            // Since we're using nearest neighbor scaling, we need to round to nearest integer, to keep it from looking blocky.
            float idealWidthScale = preserveAspectRatio ? minScale : widthScale;
            float idealWidth = idealWidthScale * TotalContentWidth;
            var xPosition = targetRectangle.X + (float)Math.Round((targetRectangle.Width - idealWidth) / 2);

            // Do the same process for the vertical position
            // TODO(spi): consider if vertical centering is desirable here or just limiting the height of the 'targetRectangle' would be better
            float idealHeightScale = preserveAspectRatio ? minScale : heightScale;
            float idealHeight = idealHeightScale * TotalContentHeight;
            var yPosition = targetRectangle.Y + (float)Math.Round((targetRectangle.Height - idealHeight) / 2);

            return Matrix.CreateTranslation(xPosition, yPosition, zPosition: 0);
        }
    }

    private void Audio_BufferReady(Audio.AudioBufferReadyEventArgs args)
    {
        _dynamicSound.SubmitBuffer(args.Buffer, args.Start, args.Length);
    }

    internal void UpdateVolume(int volume)
        => Vmu.Audio.Volume = volume;
}
