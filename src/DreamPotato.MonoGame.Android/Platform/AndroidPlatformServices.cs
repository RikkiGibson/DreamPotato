using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Provider;
using Android.Util;
using Android.Widget;

using DreamPotato.Core;
using DreamPotato.MonoGame.Android;

using AndroidUri = global::Android.Net.Uri;

namespace DreamPotato.MonoGame;

public sealed class AndroidPlatformServices : IPlatformServices
{
    private const int OpenFileRequestCode = 9001;
    private const int SaveAsRequestCode = 9002;
    private const int RomRequestCode = 9003;
    private const string LogTag = "DreamPotato";
    private const string ImportFolderName = "Imported";
    private const string SaveAsFolderName = "SaveAs";

    private readonly MainActivity _activity;
    private readonly object _syncRoot = new();
    private TaskCompletionSource<string?>? _openTaskSource;
    private TaskCompletionSource<string?>? _saveAsTaskSource;
    private TaskCompletionSource<string?>? _romTaskSource;
    private string? _romDestinationPath;
    private long? _romExpectedLength;
    private AndroidUri? _pendingExportUri;

    public AndroidPlatformServices(MainActivity activity)
    {
        _activity = activity;
    }

    public bool CanOpenDataFolder => false;
    public bool UseTouchOverlay => true;

    public void OpenDataFolder(string path)
    {
    }

    public Task<string?> PickOpenVmuOrVmsFileAsync()
    {
        lock (_syncRoot)
        {
            if (_openTaskSource is not null || _saveAsTaskSource is not null || _romTaskSource is not null)
                return Task.FromResult<string?>(null);

            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");

            var taskSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _openTaskSource = taskSource;
            TryStartActivityForResult(intent, OpenFileRequestCode, taskSource, () => _openTaskSource = null, "Unable to open Android file picker.");
            return taskSource.Task;
        }
    }

