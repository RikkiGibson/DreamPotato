using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

using System;
using System.IO;
using System.Threading.Tasks;

using DreamPotato.MonoGame;
using DreamPotato.Core;

using Microsoft.Xna.Framework;

namespace DreamPotato.MonoGame.Android;

[Activity(
    Label = "DreamPotato",
    MainLauncher = true,
    Theme = "@android:style/Theme.Black.NoTitleBar",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleTask,
    ScreenOrientation = ScreenOrientation.FullSensor,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AndroidGameActivity
{
    private Game1? _game;
    private View? _gameView;
    private AndroidPlatformServices? _platformServices;
    private bool _isStarting;
    private const long RomLengthInBytes = 64 * 1024;
    private const string LogTag = "DreamPotato";

    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);

        _platformServices = new AndroidPlatformServices(this);
        PlatformServices.SetCurrent(_platformServices);
        AndroidStorageBootstrap.Initialize(this);

        _ = StartGameWhenRomReadyAsync();
    }

    private async Task StartGameWhenRomReadyAsync()
    {
        if (_isStarting)
            return;

        _isStarting = true;
        try
        {
            var romReady = await EnsureRomFileAvailableAsync();
            if (!romReady)
            {
                Toast.MakeText(this, "Unable to start DreamPotato because BIOS is required.", ToastLength.Long)?.Show();
                Finish();
                return;
            }

            if (IsFinishing || IsDestroyed)
                return;

            _game = new Game1(gameFilePath: null);
            _gameView = _game.Services.GetService(typeof(View)) as View;
            if (_gameView is null)
                throw new InvalidOperationException("Unable to initialize Android game view.");

            if (IsFinishing || IsDestroyed)
            {
                _game.Dispose();
                _game = null;
                _gameView = null;
                return;
            }

            SetContentView(_gameView);
            _game.Run();
        }
        catch (Exception ex)
        {
            Log.Error(LogTag, ex.ToString());
            Toast.MakeText(this, "Unable to start DreamPotato.", ToastLength.Long)?.Show();
            Finish();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _game?.Dispose();
        _game = null;
        _gameView = null;
        _platformServices = null;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (_platformServices?.HandleActivityResult(requestCode, resultCode, data) == true)
        {
            return;
        }

        base.OnActivityResult(requestCode, resultCode, data);
    }

    private async Task<bool> EnsureRomFileAvailableAsync()
    {
        if (_platformServices is null)
            return false;

        var romFilePath = Path.Combine(Vmu.EmbeddedDataFolder, Vmu.RomFileName);
        if (IsRomFileValid(romFilePath))
            return true;

        var copiedRomFilePath = await _platformServices.PickRomFileAsync(romFilePath, RomLengthInBytes);
        if (copiedRomFilePath is null)
            return false;

        return IsRomFileValid(copiedRomFilePath);
    }

    private static bool IsRomFileValid(string romFilePath)
    {
        try
        {
            return File.Exists(romFilePath) && new FileInfo(romFilePath).Length == RomLengthInBytes;
        }
        catch
        {
            return false;
        }
    }
}
