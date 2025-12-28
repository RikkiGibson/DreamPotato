using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using DreamPotato.Core;

using Microsoft.Xna.Framework.Input;

namespace DreamPotato.MonoGame;

/// <param name="AutoInitializeDate">
/// If true, skips the date initialization on startup.
/// Specify false to get the real hardware behavior, like when batteries are first inserted in the VMU.
/// </param>
public record Configuration(
    bool AutoInitializeDate = true,
    bool AnyButtonWakesFromSleep = true,
    bool PreserveAspectRatio = true,
    int Volume = Audio.DefaultVolume,
    bool MuteSecondaryVmuAudio = true,
    string? ColorPaletteName = null,
    InputMappings? PrimaryInput = null,
    InputMappings? SecondaryInput = null,
    ViewportSize? ViewportSize = null,
    int CurrentSaveStateSlot = 0,
    VmuConnectionState VmuConnectionState = VmuConnectionState.None,
    ExpansionSlots ExpansionSlots = ExpansionSlots.Slot1,
    DreamcastPort DreamcastPort = DreamcastPort.A)
{
    private const string FileName = "configuration.json";
    private static string FilePath => Path.Combine(Vmu.DataFolder, FileName);

    public string ColorPaletteName { get; init; } = ColorPaletteName ?? ColorPalette.White.Name;
    public InputMappings PrimaryInput { get; init; } = SetupPrimaryInput(PrimaryInput);
    public InputMappings SecondaryInput { get; init; } = SetupSecondaryInput(SecondaryInput);

    private static InputMappings SetupPrimaryInput(InputMappings? primaryInput)
    {
        if (primaryInput is null)
            return DefaultPrimaryInput;

        // config from old emu version
        if (primaryInput.GamePadIndex == InputMappings.GamePadIndex_NotSet)
            primaryInput = primaryInput with { GamePadIndex = 0 };

        return primaryInput;
    }

    private static InputMappings SetupSecondaryInput(InputMappings? secondaryInput)
    {
        if (secondaryInput is null)
            return DefaultSecondaryInput;

        // config from old emu version
        if (secondaryInput.GamePadIndex == InputMappings.GamePadIndex_NotSet)
            secondaryInput = secondaryInput with { GamePadIndex = 1 };

        return secondaryInput;
    }

    public ViewportSize ViewportSize { get; init; } = ViewportSize ?? new ViewportSize(Width: VmuPresenter.TotalContentWidth * 2, Height: VmuPresenter.TotalContentHeight * 2 + Game1.MenuBarHeight);
    public WindowPosition? WindowPosition { get; init; }

    public void Save()
    {
        using var fileStream = File.Open(FilePath, FileMode.Create);
        JsonSerializer.Serialize(fileStream, this, ConfigurationJsonSerializerContext.Default.Configuration);
    }

    public static Configuration Load()
    {
        var path = FilePath;
        if (!File.Exists(path))
            return Default;

        using var fileStream = File.OpenRead(path);
        return JsonSerializer.Deserialize(fileStream, ConfigurationJsonSerializerContext.Default.Configuration) ?? Default;
    }

    public static readonly ImmutableArray<KeyMapping> KeyPreset_WASD = [
        new KeyMapping { SourceKey = Keys.W, TargetButton = VmuButton.Up },
        new KeyMapping { SourceKey = Keys.S, TargetButton = VmuButton.Down },
        new KeyMapping { SourceKey = Keys.A, TargetButton = VmuButton.Left },
        new KeyMapping { SourceKey = Keys.D, TargetButton = VmuButton.Right },
        new KeyMapping { SourceKey = Keys.K, TargetButton = VmuButton.A },
        new KeyMapping { SourceKey = Keys.L, TargetButton = VmuButton.B },
        new KeyMapping { SourceKey = Keys.I, TargetButton = VmuButton.Mode },
        new KeyMapping { SourceKey = Keys.J, TargetButton = VmuButton.Sleep },
    ];

    public static readonly ImmutableArray<KeyMapping> KeyPreset_MappedStateCommands = [
        new KeyMapping { SourceKey = Keys.Insert, TargetButton = VmuButton.InsertEject },
        new KeyMapping { SourceKey = Keys.F5, TargetButton = VmuButton.SaveState },
        new KeyMapping { SourceKey = Keys.F8, TargetButton = VmuButton.LoadState },
        new KeyMapping { SourceKey = Keys.F10, TargetButton = VmuButton.Pause },
        new KeyMapping { SourceKey = Keys.F12, TargetButton = VmuButton.TakeScreenshot },
        new KeyMapping { SourceKey = Keys.Tab, TargetButton = VmuButton.FastForward },
    ];

    public static readonly ImmutableArray<KeyMapping> KeyPreset_UnmappedStateCommands = [
        new KeyMapping { SourceKey = Keys.None, TargetButton = VmuButton.InsertEject },
        new KeyMapping { SourceKey = Keys.None, TargetButton = VmuButton.SaveState },
        new KeyMapping { SourceKey = Keys.None, TargetButton = VmuButton.LoadState },
        new KeyMapping { SourceKey = Keys.None, TargetButton = VmuButton.Pause },
        new KeyMapping { SourceKey = Keys.None, TargetButton = VmuButton.TakeScreenshot },
        new KeyMapping { SourceKey = Keys.None, TargetButton = VmuButton.FastForward },
    ];

    public static readonly ImmutableArray<KeyMapping> KeyPreset_Arrows = [
        new KeyMapping { SourceKey = Keys.Up, TargetButton = VmuButton.Up },
        new KeyMapping { SourceKey = Keys.Down, TargetButton = VmuButton.Down },
        new KeyMapping { SourceKey = Keys.Left, TargetButton = VmuButton.Left },
        new KeyMapping { SourceKey = Keys.Right, TargetButton = VmuButton.Right },
        new KeyMapping { SourceKey = Keys.Z, TargetButton = VmuButton.A },
        new KeyMapping { SourceKey = Keys.X, TargetButton = VmuButton.B },
        new KeyMapping { SourceKey = Keys.C, TargetButton = VmuButton.Sleep },
        new KeyMapping { SourceKey = Keys.V, TargetButton = VmuButton.Mode },
    ];

    public static readonly ImmutableArray<KeyMapping> DefaultPrimaryKeyPreset = [.. KeyPreset_WASD, .. KeyPreset_MappedStateCommands];
    public static readonly ImmutableArray<KeyMapping> DefaultSecondaryKeyPreset = [.. KeyPreset_WASD, .. KeyPreset_UnmappedStateCommands];

    public static readonly ImmutableArray<(string name, string description, ImmutableArray<KeyMapping> mappings)> AllPrimaryKeyPresets = [
        ("WASD", "Uses WASD for D-pad and IJKL for buttons", DefaultPrimaryKeyPreset),
        ("Arrows", "Uses arrows for D-pad and ZXCV for buttons", [.. KeyPreset_Arrows, .. KeyPreset_MappedStateCommands])
    ];

    public static readonly ImmutableArray<(string name, string description, ImmutableArray<KeyMapping> mappings)> AllSecondaryKeyPresets = [
        ("WASD", "Uses WASD for D-pad and IJKL for buttons", DefaultSecondaryKeyPreset),
        ("Arrows", "Uses arrows for D-pad and ZXCV for buttons", [.. KeyPreset_Arrows, .. KeyPreset_UnmappedStateCommands])
    ];

    public static readonly ImmutableArray<ButtonMapping> ButtonPreset_Default = [
        new ButtonMapping { SourceButton = Buttons.DPadUp, TargetButton = VmuButton.Up },
        new ButtonMapping { SourceButton = Buttons.DPadDown, TargetButton = VmuButton.Down },
        new ButtonMapping { SourceButton = Buttons.DPadLeft, TargetButton = VmuButton.Left },
        new ButtonMapping { SourceButton = Buttons.DPadRight, TargetButton = VmuButton.Right },
        new ButtonMapping { SourceButton = Buttons.A, TargetButton = VmuButton.A },
        new ButtonMapping { SourceButton = Buttons.B, TargetButton = VmuButton.B },
        new ButtonMapping { SourceButton = Buttons.Start, TargetButton = VmuButton.Mode },
        new ButtonMapping { SourceButton = Buttons.Back, TargetButton = VmuButton.Sleep },
    ];

    /// <summary>Configuration suitable for controlling both Dreamcast and VMU using a single gamepad.</summary>
    public static readonly ImmutableArray<ButtonMapping> ButtonPreset_Sidecar = [
        new ButtonMapping { SourceButton = Buttons.RightThumbstickUp, TargetButton = VmuButton.Up },
        new ButtonMapping { SourceButton = Buttons.RightThumbstickDown, TargetButton = VmuButton.Down },
        new ButtonMapping { SourceButton = Buttons.RightThumbstickLeft, TargetButton = VmuButton.Left },
        new ButtonMapping { SourceButton = Buttons.RightThumbstickRight, TargetButton = VmuButton.Right },
        new ButtonMapping { SourceButton = Buttons.RightShoulder, TargetButton = VmuButton.A },
        new ButtonMapping { SourceButton = Buttons.LeftShoulder, TargetButton = VmuButton.B },
        new ButtonMapping { SourceButton = Buttons.RightStick, TargetButton = VmuButton.Mode },
        new ButtonMapping { SourceButton = Buttons.Back, TargetButton = VmuButton.Sleep },
    ];

    public static readonly ImmutableArray<(string name, string description, ImmutableArray<ButtonMapping> mappings)> AllButtonPresets = [
        ("Default", "General purpose preset", ButtonPreset_Default),
        ("Sidecar", """
            Allows mapping both Dreamcast and
            VMU buttons to a single gamepad
            """, ButtonPreset_Sidecar),
    ];

    private static readonly InputMappings DefaultPrimaryInput = new InputMappings()
    {
        KeyMappings = DefaultPrimaryKeyPreset,
        ButtonMappings = ButtonPreset_Default,
        GamePadIndex = 0
    };

    private static readonly InputMappings DefaultSecondaryInput = new InputMappings()
    {
        KeyMappings = DefaultSecondaryKeyPreset,
        ButtonMappings = ButtonPreset_Default,
        GamePadIndex = 1
    };

    public static readonly Configuration Default = new Configuration();
}

