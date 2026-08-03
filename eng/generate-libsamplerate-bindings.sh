#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
bindings_root="$repo_root/src/LibSampleRateDotNet"

cd "$bindings_root"
dotnet tool restore
rm -rf "$bindings_root/lib"
mkdir -p "$bindings_root/lib"
dotnet tool run ClangSharpPInvokeGenerator -- @GenerateClang.rsp --output "$bindings_root/lib"
find "$bindings_root/lib" -type f -name '*.cs' -exec perl -pi -e 's/\bnint\b/System.Runtime.InteropServices.CLong/g' {} +