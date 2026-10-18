using System.Text.Json;
using Pado.Arena.Core.Combat;
using Pado.Arena.Core.Game;

namespace Pado.Arena.Core.Persistence;

public static class SaveGameStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = false };
    private const long MaxSaveBytes = 1_000_000;

    public static void Save(string path, GameSession session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(session);
        GameSave state = session.CaptureSave();
        string json = JsonSerializer.Serialize(state, Options);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxSaveBytes)
            throw new InvalidDataException("Save game is too large.");
        // Write to a sibling temporary file, then atomically replace the target where supported.
        string fullPath = Path.GetFullPath(path);
        string temporary = fullPath + ".tmp";
        try
        {
            File.WriteAllText(temporary, json);
            File.Move(temporary, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static GameSession Load(string path, IRandomSource random)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(random);
        FileInfo file = new(path);
        if (!file.Exists) throw new FileNotFoundException("Save file not found.", path);
        if (file.Length > MaxSaveBytes) throw new InvalidDataException("Save file is too large.");
        try
        {
            GameSave? state = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(path), Options);
            return GameSession.FromSave(state ?? throw new InvalidDataException("Empty save game."), random);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Save file contains invalid JSON.", exception);
        }
    }
}