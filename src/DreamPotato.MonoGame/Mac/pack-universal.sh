# Runs a publish and creates an app bundle for Mac.
# See https://docs.monogame.net/articles/tutorials/building_2d_games/25_packaging_game/index.html?tabs=macOS#platform-specific-packaging
scriptroot="$(dirname $0)"
project="$scriptroot/.."
reporoot="$scriptroot/../../.."
artifacts="$scriptroot/../../../artifacts"

echo "Creating app bundle at '$artifacts/publish/DreamPotato.MonoGame/DreamPotato.app'..."
mkdir -p $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/
mkdir -p $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Resources/

echo "Running dotnet publish..."
dotnet publish $project -c Release -r osx-x64 --self-contained
dotnet publish $project -c Release -r osx-arm64 --self-contained

echo "Creating universal binaries..."
lipo -create $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/DreamPotato \
    $artifacts/publish/DreamPotato.MonoGame/release_osx-x64/DreamPotato \
    -output $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/DreamPotato

cp -R $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/*.dylib $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/

# libnfd specifically doesn't create a fat/cross-arch binary. The other dylibs we are using do already.
lipo -create $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/libnfd.dylib \
    $artifacts/publish/DreamPotato.MonoGame/release_osx-x64/libnfd.dylib \
    -output $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/libnfd.dylib

echo "Copying Content and Data folders..."
cp -R $artifacts/bin/DreamPotato.MonoGame/release/Content/ $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Resources/Content
cp -R $artifacts/bin/DreamPotato.MonoGame/release/Data/ $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/MacOS/Data
cp $project/Mac/Info.plist $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Info.plist

# Icons
echo "Writing icons..."
mkdir -p $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset
sips -z 16 16 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_16x16.png
sips -z 32 32 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_16x16@2x.png
sips -z 32 32 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_32x32.png
sips -z 64 64 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_32x32@2x.png
sips -z 128 128 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_128x128.png
sips -z 256 256 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_128x128@2x.png
sips -z 256 256 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_256x256.png
sips -z 512 512 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_256x256@2x.png
sips -z 512 512 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_512x512.png
sips -z 1024 1024 $project/Icon.png --out $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset/icon_512x512@2x.png
iconutil -c icns $artifacts/bin/DreamPotato.MonoGame/release/DreamPotato.iconset --output $artifacts/publish/DreamPotato.MonoGame/DreamPotato.icns
cp $artifacts/publish/DreamPotato.MonoGame/DreamPotato.icns $artifacts/publish/DreamPotato.MonoGame/DreamPotato.app/Contents/Resources/DreamPotato.icns
