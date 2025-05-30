# DreamPotato

DreamPotato is a Dreamcast VMU emulator with the following goals:
- High-accuracy emulation of commercial and homebrew games.
- Integration with Dreamcast emulator(s) in order to simulate the overall original hardware experience.
- Eventually: provide developer tools to assist with homebrew development, debugging, and reverse engineering.

The emulator is in a very early state currently. None of the above goals are particularly met at this time. Right now it only works on Windows, but Mac and Linux support is planned.

> [!WARNING]
> When opening a `.vmu` or `.bin` file, the emulator will modify the file directly while running in order to persist changes, such as file changes on the memory card or saving your progress in minigames.
> Since the emulator is in alpha state, please make copies of any VMU files before loading them to avoid corrupting your saves.
> `.vms` files opened in the emulator will not be modified.

See [compatibility.md](compatibility.md) for the current compatibility status of various games.

## Usage

Install the latest bits from the Releases section.

### Default Key Mappings
- W - Up
- A - Left
- S - Down
- D - Right
- K - A
- L - B
- I - Mode
- J - Sleep
- Insert - Insert/Eject VMU (experimental--simulates connecting to Dreamcast)
- F10 - Pause/Resume
- Tab (hold) - Fast forward

### Configuration

Button mapping is done by editing a json file by hand for now. Eventually a proper button mapping UI would be desirable of course.

`"SourceKey"`/`"SourceButton"` means the key/button on your keyboard/gamepad you want to use. `TargetButton` means the emulated VMU button or emulator command you want to perform. See [Keys](https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.Keys.html#fields) and [Buttons](https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.Buttons.html) for source key/button names. See [VmuButton](src/VEmu.MonoGame/Configuration.cs) for target button names.

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
