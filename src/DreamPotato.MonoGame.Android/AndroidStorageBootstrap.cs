using System;
using System.IO;

using Android.Content;

using DreamPotato.Core;

namespace DreamPotato.MonoGame.Android;

internal static class AndroidStorageBootstrap
{
    internal static void Initialize(Context context)
    {
        var filesDir = context.FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException("Android files directory is not available.");

        var dataDir = Path.Combine(filesDir, "Data");
        Directory.CreateDirectory(dataDir);
        CopyAssetDirectory(context, assetPath: "Data", destinationPath: dataDir);

        Vmu.ConfigurePlatformPaths(
            embeddedDataFolder: dataDir,
            userDataRootFolder: filesDir);
    }

    private static void CopyAssetDirectory(Context context, string assetPath, string destinationPath)
    {
        var assetManager = context.Assets
            ?? throw new InvalidOperationException("Android assets are not available.");

        foreach (var name in assetManager.List(assetPath) ?? [])
        {
            var childAssetPath = $"{assetPath}/{name}";
            var childDestinationPath = Path.Combine(destinationPath, name);
            var children = assetManager.List(childAssetPath);
            if (children is { Length: > 0 })
            {
                Directory.CreateDirectory(childDestinationPath);
                CopyAssetDirectory(context, childAssetPath, childDestinationPath);
                continue;
            }

            using var input = assetManager.Open(childAssetPath);
            using var output = File.Create(childDestinationPath);
            input.CopyTo(output);
        }
    }
}
