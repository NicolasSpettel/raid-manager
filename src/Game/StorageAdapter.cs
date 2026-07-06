using System.IO;

namespace Game;

/// <summary>Where a save blob is read from and written to. The engine/app choose the concrete adapter.</summary>
public interface IStorageAdapter
{
    /// <summary>The stored blob, or null if none exists yet.</summary>
    string? Load();

    /// <summary>Persist the blob (atomically for a real file store).</summary>
    void Save(string blob);
}

/// <summary>
/// A single-file store with atomic writes: write to a temp file, then replace the target, keeping one
/// <c>.bak</c> sibling — so a crash mid-write never corrupts the save (save-format.md §Storage adapters).
/// The Godot app points this at <c>user://saves/</c>; tests point it at a temp path.
/// </summary>
public sealed class FileStorageAdapter : IStorageAdapter
{
    private readonly string _path;

    public FileStorageAdapter(string path) => _path = path;

    public string? Load() => File.Exists(_path) ? File.ReadAllText(_path) : null;

    public void Save(string blob)
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temp = _path + ".tmp";
        File.WriteAllText(temp, blob);

        if (File.Exists(_path))
        {
            File.Replace(temp, _path, _path + ".bak");
        }
        else
        {
            File.Move(temp, _path);
        }
    }
}
