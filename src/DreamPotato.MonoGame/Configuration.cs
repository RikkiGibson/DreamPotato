using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using DreamPotato.Core;

using Microsoft.Xna.Framework.Input;

namespace DreamPotato.MonoGame;

public record Configuration
{
    private const string FileName = "configuration.json";
    private static string FilePath => Path.Combine(Vmu.DataFolder, FileName);

    public ImmutableArray<KeyMapping> KeyMappings { get; init; }
    public ImmutableArray<ButtonMapping> ButtonMappings { get; init; }

    public void Save()
    {
        using var fileStream = File.OpenWrite(FilePath);
        JsonSerializer.Serialize(fileStream, this, ConfigurationJsonSerializerContext.Default.Configuration);
    }

    public static Configuration Load()
    {
        var path = FilePath;
        if (!File.Exists(path))
            return Preset_DreamcastSimultaneous;

        using var fileStream = File.OpenRead(path);
        return JsonSerializer.Deserialize(fileStream, ConfigurationJsonSerializerContext.Default.Configuration) ?? Preset_DreamcastSimultaneous;
    }

    // NOTE: you may need to delete configuration.json from the build folder for changes to defaults to take effect.
    public static Configuration Default = new Configuration()
    {
        KeyMappings = [
            new KeyMapping { SourceKey = Keys.W, TargetButton = VmuButton.Up },
            new KeyMapping { SourceKey = Keys.S, TargetButton = VmuButton.Down },
            new KeyMapping { SourceKey = Keys.A, TargetButton = VmuButton.Left },
            new KeyMapping { SourceKey = Keys.D, TargetButton = VmuButton.Right },
            new KeyMapping { SourceKey = Keys.K, TargetButton = VmuButton.A },
            new KeyMapping { SourceKey = Keys.L, TargetButton = VmuButton.B },
            new KeyMapping { SourceKey = Keys.I, TargetButton = VmuButton.Mode },
            new KeyMapping { SourceKey = Keys.J, TargetButton = VmuButton.Sleep },

            new KeyMapping { SourceKey = Keys.Insert, TargetButton = VmuButton.InsertEject },

            new KeyMapping { SourceKey = Keys.F5, TargetButton = VmuButton.SaveState },
            new KeyMapping { SourceKey = Keys.F8, TargetButton = VmuButton.LoadState },
            new KeyMapping { SourceKey = Keys.F10, TargetButton = VmuButton.Pause },
            new KeyMapping { SourceKey = Keys.Tab, TargetButton = VmuButton.FastForward },
        ],
        ButtonMappings = [
            new ButtonMapping { SourceButton = Buttons.DPadUp, TargetButton = VmuButton.Up },
            new ButtonMapping { SourceButton = Buttons.DPadDown, TargetButton = VmuButton.Down },
            new ButtonMapping { SourceButton = Buttons.DPadLeft, TargetButton = VmuButton.Left },
            new ButtonMapping { SourceButton = Buttons.DPadRight, TargetButton = VmuButton.Right },
            new ButtonMapping { SourceButton = Buttons.A, TargetButton = VmuButton.A },
            new ButtonMapping { SourceButton = Buttons.B, TargetButton = VmuButton.B },
            new ButtonMapping { SourceButton = Buttons.Start, TargetButton = VmuButton.Mode },
            new ButtonMapping { SourceButton = Buttons.BigButton, TargetButton = VmuButton.Sleep },
        ],
    };

    /// <summary>Configuration suitable for controlling both Dreamcast and VMU using a single gamepad.</summary>
    public static Configuration Preset_DreamcastSimultaneous = Default with
    {
        ButtonMappings = [
            new ButtonMapping { SourceButton = Buttons.RightThumbstickUp, TargetButton = VmuButton.Up },
            new ButtonMapping { SourceButton = Buttons.RightThumbstickDown, TargetButton = VmuButton.Down },
            new ButtonMapping { SourceButton = Buttons.RightThumbstickLeft, TargetButton = VmuButton.Left },
            new ButtonMapping { SourceButton = Buttons.RightThumbstickRight, TargetButton = VmuButton.Right },
            new ButtonMapping { SourceButton = Buttons.RightShoulder, TargetButton = VmuButton.A },
            new ButtonMapping { SourceButton = Buttons.LeftShoulder, TargetButton = VmuButton.B },
            new ButtonMapping { SourceButton = Buttons.RightStick, TargetButton = VmuButton.Mode },
            new ButtonMapping { SourceButton = Buttons.Back, TargetButton = VmuButton.Sleep },
        ],
    };
}

internal class ButtonChecker
{
    // TODO: cleraly a dictionary is needed.
    private readonly Dictionary<VmuButton, (List<Keys> Keys, List<Buttons> Buttons)> Mappings;

    public ButtonChecker(Configuration configuration)
    {
        Mappings = [];
        foreach (var value in Enum.GetValues<VmuButton>())
        {
            Mappings.Add(value, ([], []));
        }

        foreach (var mapping in configuration.KeyMappings)
            Mappings[mapping.TargetButton].Keys.Add(mapping.SourceKey);

        foreach (var mapping in configuration.ButtonMappings)
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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Configuration))]
public partial class ConfigurationJsonSerializerContext : JsonSerializerContext;
