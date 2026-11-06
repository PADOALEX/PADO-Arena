using Pado.Arena.Core.Combat;
using Pado.Arena.Core.Game;
using Pado.Arena.Core.Persistence;
using Xunit;

namespace Pado.Arena.Tests;

public sealed class SaveConcurrencyTests
{
    [Fact]
    public void SavingDoesNotOverwriteAnotherWritersTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "arena-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "save.json");
            File.WriteAllText(path + ".tmp", "another writer's data");
            var session = GameFactory.NewGame("Warrior", "Save test", new SeededRandomSource(1));
            SaveGameStore.Save(path, session);
            Assert.Equal("another writer's data", File.ReadAllText(path + ".tmp"));
            Assert.Equal("Warrior", SaveGameStore.Load(path, new SeededRandomSource(1)).Hero.ClassId);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task ConcurrentSavesLeaveOneValidSaveAndNoTemporaryFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "arena-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "save.json");
            await Task.WhenAll(Enumerable.Range(0, 16).Select(seed => Task.Run(() =>
                SaveGameStore.Save(path, GameFactory.NewGame("Mage", "Save test", new SeededRandomSource(seed))))));
            Assert.Equal("Mage", SaveGameStore.Load(path, new SeededRandomSource(1)).Hero.ClassId);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, true); }
    }
}