internal class ButtonChecker
{
    private readonly Dictionary<VmuButton, (List<Keys> Keys, List<Buttons> Buttons)> Mappings;

    public ButtonChecker(InputMappings mappings)
    {
        Mappings = [];
        foreach (var value in Enum.GetValues<VmuButton>())
        {
            Mappings.Add(value, ([], []));
        }

        foreach (var mapping in mappings.KeyMappings)
            Mappings[mapping.TargetButton].Keys.Add(mapping.SourceKey);

        foreach (var mapping in mappings.ButtonMappings)
            Mappings[mapping.TargetButton].Buttons.Add(mapping.SourceButton);
    }

    public bool IsPressed(VmuButton vmuButton, KeyboardState keyboard, GamePadState gamepad)
    {
        var (keys, buttons) = Mappings[vmuButton];

        foreach (var button in buttons)
        {
            if (gamepad.IsButtonDown(button))
                return true;
        }

        foreach (var key in keys)
        {
            if (keyboard.IsKeyDown(key))
                return true;
        }

        return false;
    }

    public bool IsNewlyPressed(VmuButton vmuButton, KeyboardState previousKeyboard, KeyboardState newKeyboard, GamePadState previousGamepad, GamePadState newGamepad)
    {
        var (keys, buttons) = Mappings[vmuButton];

        foreach (var button in buttons)
        {
            if (!previousGamepad.IsButtonDown(button) && newGamepad.IsButtonDown(button))
                return true;
        }

        foreach (var key in keys)
        {
            if (!previousKeyboard.IsKeyDown(key) && newKeyboard.IsKeyDown(key))
                return true;
        }

        return false;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<VmuButton>))]
