using DreamPotato.Core;

namespace DreamPotato.Tests;

public class PlatformPathTests
{
    [Fact]
    public void ConfigurePlatformPathsUsesAppPrivateDataRoot()
    {
        var embeddedDataFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Data");
        var userDataRootFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            Vmu.ConfigurePlatformPaths(embeddedDataFolder, userDataRootFolder);

            Assert.Equal(embeddedDataFolder, Vmu.EmbeddedDataFolder);
            Assert.Equal(userDataRootFolder, Vmu.UserDataRootFolder);
            Assert.Equal(Path.Combine(userDataRootFolder, "DreamPotato"), Vmu.UserDataFolder);

            var vmu = new Vmu(mapleMessageBroker: null);
            Assert.Equal(Path.Combine(embeddedDataFolder, Vmu.RomFileName), vmu.RomFilePath);
            Assert.Equal(
                Path.Combine(userDataRootFolder, "DreamPotato", "SaveStates", "save_1.dpstate"),
                Vmu.GetSaveStatePath("save.vmu", "1"));
        }
        finally
        {
            Vmu.ConfigurePlatformPaths();
        }
    }
}
