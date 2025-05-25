using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Xna.Framework.Input;

namespace VEmu.MonoGame;

public record Configuration
{
    public const string FileName = "configuration.json";
    public ImmutableArray<KeyMapping> KeyMappings { get; init; }
    public ImmutableArray<ButtonMapping> ButtonMappings { get; init; }

    public void Save()
    {
        using var fileStream = File.OpenWrite(FileName);
        JsonSerializer.Serialize(fileStream, this, ConfigurationJsonSerializerContext.Default.Configuration);
    }

    public static Configuration Load()
    {
        if (!File.Exists(FileName))
            return Preset_DreamcastSimultaneous;

        using var fileStream = File.OpenRead(FileName);
        return JsonSerializer.Deserialize(fileStream, ConfigurationJsonSerializerContext.Default.Configuration) ?? Preset_DreamcastSimultaneous;
    }

    public static Configuration Default = new Configuration()
    {
        KeyMappings = [
            new KeyMapping { SourceKey = Keys.W, TargetButton = VmuButton.Up },
            new KeyMapping { SourceKey = Keys.S, TargetButton = VmuButton.Down },
            new KeyMapping { SourceKey = Keys.A, TargetButton = VmuButton.Left },
            new KeyMapping { SourceKey = Keys.D, TargetButton = VmuButton.Right },
            new KeyMapping { SourceKey = Keys.K, TargetButton = VmuButton.A },
            new KeyMapping { SourceKey = Keys.L, TargetButton = VmuButton.B },
            new KeyMapping { SourceKey = Keys.J, TargetButton = VmuButton.Mode },
            new KeyMapping { SourceKey = Keys.I, TargetButton = VmuButton.Sleep },
            new KeyMapping { SourceKey = Keys.F10, TargetButton = VmuButton.Sleep },
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
    public static Configuration Preset_DreamcastSimultaneous = new Configuration()
    {
        KeyMappings = [
            new KeyMapping { SourceKey = Keys.W, TargetButton = VmuButton.Up },
            new KeyMapping { SourceKey = Keys.S, TargetButton = VmuButton.Down },
            new KeyMapping { SourceKey = Keys.A, TargetButton = VmuButton.Left },
            new KeyMapping { SourceKey = Keys.D, TargetButton = VmuButton.Right },
            new KeyMapping { SourceKey = Keys.K, TargetButton = VmuButton.A },
            new KeyMapping { SourceKey = Keys.L, TargetButton = VmuButton.B },
            new KeyMapping { SourceKey = Keys.J, TargetButton = VmuButton.Mode },
            new KeyMapping { SourceKey = Keys.I, TargetButton = VmuButton.Sleep },
        ],
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

    // Commands
    Pause,
    FastForward,
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
