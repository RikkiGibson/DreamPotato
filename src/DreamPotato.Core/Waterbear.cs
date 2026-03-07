global using WB = DreamPotato.Core.Waterbear;

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DreamPotato.Core.Waterbear;

/// <summary>
/// https://github.com/wtetzner/waterbear/blob/master/docs/debug.md
/// </summary>
public class DebugInfo
{
    /// <summary>File format version</summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>Original source language (default is "asm")</summary>
    [JsonPropertyName("language")]
    public required string Language { get; init; }

    /// <summary>Path to the VMS binary</summary>
    [JsonPropertyName("binary")]
    public required string Binary { get; init; }

    /// <summary>Name of the software that generated this file</summary>
    [JsonPropertyName("producer")]
    public required string Producer { get; init; }

    /// <summary>The algorithm used to hash sources</summary>
    [JsonPropertyName("hash-algorithm")]
    public required string HashAlgorithm { get; init; }

    [JsonPropertyName("sources")]
    public required ImmutableArray<Source> Sources { get; init; }

    /// <summary>Sorted by <see cref="Label.Span" />, not by <see cref="Label.Offset" />.</summary>
    [JsonPropertyName("labels")]
    public required ImmutableArray<Label> Labels { get; init; }

    [JsonPropertyName("constants")]
    public required ImmutableArray<Constant> Constants { get; init; }

    /// <summary>Sorted by <see cref="Instruction.Span" />, not by <see cref="Instruction.Offset"/>.</summary>
    [JsonPropertyName("instructions")]
    public required ImmutableArray<Instruction> Instructions { get; init; }
}

public class Source
{
    /// <summary>File path of the source file</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>ID of the file, referenced by <see cref="Location"/></summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    /// <summary>Hash of the source file, using <see cref="DebugInfo.HashAlgorithm"/></summary>
    [JsonPropertyName("hash")]
    public required string Hash { get; init; }
}

public class Label
{
    /// <summary>Name of the label</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonIgnore]
    public string DisplayName => field ??= Parent is null ? Name : $"  {Name}";

    /// <summary>Text span of the label in the original source file</summary>
    [JsonPropertyName("span")]
    public required Span Span { get; init; }

    /// <summary>Byte offset into the VMS file</summary>
    [JsonPropertyName("offset")]
    public required ushort Offset { get; init; }

    /// <summary>If this is a local label, it refers to the parent global label</summary>
    [JsonPropertyName("parent")]
    public string? Parent { get; init; }

    public int CompareTo(Label? other)
    {
        return other is null ? 1 : Offset.CompareTo(other.Offset);
    }
}

public class Constant
{
    /// <summary>Name of the constant</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Text span of the constant definition in the original source file</summary>
    [JsonPropertyName("span")]
    public required Span Span { get; init; }

    /// <summary>The value of the constant</summary>
    [JsonPropertyName("value")]
    public required int Value { get; init; }
}

public class Instruction : IComparable<Instruction>
{
    /// <summary>Make an argument to BinarySearch</summary>
    public static Instruction SearchFor(ushort offset) => new Instruction()
    {
        Text = null!,
        Span = null!,
        Offset = offset,
    };

    /// <summary>A textual representation of the instruction from the source file</summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>Text span of the instruction call from the original source file</summary>
    [JsonPropertyName("span")]
    public required Span Span { get; init; }

    /// <summary>The byte offset of the instruction in the VMS file</summary>
    [JsonPropertyName("offset")]
    public required ushort Offset { get; init; }

    public int CompareTo(Instruction? other)
    {
        return other is null ? 1 : Offset.CompareTo(other.Offset);
    }
}

public class Span
{
    [JsonPropertyName("start")]
    public required Location Start { get; init; }

    [JsonPropertyName("end")]
    public required Location End { get; init; }
}

public class Location
{
    /// <summary>The ID of the source file, as listed in <see cref="DebugInfo.Sources"/></summary>
    [JsonPropertyName("source")]
    public required int Source { get; init; }

    /// <summary>The byte offset into the original source file. Makes slicing a chunk of text out of a source file easy.</summary>
    [JsonPropertyName("byte-offset")]
    public required int ByteOffset { get; init; }

    /// <summary>Line number (starts at 1)</summary>
    [JsonPropertyName("line")]
    public required int LineOneBased { get; init; }

    /// <summary>Column number, computed in terms of Unicode grapheme clusters (starts at 0)</summary>
    [JsonPropertyName("column")]
    public required int ColumnZeroBased { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DebugInfo))]
public partial class WaterbearJsonSerializerContext : JsonSerializerContext;
