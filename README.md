# DreamPotato

DreamPotato is a Dreamcast VMU emulator with the following goals:
- High-accuracy emulation of commercial and homebrew games.
- Integration with Dreamcast emulator(s) in order to simulate the overall original hardware experience.
- Eventually: provide developer tools to assist with homebrew development, debugging, and reverse engineering.

The emulator is in a very early state currently. None of the above goals are particularly met at this time. Right now it only works on Windows, but Mac and Linux support is planned.

See [compatibility.md](compatibility.md) for the current compatibility status of various games.

## Building

- You'll need the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed on your computer.
- Copy your `american_v1.05.bin` VMU ROM into `src/VEmu.MonoGame/ROM/`.
- Build everything: `dotnet build`.
- Run the emulator: `dotnet run --project src/VEmu.MonoGame`.
- Run tests: `dotnet test`

## Project Overview

Under `src/`:
- `VEmu.Core` is where the emulator implementation properly resides.
- `VEmu.Tests` includes unit tests for the above.
- `VEmu.MonoGame` is the front-end.
- `dreampotato-vscode` is the VS Code extension--in *very* barebones/"alpha" state.
    - Currently, it contains only a TextMate grammar for LC86k assembly.

## Acknowledgements

Thanks to the following individuals, whose invaluable work on the VMU and related software helped make DreamPotato possible.

- Falco Girgis, author of [ElysianVMU](https://github.com/gyrovorbis/libevmu) and extensive [documentation](https://vmu.elysianshadows.com/index.html) of VMU internals.
- Dmitry Grinberg for his [VMU hackery](https://dmitry.gr/index.php?r=05.Projects&proj=25.%20VMU%20Hacking) project.
- Walter Tetzner, author of the [waterbear](https://github.com/wtetzner/waterbear) assembler/disassembler.
- Homebrew developers jvsTSX and Jahan Addison for publishing [homebrew](https://github.com/jvsTSX/VMU-MISC-CODE) [software](https://github.com/jahan-addison/snake) which was very helpful for testing the emulator.
