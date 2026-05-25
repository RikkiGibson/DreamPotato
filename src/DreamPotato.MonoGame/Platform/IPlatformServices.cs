using System.Threading.Tasks;

namespace DreamPotato.MonoGame;

public interface IPlatformServices
{
    Task<string?> PickOpenVmuOrVmsFileAsync();
    Task<string?> PickSaveVmuAsFileAsync(string suggestedFileName);
    Task PostSaveVmuAsFileAsync(string localVmuFilePath);

    bool CanOpenDataFolder { get; }
    bool UseTouchOverlay { get; }
    void OpenDataFolder(string path);
}

public static class PlatformServices
{
    private static IPlatformServices _services = new UnconfiguredPlatformServices();

    public static IPlatformServices Current => _services;

    internal static void SetCurrent(IPlatformServices services)
    {
        _services = services;
    }

    private sealed class UnconfiguredPlatformServices : IPlatformServices
    {
        public bool CanOpenDataFolder => false;
        public bool UseTouchOverlay => false;

        public void OpenDataFolder(string path)
        {
        }

        public Task<string?> PickOpenVmuOrVmsFileAsync() => Task.FromResult<string?>(null);

        public Task<string?> PickSaveVmuAsFileAsync(string suggestedFileName) => Task.FromResult<string?>(null);

        public Task PostSaveVmuAsFileAsync(string localVmuFilePath) => Task.CompletedTask;
    }
}
