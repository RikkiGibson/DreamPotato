
using DreamPotato.Core;

namespace DreamPotato.MonoGame;

public record IntegratedModeArgs(int TcpPort, DreamcastPort Port, ExpansionSlots Slots);

/// <param name="IntegratedModeInfo">When non-null, indicates the emulator is running in integrated mode.</param>
public record CommandLineArgs(string? GameFilePath, IntegratedModeArgs? IntegratedModeInfo);
