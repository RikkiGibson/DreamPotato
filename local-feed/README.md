# Local feed

This directory contains dependencies with private fixes applied.

## MonoGame.Framework.DesktopGL

https://github.com/RikkiGibson/MonoGame/commit/3bf1399dc3c32f767f5935f1947d577eb37c3675

Do not embed MonoGame.bmp so that we can allow the icons indicated by the per-OS icon bundles (`.ico`, `.icns`, ...) to be used automatically instead.

See also https://github.com/MonoGame/MonoGame/issues/9601
