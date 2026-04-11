#!/bin/bash
set -e

# Runs a publish and creates a Linux AppImage for x64.
# Requires appimagetool to be available on PATH.
# See https://docs.appimage.org/packaging-guide/manual.html
scriptroot="$(dirname "$0")"
project="$scriptroot/.."
reporoot="$(realpath "$scriptroot/../../..")"
artifacts="$reporoot/artifacts"
publishdir="$artifacts/publish/DreamPotato.MonoGame/release_linux-x64"
appdir="$artifacts/linux-x64/DreamPotato.AppDir"
appimagetool="$artifacts/tools/appimagetool.AppImage"

if ! command -v $appimagetool &> /dev/null; then
    mkdir -p $artifacts/tools/
    curl -s https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage -o $appimagetool
    chmod +x $appimagetool
fi

echo "Running dotnet publish..."
dotnet publish "$project" -c Release -r linux-x64 --self-contained -p:DebugSymbols=False -p:LinuxAppImage=True

echo "Assembling AppDir at '$appdir'..."
rm -rf "$appdir"
mkdir -p "$appdir/usr/bin"
mkdir -p "$appdir/usr/share/icons/hicolor/32x32/apps"

# Copy published output
cp "$publishdir/DreamPotato" "$appdir/usr/bin/"
cp "$publishdir"/*.so "$appdir/usr/bin/" 2>/dev/null || true
cp -R "$publishdir/Data" "$appdir/usr/bin/Data"
cp -R "$publishdir/Content" "$appdir/usr/bin/Content"

# Desktop entry, icon, and AppRun
cp "$scriptroot/DreamPotato.desktop" "$appdir/"
cp "$project/Icon.png" "$appdir/DreamPotato.png"
cp "$project/Icon.png" "$appdir/usr/share/icons/hicolor/32x32/apps/DreamPotato.png"
cp "$scriptroot/AppRun" "$appdir/"

echo "Building AppImage..."
$appimagetool "$appdir" "$artifacts/DreamPotato-linux-x64.AppImage"
chmod +x "$artifacts/DreamPotato-linux-x64.AppImage"

echo "Done: $artifacts/DreamPotato-linux-x64.AppImage"
