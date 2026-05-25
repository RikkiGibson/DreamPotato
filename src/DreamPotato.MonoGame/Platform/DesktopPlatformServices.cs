using NativeFileDialogSharp;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DreamPotato.MonoGame;

public sealed class DesktopPlatformServices : IPlatformServices
{
    public bool CanOpenDataFolder => true;
    public bool UseTouchOverlay => false;

    public void OpenDataFolder(string path)
    {
        new Process
        {
            StartInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
            }
        }.Start();
    }

    public Task<string?> PickOpenVmuOrVmsFileAsync()
    {
        var result = Dialog.FileOpen("vmu,bin,vms", defaultPath: null);
        return Task.FromResult(result.IsOk ? result.Path : null);
    }

    public Task<string?> PickSaveVmuAsFileAsync(string suggestedFileName)
    {
        var result = Dialog.FileSave("vmu,bin", defaultPath: null);
        return Task.FromResult(result.IsOk ? result.Path : null);
    }

    public Task PostSaveVmuAsFileAsync(string localVmuFilePath) => Task.CompletedTask;
}
