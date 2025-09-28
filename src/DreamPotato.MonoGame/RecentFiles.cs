
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DreamPotato.Core;

namespace DreamPotato.MonoGame;

public record RecentFilesInfo
{
    /// <summary>Recent files with the most recently used first. null denotes a "new VMU" that is not saved to disk.</summary>
    public ImmutableArray<string?> RecentFiles { get; init; }

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
        return value ?? Default;
    }

    public RecentFilesInfo PrependRecentFile(string? newRecentFile)
        => this with { RecentFiles = [newRecentFile, .. RecentFiles.Where(file => file != null && file != newRecentFile).Take(5)] };

    public void Save()
    {
        using var fileStream = File.Open(FilePath, FileMode.Create);
        JsonSerializer.Serialize(fileStream, this, RecentFilesInfoJsonSerializerContext.Default.RecentFilesInfo);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(RecentFilesInfo))]
public partial class RecentFilesInfoJsonSerializerContext : JsonSerializerContext;
