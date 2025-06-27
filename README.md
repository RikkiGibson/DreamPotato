# DreamPotato

DreamPotato is a Dreamcast VMU emulator in currently in alpha. For the moment only Windows builds are available.

> [!WARNING]
> When opening a `.vmu` or `.bin` file, the emulator will modify the file on disk while running in order to persist changes, such as save file changes on the memory card or saving your progress in minigames.
> Since the emulator is in alpha state, please make copies of any VMU files before loading them to avoid any chance of corrupting your saves.
> `.vms` files opened in the emulator will not be modified.

See [compatibility.md](compatibility.md) for the current compatibility status of various games.

## Usage

Download the latest bits from the [Releases](https://github.com/RikkiGibson/DreamPotato/releases) section.

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
- F5 - Save State
- F8 - Load State
- F10 - Pause/Resume
- Tab (hold) - Fast Forward

### Configuration

Button mapping is done by editing a json file by hand for now. Eventually a proper button mapping UI would be desirable of course.

`"SourceKey"`/`"SourceButton"` means the key/button on your keyboard/gamepad you want to use. `TargetButton` means the emulated VMU button or emulator command you want to perform. See [Keys](https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.Keys.html#fields) and [Buttons](https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.Buttons.html) for source key/button names. See [enum VmuButton](src/DreamPotato.MonoGame/Configuration.cs) for target button names.

## Building

- You'll need the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed on your computer.
- Copy your `american_v1.05.bin` VMU ROM into `src/DreamPotato.MonoGame/ROM/`.
- Build everything: `dotnet build`.
- Run the emulator: `dotnet run --project src/DreamPotato.MonoGame`.
- Run tests: `dotnet test`

## Project Overview

Under `src/`:
- `DreamPotato.Core` is where the emulator implementation resides.
- `DreamPotato.Tests` includes unit tests for the emulator implementation.
- `DreamPotato.MonoGame` is the front-end, including UI and config file handling.
- `dreampotato-vscode` is the VS Code extension--in *very* barebones/"alpha" state.
    - Currently, it contains only a TextMate grammar for LC86k assembly.

## Why write a new emulator?

The biggest reason was: I was interested in timing-based luck manipulation in Pinta's Quest, which requires emulating the game at a very similar speed to real VMU hardware. I found that existing emulators did not run the game at a similar enough speed to real hardware to allow the same timings to work. I thought that writing a new emulator from scratch would be a good way to learn the hardware well enough to get to the bottom of how to do that.

## Acknowledgements

Thanks to the following individuals, whose invaluable work on VMU emulation and reverse engineering helped make DreamPotato possible.

- Falco Girgis, author of [ElysianVMU](http://evmu.elysianshadows.com/)/[libevmu](https://github.com/gyrovorbis/libevmu) and extensive [documentation](https://vmu.elysianshadows.com/index.html) of VMU internals.
- Dmitry Grinberg for his [VMU hackery](https://dmitry.gr/index.php?r=05.Projects&proj=25.%20VMU%20Hacking) project.
- Walter Tetzner, author of the [waterbear](https://github.com/wtetzner/waterbear) assembler/disassembler.
- Homebrew developers jvsTSX and Jahan Addison for publishing [homebrew](https://github.com/jvsTSX/VMU-MISC-CODE) [software](https://github.com/jahan-addison/snake) which was very helpful for testing the emulator.
