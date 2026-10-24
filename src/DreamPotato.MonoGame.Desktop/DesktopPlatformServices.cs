
using System.Diagnostics;

using DreamPotato.MonoGame;

using NativeFileDialogSharp;

internal class DesktopPlatformServices : IPlatformServices
{
    public FileDialogResult PickFolderDialog()
    {
        var result = Dialog.FolderPicker(defaultPath: null);
        Debug.Assert(result.IsOk == result.Path is not null);
        return new(result.Path);
    }

    public FileDialogResult OpenVmuDialog()
    {
        var result = Dialog.FileOpen("vmu,bin,vms,dci", defaultPath: null);
        Debug.Assert(result.IsOk == result.Path is not null);
        return new(result.Path);
    }

    public FileDialogResult SaveVmuDialog()
    {
        var result = Dialog.FileSave(filterList: "vmu,bin", defaultPath: null);
        Debug.Assert(result.IsOk == result.Path is not null);
        return new(result.Path);
    }
}