# Runs a publish and creates an app bundle for Mac.
# See https://docs.monogame.net/articles/tutorials/building_2d_games/25_packaging_game/index.html?tabs=macOS#platform-specific-packaging
scriptroot="$(dirname $0)"
project="$scriptroot/.."
reporoot="$(realpath $scriptroot/../../..)"
artifacts="$reporoot/artifacts"

echo "Running dotnet publish..."
dotnet publish $project -c Release -r osx-arm64 --self-contained

echo "Copying artifacts to app bundle '$artifacts/mac-arm64/DreamPotato.app'..."
mkdir -p $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/
mkdir -p $artifacts/mac-arm64/DreamPotato.app/Contents/Resources/

cp $scriptroot/Info.plist $artifacts/mac-arm64/DreamPotato.app/Contents/
cp $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/*.dylib $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/
cp $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/*.pdb $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/
cp $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/DreamPotato $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/
cp -R $artifacts/publish/DreamPotato.MonoGame/release_osx-arm64/Data/ $artifacts/mac-arm64/DreamPotato.app/Contents/MacOS/Data/
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
ditto -c -k --sequesterRsrc --keepParent $artifacts/mac-arm64/DreamPotato.app $artifacts/mac-arm64/DreamPotato.app.zip
