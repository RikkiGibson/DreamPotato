# Android Build + Parity Notes

## Build checklist

- Install [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
- Install Android workload:
  - `dotnet workload install android`
- Build debug APK:
  - `dotnet build src\DreamPotato.MonoGame.Android\DreamPotato.MonoGame.Android.csproj -c Debug`
- Verify APK:
  - `artifacts\bin\DreamPotato.MonoGame.Android\debug\com.dreampotato.monogame-Signed.apk`

## Runtime checklist

- `american_v1.05.bin` ROM is required.
  - Exactly 64KB.
  - Choose it from the Android file picker during first app launch.
- `Open Data Folder` and `Set Window Size` menu items are intentionally hidden on Android.
