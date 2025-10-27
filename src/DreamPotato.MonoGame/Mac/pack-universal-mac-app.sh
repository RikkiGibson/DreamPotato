# Runs a publish and creates an app bundle for Mac.
# See https://docs.monogame.net/articles/tutorials/building_2d_games/25_packaging_game/index.html?tabs=macOS#platform-specific-packaging
scriptroot="$(dirname $0)"

echo "Creating app bundle at '$scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app'..."
mkdir -p $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/
mkdir -p $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Resources/

echo "Running dotnet publish..."
dotnet publish $scriptroot/src/DreamPotato.MonoGame -c Release -r osx-x64 --self-contained
dotnet publish $scriptroot/src/DreamPotato.MonoGame -c Release -r osx-arm64 --self-contained

echo "Creating universal binaries..."
lipo -create $scriptroot/artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/DreamPotato \
    $scriptroot/artifacts/publish/DreamPotato.MonoGame/release_osx-x64/DreamPotato \
    -output $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/DreamPotato

cp -R $scriptroot/artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/*.dylib $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/

# libnfd specifically doesn't create a fat/cross-arch binary. The other dylibs we are using do already.
lipo -create $scriptroot/artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/libnfd.dylib \
    $scriptroot/artifacts/publish/DreamPotato.MonoGame/release_osx-x64/libnfd.dylib \
    -output $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/libnfd.dylib

echo "Copying Content and Data folders..."
cp -R $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/Content/ $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Resources/Content
cp -R $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/Data/ $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/Data
cp $scriptroot/src/DreamPotato.MonoGame/Info.plist $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Info.plist

# Icons
echo "Writing icons..."
mkdir -p $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset
sips -z 16 16 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_16x16.png
sips -z 32 32 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_16x16@2x.png
sips -z 32 32 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_32x32.png
sips -z 64 64 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_32x32@2x.png
sips -z 128 128 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_128x128.png
sips -z 256 256 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_128x128@2x.png
sips -z 256 256 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_256x256.png
sips -z 512 512 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_256x256@2x.png
sips -z 512 512 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_512x512.png
sips -z 1024 1024 $scriptroot/src/DreamPotato.MonoGame/Icon.png --out $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_512x512@2x.png
iconutil -c icns $scriptroot/artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset --output $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.icns
cp $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.icns $scriptroot/artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Resources/DreamPotato.icns
