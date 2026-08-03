#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
native_root="$repo_root/src/native/libsamplerate"
source_root="$native_root/source"
ref="${1:-0.2.2}"
work_root="$(mktemp -d)"
trap 'rm -rf "$work_root"' EXIT

git clone --quiet --filter=blob:none --no-checkout https://github.com/libsndfile/libsamplerate.git "$work_root/repository"
git -C "$work_root/repository" fetch --quiet --depth 1 origin "$ref"
commit="$(git -C "$work_root/repository" rev-parse FETCH_HEAD)"

rm -rf "$source_root"
mkdir -p "$source_root"
git -C "$work_root/repository" archive "$commit" | tar -x -C "$source_root"
printf '%s\n' "$commit" > "$native_root/commitid.txt"

echo "Vendored libsamplerate $ref ($commit)"
"$repo_root/eng/generate-libsamplerate-bindings.sh"
"$repo_root/eng/build-libsamplerate.sh"