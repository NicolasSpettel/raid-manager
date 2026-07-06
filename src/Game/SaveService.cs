namespace Game;

/// <summary>
/// The one entry point for persistence: serialize the aggregate to a storage adapter, and load it back
/// through the full migrate + validate pipeline. Scenes never touch files or JSON directly.
/// </summary>
public sealed class SaveService
{
    private readonly IStorageAdapter _storage;

    public SaveService(IStorageAdapter storage) => _storage = storage;

    public void Save(GuildSave save) => _storage.Save(SaveSerializer.Serialize(save));

    /// <summary>Load the save, or null if none exists. Throws <see cref="SaveException"/> on a corrupt blob.</summary>
    public GuildSave? Load()
    {
        string? json = _storage.Load();
        return json is null ? null : SaveSerializer.Load(json);
    }
}
