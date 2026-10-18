
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace DreamPotato.MonoGame;

public interface IPlatformServices
{
    FileDialogResult OpenVmuDialog();
    FileDialogResult PickFolderDialog();
    FileDialogResult SaveVmuDialog();
}

public readonly record struct FileDialogResult(string? Path)
{
    [MemberNotNullWhen(true, nameof(Path))]
    public bool IsOk => Path is not null;
}
