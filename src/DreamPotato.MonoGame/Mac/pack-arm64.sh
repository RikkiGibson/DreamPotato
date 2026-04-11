#!/bin/bash
set -euo pipefail

# Runs a publish and creates an app bundle for Mac.
# See https://docs.monogame.net/articles/tutorials/building_2d_games/25_packaging_game/index.html?tabs=macOS#platform-specific-packaging
scriptroot="$(dirname $0)"
project="$scriptroot/.."
reporoot="$(realpath $scriptroot/../../..)"
artifacts="$reporoot/artifacts"

echo "Running dotnet publish..."
dotnet publish $project -c Release -r osx-arm64 --self-contained -p:DebugSymbols=False

echo "Copying artifacts to app bundle '$artifacts/mac-arm64/DreamPotato.app'..."
mkdir -p $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/
mkdir -p $artifacts/mac-arm64/DreamPotato.app/Contents/Resources/

cp $scriptroot/Info.plist $artifacts/mac-arm64/DreamPotato.app/Contents/
cp $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/*.dylib $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/
cp $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/DreamPotato $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/
cp -R $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/Data/ $artifacts/mac-arm64/DreamPotato.app/Contents/Resources/Data/
cp -R $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/Content/ $artifacts/mac-arm64/DreamPotato.app/Contents/Resources/Content/

# Icons
echo "Writing icons..."
mkdir -p $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset
sips -z 16 16 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_16x16.png 1>/dev/null
sips -z 32 32 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_16x16@2x.png  1>/dev/null
sips -z 32 32 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_32x32.png  1>/dev/null
sips -z 64 64 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_32x32@2x.png  1>/dev/null
sips -z 128 128 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_128x128.png  1>/dev/null
sips -z 256 256 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_128x128@2x.png  1>/dev/null
sips -z 256 256 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_256x256.png  1>/dev/null
sips -z 512 512 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_256x256@2x.png  1>/dev/null
sips -z 512 512 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_512x512.png  1>/dev/null
sips -z 1024 1024 $project/Icon.png --out $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset/icon_512x512@2x.png 1>/dev/null
iconutil -c icns $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.iconset --output $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.icns
cp $artifacts/obj/DreamPotato.MonoGame/release_osx-arm64/DreamPotato.icns $artifacts/mac-arm64/DreamPotato.app/Contents/Resources/DreamPotato.icns

# Code signing
if [ -n "$CODESIGN_IDENTITY" ]; then
    echo "Signing app bundle..."
    entitlements="$scriptroot/DreamPotato.entitlements"
    app="$artifacts/mac-arm64/DreamPotato.app"

    # Sign all dylibs first, then the main executable, then the bundle
    find "$app/Contents/MacOS" -name "*.dylib" -exec \
        codesign --force --options runtime --entitlements "$entitlements" --sign "$CODESIGN_IDENTITY" --timestamp {} \;
    codesign --force --options runtime --entitlements "$entitlements" --sign "$CODESIGN_IDENTITY" --timestamp "$app/Contents/MacOS/DreamPotato"
    codesign --force --options runtime --entitlements "$entitlements" --sign "$CODESIGN_IDENTITY" --timestamp "$app"

    echo "Verifying signature..."
    codesign --verify --deep --strict "$app"
else
    cp $scriptroot/README.txt $artifacts/mac-arm64/
fi

ditto -c -k --sequesterRsrc --keepParent $artifacts/mac-arm64/ $artifacts/DreamPotato-mac-arm64.zip

# Notarization
if [ -n "$APPLE_ID" ] && [ -n "$APPLE_ID_PASSWORD" ] && [ -n "$APPLE_TEAM_ID" ]; then
    echo "Submitting for notarization..."
    xcrun notarytool submit "$artifacts/DreamPotato-mac-arm64.zip" \
        --apple-id "$APPLE_ID" \
        --password "$APPLE_ID_PASSWORD" \
        --team-id "$APPLE_TEAM_ID" \
        --wait 2>&1 | tee /tmp/notarytool-output.txt

    # Fetch the notarization log for diagnostics
    SUBMISSION_ID=$(grep 'id:' /tmp/notarytool-output.txt | head -1 | awk '{print $2}')
    if [ -n "$SUBMISSION_ID" ]; then
        echo "Fetching notarization log for submission $SUBMISSION_ID..."
        xcrun notarytool log "$SUBMISSION_ID" \
            --apple-id "$APPLE_ID" \
            --password "$APPLE_ID_PASSWORD" \
            --team-id "$APPLE_TEAM_ID" || true
    fi

    echo "Stapling notarization ticket..."
    xcrun stapler staple "$app"

    # Re-zip after stapling
    rm "$artifacts/DreamPotato-mac-arm64.zip"
    ditto -c -k --sequesterRsrc --keepParent $artifacts/mac-arm64/ $artifacts/DreamPotato-mac-arm64.zip
fi
