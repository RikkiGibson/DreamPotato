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
polyfill_glibc="$artifacts/tools/polyfill-glibc"

echo "Ensuring prerequisites..."
mkdir -p "$artifacts/tools/"

if [ ! -x "$appimagetool" ]; then
    wget -q https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage -O "$appimagetool"
    chmod +x "$appimagetool"
fi

if [ ! -x "$polyfill_glibc" ]; then
    ninja_bin="ninja"
    if ! command -v ninja &> /dev/null; then
        ninja_bin="$artifacts/tools/ninja"
        if [ ! -x "$ninja_bin" ]; then
            echo "Downloading ninja..."
            wget -q https://github.com/ninja-build/ninja/releases/download/v1.12.1/ninja-linux.zip -O "$artifacts/tools/ninja-linux.zip"
            unzip -o "$artifacts/tools/ninja-linux.zip" -d "$artifacts/tools/"
            chmod +x "$ninja_bin"
            rm "$artifacts/tools/ninja-linux.zip"
        fi
    fi

    git init "$artifacts/tools/polyfill-glibc-src"
    git -C "$artifacts/tools/polyfill-glibc-src" remote add origin https://github.com/corsix/polyfill-glibc
    git -C "$artifacts/tools/polyfill-glibc-src" fetch origin dd59051faaa10ee63c1b96f1b47bf9fcd3770ee2
    git -C "$artifacts/tools/polyfill-glibc-src" checkout dd59051faaa10ee63c1b96f1b47bf9fcd3770ee2
    "$ninja_bin" -C "$artifacts/tools/polyfill-glibc-src" polyfill-glibc
    cp "$artifacts/tools/polyfill-glibc-src/polyfill-glibc" "$polyfill_glibc"
    rm -rf "$artifacts/tools/polyfill-glibc-src"
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

# Some libraries like libcimgui.so have a high glibc requirement.
# This makes it so only quite recent systems are allowed to run it.
# Downpatch such libraries so they will run on older systems.
# https://github.com/ImGuiNET/ImGui.NET/issues/491
echo "Patching binaries for glibc 2.34 compatibility..."
for f in "$appdir/usr/bin/DreamPotato" "$appdir"/usr/bin/*.so; do
    [ -f "$f" ] && "$polyfill_glibc" --target-glibc=2.34 "$f" && echo "  patched $f"
done

echo "Building AppImage..."
$appimagetool "$appdir" "$artifacts/DreamPotato-linux-x64.AppImage"
chmod +x "$artifacts/DreamPotato-linux-x64.AppImage"

echo "Done: $artifacts/DreamPotato-linux-x64.AppImage"
