using System.Text.Json;

namespace AppSettings;

public class Settings
{
    private static Settings? instance;
    private static readonly Lock _lock = new();
    private readonly Dictionary<string, object> lookup;
    public string rootPath;

    private Settings()
    {
        lookup = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText("./Properties/settings.json"))!;
        rootPath = JsonSerializer.Deserialize<string>((JsonElement)lookup["rootPath"])!;
    }

    public static Settings Get()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new Settings();
            }
        }
        return instance;
    }

    public static int? GetInt(string setting)
    {
        instance ??= Get();
        try
        {
            return JsonSerializer.Deserialize<int>((JsonElement)instance.lookup[setting])!;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public static string? GetString(string setting)
    {
        instance ??= Get();
        try
        {
            return JsonSerializer.Deserialize<string>((JsonElement)instance.lookup[setting])!;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public static JsonElement? GetJSON(string setting)
    {
        instance ??= Get();
        try
        {
            return (JsonElement)instance!.lookup[setting];
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public static void Refresh()
    {
        instance = new Settings();
    }
}
