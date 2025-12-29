
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DreamPotato.Core;

namespace DreamPotato.MonoGame;

public record RecentFilesInfo
{
    /// <summary>Recent files with the most recently used first.</summary>
    public ImmutableArray<string> RecentFiles { get; init; }

    /// <summary>In presence of non-empty RecentFiles, denotes that "New VMU" was used, and a VMU file shouldn't be automatically loaded.</summary>
    public string? PrimaryVmuMostRecent { get; init; }

    /// <inheritdoc cref="PrimaryVmuMostRecent"/>
    public string? SecondaryVmuMostRecent { get; init; }

    public const int MaxRecentCount = 8;
    const string FileName = "recent.json";
    private static string FilePath => Path.Combine(Vmu.DataFolder, FileName);

    public static readonly RecentFilesInfo Default = new() { RecentFiles = [] };
    public static RecentFilesInfo Load()
    {
        var path = FilePath;
        if (!File.Exists(path))
            return Default;

        using var fileStream = File.OpenRead(path);
        var value = JsonSerializer.Deserialize(fileStream, RecentFilesInfoJsonSerializerContext.Default.RecentFilesInfo);
        // We used to intentionally place null values in 'RecentFiles' in recent.json. Now we don't, and don't want to consume such values.
        if (value != null && value.RecentFiles.Any(recentFile => recentFile == null))
            value = value with { RecentFiles = [.. value.RecentFiles.Where(file => file != null)] };

        return value ?? Default;
    }

    public RecentFilesInfo AddRecentFile(bool forPrimary, string? newRecentFile)
        => this with
        {
            PrimaryVmuMostRecent = forPrimary ? newRecentFile : PrimaryVmuMostRecent,
            SecondaryVmuMostRecent = forPrimary ? SecondaryVmuMostRecent : newRecentFile,
            RecentFiles = newRecentFile is null ? RecentFiles : PrependRecentFile(newRecentFile)
        };

    private ImmutableArray<string> PrependRecentFile(string newRecentFile)
        => [newRecentFile, .. RecentFiles.Where(file => file != null && file != newRecentFile).Take(MaxRecentCount - 1)];

    public void Save()
    {
        using var fileStream = File.Open(FilePath, FileMode.Create);
        JsonSerializer.Serialize(fileStream, this, RecentFilesInfoJsonSerializerContext.Default.RecentFilesInfo);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(RecentFilesInfo))]
public partial class RecentFilesInfoJsonSerializerContext : JsonSerializerContext;
