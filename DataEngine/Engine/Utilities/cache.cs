using Microsoft.Extensions.Caching.Memory;
using DB.Documents;
using System.Text.Json;

namespace Cache;

public class PersistentCache
{
    private MemoryCache cache;
    private PersistentCache()
    {
        MemoryCacheOptions options = new();
        cache = new(options);
    }
    private static PersistentCache? instance;
    private static readonly Lock _lock = new();

    public static MemoryCache Get()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new PersistentCache();
            }
        }
        return instance.cache;
    }

    public static void Refresh()
    {
        instance = new PersistentCache();
    }
}

public class DisplayNameLookup
{
    Documents db;
    private MemoryCache cache;

    private DisplayNameLookup()
    {
        db = new();
        MemoryCacheOptions options = new();
        options.SizeLimit = 50000;
        cache = new(options);
    }
    private static DisplayNameLookup? instance;
    private static readonly Lock _lock = new();

    private static DisplayNameLookup Get()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new DisplayNameLookup();
            }
        }
        return instance;
    }

    public static void Initialize()
    {
        instance ??= Get();
    }

    public static string? ResolveID(UInt128 id)
    {
        string? displayName;
        if (Get().cache.TryGetValue(id, out displayName))
        {
            return displayName;
        }
        else
        {
            JsonDocument? result = Get().db.GetDoc("users", id);
            Get().db.Close();
            if (result is null)
            {
                displayName = null;
            }
            else
            {
                result.RootElement.TryGetProperty("displayName", out JsonElement displayNameElement);
                displayName = displayNameElement.GetString();
            }
            Get().cache.Set(id, displayName, new MemoryCacheEntryOptions().SetSlidingExpiration(new TimeSpan(0, 0, 30)).SetSize(1));
            return displayName;
        }
    }
}