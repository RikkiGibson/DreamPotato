#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
native_root="$repo_root/src/native/libsamplerate"
source_root="$native_root/source"
build_root="$native_root/build"
bindings_root="$repo_root/src/LibSampleRateDotNet"

case "$(uname -s)-$(uname -m)" in
    Darwin-arm64) rid=osx-arm64; library=libsamplerate.dylib ;;
    Darwin-x86_64) rid=osx-x64; library=libsamplerate.dylib ;;
    Linux-x86_64) rid=linux-x64; library=libsamplerate.so ;;
    Linux-aarch64) rid=linux-arm64; library=libsamplerate.so ;;
    MINGW*-x86_64|MSYS*-x86_64) rid=win-x64; library=samplerate.dll ;;
    *) echo "Unsupported host: $(uname -s) $(uname -m)" >&2; exit 1 ;;
esac

if [[ ! -f "$source_root/CMakeLists.txt" ]]; then
    echo "libsamplerate source is missing. Run eng/update-libsamplerate.sh first." >&2
    exit 1
fi

cmake -S "$source_root" -B "$build_root/$rid" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 \
    -DBUILD_SHARED_LIBS=ON \
    -DBUILD_TESTING=OFF \
    -DLIBSAMPLERATE_EXAMPLES=OFF
cmake --build "$build_root/$rid" --config Release --parallel

artifact="$(find "$build_root/$rid" -name "$library" -print -quit)"
if [[ -z "$artifact" ]]; then
    echo "CMake completed but did not produce $library." >&2
    exit 1
fi

destination="$bindings_root/runtimes/$rid/native"
mkdir -p "$destination"
cp -L "$artifact" "$destination/$library"
echo "Updated $destination/$library"