public enum VmuButton
{
    Up,
    Down,
    Left,
    Right,
    A,
    B,
    Mode,
    Sleep,

    /// <summary>
    /// Insert or eject the VMU from the Dreamcast controller.
    /// </summary>
    InsertEject,

    // Commands
    Pause,
    FastForward,
    LoadState,
    SaveState,
    TakeScreenshot,
}

[JsonConverter(typeof(JsonStringEnumConverter<VmuConnectionState>))]
public enum VmuConnectionState
{
    None,
    PrimaryDocked,
    SecondaryDocked,
    PrimaryAndSecondaryDocked,
    VmuToVmuConnection,
}

/// <summary>Which expansion slots are being used</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ExpansionSlots>))]
public enum ExpansionSlots
{
    Slot1 = 0,
    Slot2 = 1,
    Slot1And2 = 2,
}

public struct KeyMapping
{
    [JsonConverter(typeof(JsonStringEnumConverter<Keys>))]
    public Keys SourceKey { get; set; }
    public VmuButton TargetButton { get; set; }
}

public struct ButtonMapping
{
    [JsonConverter(typeof(JsonStringEnumConverter<Buttons>))]
    public Buttons SourceButton { get; set; }
    public VmuButton TargetButton { get; set; }
}

public record InputMappings
{
    public required ImmutableArray<KeyMapping> KeyMappings { get; init; }
    public required ImmutableArray<ButtonMapping> ButtonMappings { get; init; }
    public int GamePadIndex { get; init; } = GamePadIndex_NotSet;

    public const int GamePadIndex_NotSet = -2;
    public const int GamePadIndex_None = -1;
}

/// <summary>Size of the rendered content (i.e. not including the operating system menu bar.)</summary>
public record ViewportSize(int Width, int Height);
public record WindowPosition(int X, int Y);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Configuration))]
public partial class ConfigurationJsonSerializerContext : JsonSerializerContext;
