
using System.Collections.Immutable;
using System.Text.Json;

using DreamPotato.Core.Waterbear;

namespace DreamPotato.Tests;

public class WaterbearTests
{
    [Fact]
    public void DebugJson_01()
    {
        // Verify that we can read the symbol json
        using var fileStream = File.OpenRead("TestSource/helloworld.vms.debug.json");
        var result = JsonSerializer.Deserialize(fileStream, WaterbearJsonSerializerContext.Default.DebugInfo);
        Assert.NotNull(result);
        Assert.Equal("asm", result.Language);
    }
}