    public Task<string?> PickRomFileAsync(string destinationFilePath, long expectedLengthBytes)
    {
        lock (_syncRoot)
        {
            if (_openTaskSource is not null || _saveAsTaskSource is not null || _romTaskSource is not null)
                return Task.FromResult<string?>(null);

            if (string.IsNullOrWhiteSpace(destinationFilePath) || expectedLengthBytes <= 0)
                return Task.FromResult<string?>(null);

            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraMimeTypes, new[] { "application/octet-stream" });

            var taskSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _romTaskSource = taskSource;
            _romDestinationPath = destinationFilePath;
            _romExpectedLength = expectedLengthBytes;
            TryStartActivityForResult(intent, RomRequestCode, taskSource, () =>
            {
                _romTaskSource = null;
                _romDestinationPath = null;
                _romExpectedLength = null;
            }, "Unable to open Android BIOS picker.");
            return taskSource.Task;
        }
    }

    public Task<string?> PickSaveVmuAsFileAsync(string suggestedFileName)
    {
        lock (_syncRoot)
        {
            if (_saveAsTaskSource is not null || _openTaskSource is not null || _romTaskSource is not null)
                return Task.FromResult<string?>(null);

            var safeSuggestedFileName = EnsureSafeFileName(Path.GetFileName(suggestedFileName));
            if (safeSuggestedFileName.Length == 0)
                safeSuggestedFileName = "DreamPotato.vmu";

            if (Path.GetExtension(safeSuggestedFileName).Length == 0)
                safeSuggestedFileName = $"{safeSuggestedFileName}.vmu";

            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Intent.ExtraTitle, safeSuggestedFileName);

            var taskSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _saveAsTaskSource = taskSource;
            TryStartActivityForResult(intent, SaveAsRequestCode, taskSource, () => _saveAsTaskSource = null, "Unable to open Android save picker.");
            return taskSource.Task;
        }
    }

    public Task PostSaveVmuAsFileAsync(string localVmuFilePath)
    {
        AndroidUri? destinationUri;
        lock (_syncRoot)
        {
            destinationUri = _pendingExportUri;
            _pendingExportUri = null;
        }

        if (destinationUri is null || !File.Exists(localVmuFilePath))
            return Task.CompletedTask;

        return ExportSavedFileAsync(destinationUri, localVmuFilePath);
    }

    internal bool HandleActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode == OpenFileRequestCode)
            return HandleOpenResult(resultCode, data?.Data);

        if (requestCode == SaveAsRequestCode)
            return HandleSaveAsResult(resultCode, data?.Data);

        if (requestCode == RomRequestCode)
            return HandleRomResult(resultCode, data?.Data);

        return false;
    }

    private bool HandleOpenResult(Result resultCode, AndroidUri? sourceUri)
    {
        TaskCompletionSource<string?>? taskSource;
        lock (_syncRoot)
        {
            taskSource = _openTaskSource;
            _openTaskSource = null;
        }

        if (taskSource is null)
            return false;

        if (resultCode != Result.Ok || sourceUri is null)
        {
            taskSource.TrySetResult(null);
            return true;
        }

        _ = CompleteOpenAsync(taskSource, sourceUri);
        return true;
    }

    private bool HandleRomResult(Result resultCode, AndroidUri? sourceUri)
    {
        TaskCompletionSource<string?>? taskSource;
        string? destinationPath;
        long? expectedLength;
        lock (_syncRoot)
        {
            taskSource = _romTaskSource;
            destinationPath = _romDestinationPath;
            expectedLength = _romExpectedLength;
            _romTaskSource = null;
            _romDestinationPath = null;
            _romExpectedLength = null;
        }

        if (taskSource is null || destinationPath is null)
            return false;

        if (resultCode != Result.Ok || sourceUri is null)
        {
            taskSource.TrySetResult(null);
            return true;
        }

        _ = CompleteRomOpenAsync(taskSource, sourceUri, destinationPath, expectedLength);
        return true;
    }

    private bool HandleSaveAsResult(Result resultCode, AndroidUri? destinationUri)
    {
        TaskCompletionSource<string?>? taskSource;
        lock (_syncRoot)
        {
            taskSource = _saveAsTaskSource;
            _saveAsTaskSource = null;
        }

        if (taskSource is null)
            return false;

        if (resultCode != Result.Ok || destinationUri is null)
        {
            taskSource.TrySetResult(null);
            return true;
        }

        var safeFileName = EnsureSafeFileName(TryGetDisplayName(destinationUri));
        if (safeFileName.Length == 0)
            safeFileName = EnsureSafeFileName(destinationUri.LastPathSegment);
        if (safeFileName.Length == 0)
            safeFileName = "DreamPotato.vmu";

        if (Path.GetExtension(safeFileName).Length == 0)
            safeFileName = $"{safeFileName}.vmu";
        else if (!Path.GetExtension(safeFileName).Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(safeFileName).Equals(".bin", StringComparison.OrdinalIgnoreCase))
            safeFileName = Path.ChangeExtension(safeFileName, ".vmu");

        var folderPath = EnsureFolder(Path.Combine(Vmu.UserDataFolder, SaveAsFolderName));
        var localPath = GetAvailablePath(folderPath, safeFileName);

        lock (_syncRoot)
            _pendingExportUri = destinationUri;

        taskSource.TrySetResult(localPath);
        return true;
    }

    private async Task CompleteOpenAsync(TaskCompletionSource<string?> taskSource, AndroidUri sourceUri)
    {
        string? localFilePath = null;
        try
        {
            localFilePath = await CopyContentUriToLocalPathAsync(sourceUri, ImportFolderName, requireSupportedFileType: true);
        }
        catch (Exception ex)
        {
            ReportPlatformError("Unable to import the selected VMU file.", ex);
        }

        taskSource.TrySetResult(localFilePath);
    }

    private async Task CompleteRomOpenAsync(TaskCompletionSource<string?> taskSource, AndroidUri sourceUri, string destinationPath, long? expectedLength)
    {
        string? localFilePath = null;
        try
        {
            localFilePath = await CopyContentUriToFixedPathAsync(sourceUri, destinationPath, expectedLength);
        }
        catch (Exception ex)
        {
            ReportPlatformError("Unable to import the selected BIOS file.", ex);
        }

        taskSource.TrySetResult(localFilePath);
    }

    private async Task ExportSavedFileAsync(AndroidUri destinationUri, string localVmuFilePath)
    {
        try
        {
            using var inputStream = File.OpenRead(localVmuFilePath);
            var contentResolver = _activity.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is not available.");
            using var outputStream = contentResolver.OpenOutputStream(destinationUri);
            if (outputStream is null)
                return;

            await inputStream.CopyToAsync(outputStream);
        }
        catch (Exception ex)
        {
            ReportPlatformError("Unable to export the saved VMU file.", ex);
        }
    }

    private async Task<string> CopyContentUriToLocalPathAsync(AndroidUri sourceUri, string folderName, bool requireSupportedFileType = false)
    {
        var displayName = TryGetDisplayName(sourceUri) ?? sourceUri.LastPathSegment;
        var safeFileName = EnsureSafeFileName(displayName);
        if (safeFileName.Length == 0)
            safeFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.bin";
        if (requireSupportedFileType && !IsSupportedVmuOrVmsFile(safeFileName))
            throw new InvalidOperationException("Selected file is not a supported VMU/VMS file.");
        if (Path.GetExtension(safeFileName).Length == 0)
            safeFileName = $"{safeFileName}.bin";

        var folderPath = EnsureFolder(Path.Combine(Vmu.UserDataFolder, folderName));
        var localPath = GetAvailablePath(folderPath, safeFileName);

        var contentResolver = _activity.ContentResolver
            ?? throw new InvalidOperationException("Android content resolver is not available.");
        using var inputStream = contentResolver.OpenInputStream(sourceUri);
        if (inputStream is null)
            throw new InvalidOperationException("Could not open source content URI.");

        using var outputStream = File.Create(localPath);
        await inputStream.CopyToAsync(outputStream);
        return localPath;
    }

    private async Task<string> CopyContentUriToFixedPathAsync(AndroidUri sourceUri, string destinationFilePath, long? expectedLength)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
        if (string.IsNullOrWhiteSpace(destinationDirectory))
            throw new InvalidOperationException("Could not determine destination directory.");

        EnsureFolder(destinationDirectory);

        var contentResolver = _activity.ContentResolver
            ?? throw new InvalidOperationException("Android content resolver is not available.");
        using var inputStream = contentResolver.OpenInputStream(sourceUri);
        if (inputStream is null)
            throw new InvalidOperationException("Could not open source content URI.");

        var tempFilePath = Path.Combine(destinationDirectory, $"{Path.GetFileName(destinationFilePath)}.tmp");
        TryDeleteFile(tempFilePath);

        using (var outputStream = File.Create(tempFilePath))
        {
            try
            {
                await inputStream.CopyToAsync(outputStream);
                if (expectedLength.HasValue && outputStream.Length != expectedLength.Value)
                    throw new InvalidOperationException("Selected ROM file has invalid size.");
            }
            catch
            {
                outputStream.Close();
                TryDeleteFile(tempFilePath);
                throw;
            }
        }

        File.Move(tempFilePath, destinationFilePath, overwrite: true);
        return destinationFilePath;
    }

    private string EnsureFolder(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetAvailablePath(string folderPath, string fileName)
    {
        var localFilePath = Path.Combine(folderPath, fileName);
        if (!File.Exists(localFilePath))
            return localFilePath;

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var index = 1; ; index++)
        {
            localFilePath = Path.Combine(folderPath, $"{fileNameWithoutExtension}_{index}{extension}");
            if (!File.Exists(localFilePath))
                return localFilePath;
        }
    }

    private static string EnsureSafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeFileName = new char[fileName.Length];
        for (int i = 0; i < fileName.Length; i++)
        {
            var ch = fileName[i];
            if (Array.IndexOf(invalidChars, ch) == -1 && ch != '/' && ch != '\\')
                safeFileName[i] = ch;
            else
                safeFileName[i] = '_';
        }

        return new string(safeFileName).Trim();
    }

    private static bool IsSupportedVmuOrVmsFile(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Equals(".vms", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".vmu", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bin", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryStartActivityForResult(Intent intent, int requestCode, TaskCompletionSource<string?> taskSource, Action clearPendingState, string errorMessage)
    {
        try
        {
            _activity.StartActivityForResult(intent, requestCode);
            return true;
        }
        catch (Exception ex)
        {
            clearPendingState();
            taskSource.TrySetResult(null);
            ReportPlatformError(errorMessage, ex);
            return false;
        }
    }

    private void ReportPlatformError(string message, Exception ex)
    {
        Log.Warn(LogTag, ex.ToString());
        _activity.RunOnUiThread(() => Toast.MakeText(_activity, message, ToastLength.Long)?.Show());
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private string? TryGetDisplayName(AndroidUri uri)
    {
        try
        {
            var contentResolver = _activity.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is not available.");
            using var cursor = contentResolver.Query(uri, new[] { IOpenableColumns.DisplayName }, null, null, null);
            if (cursor is null || !cursor.MoveToFirst())
                return null;

            var displayNameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
            if (displayNameIndex == -1)
                return null;

            return cursor.GetString(displayNameIndex);
        }
        catch (Exception ex)
        {
            Log.Warn(LogTag, ex.ToString());
            return null;
        }
    }
}
