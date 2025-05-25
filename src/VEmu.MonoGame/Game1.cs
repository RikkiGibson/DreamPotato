using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using VEmu.Core;

namespace VEmu.MonoGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Color[] _vmuScreenData;

    private readonly Vmu _vmu;
    private readonly Display _display;

    // TODO: eventually, there should be UI to permit a non-constant scale.
    private const int VmuScale = 6;
    private const int ScaledWidth = Display.ScreenWidth * VmuScale;
    private const int ScaledHeight = Display.ScreenHeight * VmuScale;

    private const int TopMargin = VmuScale * 2;
    private const int SideMargin = VmuScale * 3;
    private const int BottomMargin = VmuScale * 12;

    private const int TotalScreenWidth = ScaledWidth + SideMargin * 2;
    private const int TotalScreenHeight = ScaledHeight + TopMargin + BottomMargin;

    // Set in Initialize()
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _vmuScreenTexture = null!;
    private Texture2D _iconsTexture = null!;
    private Configuration _configuration = null!;

    // Set in LoadContent()
    private SpriteFont _font1 = null!;
    private DynamicSoundEffectInstance _dynamicSound = null!;


    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _vmu = new Vmu();

        _display = new Display(_vmu._cpu);
        _vmuScreenData = new Color[Display.ScreenWidth * Display.ScreenHeight];
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = TotalScreenWidth;
        _graphics.PreferredBackBufferHeight = TotalScreenHeight;
        _graphics.ApplyChanges();

        if (Debugger.IsAttached)
        {
            // create window out of the way
            Window.Position = new Point(x: 2200, y: 600);
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _vmuScreenTexture = new Texture2D(_graphics.GraphicsDevice, Display.ScreenWidth, Display.ScreenHeight);

        // TODO: how should icons handle scaling? should there be a minimum scale to keep things from looking bad?
        _iconsTexture = new Texture2D(_graphics.GraphicsDevice, ScaledWidth, BottomMargin);

        _configuration = Configuration.Load();
        _configuration.Save();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // TODO: UI/config for picking a vmu file
        // _vmu.LoadGameVms(@"C:\Users\rikki\src\VMU-MISC-CODE\memopad.vms");
        _vmu.LoadGameVms(@"C:\Users\rikki\src\ghidra-pinta\SkiesOfArcadiaPinataQuest.vms");
        // _vmu.LoadGameVms(@"C:\Users\rikki\src\VMU-MISC-CODE\AUDIO3_TEST.vms");
        // _vmu.LoadGameVms(@"C:\Users\rikki\src\VEmu\src\VEmu.Tests\TestSource\RcOscillator.vms");
        // _vmu.LoadGameVms(@"C:\Users\rikki\src\VEmu\src\VEmu.Tests\TestSource\BaseTimerInt1Counter.vms");

        var bios = File.ReadAllBytes(@"C:\Users\rikki\OneDrive\vmu reverse engineering\dmitry-vmu\vmu\ROMs\american_v1.05.bin");
        bios.AsSpan().CopyTo(_vmu._cpu.ROM);
        _vmu._cpu.SetInstructionBank(Core.SFRs.InstructionBank.FlashBank0);
        _vmu.Audio.AudioBufferReady += Audio_BufferReady;
        // _vmu._cpu.SetInstructionBank(Core.SFRs.InstructionBank.ROM);

        // TODO: it would be good to setup the bios time automatically.
        // Possibly the host system time could be used. Dunno if the DC system time could be used implicitly, without user running the memory card clock update function in system menu.

        _font1 = Content.Load<SpriteFont>("MyMenuFont");
        _dynamicSound = new DynamicSoundEffectInstance(Audio.SampleRate, AudioChannels.Mono);
    }

    private KeyboardState _previousKeys;
    private bool _paused;

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var gamepad = GamePad.GetState(PlayerIndex.One);
        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        if (_previousKeys.IsKeyUp(Keys.F10) && keyboard.IsKeyDown(Keys.F10))
            _paused = !_paused;

        // If any of the mappings has the key pressed, the key is pressed; the key is only up if it is not pressed for all the mappings
        var upPressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.Up && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.Up && gamepad.IsButtonDown(mapping.SourceButton));
        var downPressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.Down && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.Down && gamepad.IsButtonDown(mapping.SourceButton));
        var leftPressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.Left && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.Left && gamepad.IsButtonDown(mapping.SourceButton));
        var rightPressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.Right && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.Right && gamepad.IsButtonDown(mapping.SourceButton));
        var buttonAPressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.A && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.A && gamepad.IsButtonDown(mapping.SourceButton));
        var buttonBPressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.B && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.B && gamepad.IsButtonDown(mapping.SourceButton));
        var buttonModePressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.Mode && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.Mode && gamepad.IsButtonDown(mapping.SourceButton));
        var buttonSleepPressed = _configuration.KeyMappings.Any(mapping => mapping.TargetButton == VmuButton.Sleep && keyboard.IsKeyDown(mapping.SourceKey))
            || _configuration.ButtonMappings.Any(mapping => mapping.TargetButton == VmuButton.Sleep && gamepad.IsButtonDown(mapping.SourceButton));

        _vmu._cpu.SFRs.P3 = new Core.SFRs.P3()
        {
            Up = !upPressed,
            Down = !downPressed,
            Left = !leftPressed,
            Right = !rightPressed,
            ButtonA = !buttonAPressed,
            ButtonB = !buttonBPressed,
            ButtonSleep = !buttonSleepPressed,
            ButtonMode = !buttonModePressed,
        };

        var rate = _paused ? 0 :
            keyboard.IsKeyDown(Keys.Tab) ? gameTime.ElapsedGameTime.Ticks * 2 :
            gameTime.ElapsedGameTime.Ticks;

        _vmu._cpu.Run(rate);
        _previousKeys = keyboard;

        base.Update(gameTime);
    }

    private void Audio_BufferReady(Audio.AudioBufferReadyEventArgs args)
    {
        _dynamicSound.SubmitBuffer(args.Buffer, args.Start, args.Length);
        if (_dynamicSound.State != SoundState.Playing/* && _dynamicSound.PendingBufferCount > 1*/)
            _dynamicSound.Play();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        var screenData = _display.GetBytes();
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

        _spriteBatch.Draw(_vmuScreenTexture, new Rectangle(new Point(x: SideMargin, y: TopMargin), new Point(x: ScaledWidth, y: ScaledHeight)), color: Color.White);

        // Draw icons
        var icons = _display.GetIcons();
        var fileIcon = (icons & Icons.File) != 0 ? "File " : "  ";
        var gameIcon = (icons & Icons.Game) != 0 ? "Game " : " ";
        var clockIcon = (icons & Icons.Clock) != 0 ? "Clock " : " ";
        var flashIcon = (icons & Icons.Flash) != 0 ? "Flash " : "  ";
        var sleeping = _vmu._cpu.SFRs.Vccr.DisplayControl ? " " : "(sleep) ";
        var paused = _paused ? "(paused) " : " ";
        var iconString = $"{fileIcon}{gameIcon}{clockIcon}{flashIcon}{sleeping}{paused}";
        _spriteBatch.DrawString(_font1, iconString, new Vector2(x: SideMargin, y: TopMargin + ScaledHeight), Color.Black);

        _spriteBatch.End();

        base.Draw(gameTime);

        static Color ReadColor(byte b, byte bitAddress)
        {
            return BitHelpers.ReadBit(b, bitAddress) ? Color.Black : Color.White;
        }
    }
}
