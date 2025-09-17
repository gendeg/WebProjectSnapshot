using System.Collections;
using System.Text.Json;
using System.Text;
using System.Net;

namespace Apps;

/*
Data in an IApp should be organized as follows:

Values:
    - Dictionary with values for the part and all parts inserted directly with {parts.myPart} references
    - E.g.
        "title": "MyTitle",
        "content": "MyContent",
        "headerValue": "MyHeaderValue"
    - Also includes an entry with the order for each set of blocks with the key "{type}_order";
        stored as stringified array of block IDs
    - E.g.
        "boards_order": "[731534534, 149738246, 912774190]"

Dictionaries:
    - Dictionary of dictionaries with information for blocks
    - Top-level dictionary is keyed for each block type (e.g. "boards", "postCards")
    - Sub-dictionary is keyed for each block instance;
        key is "{type}{ID}" (e.g. "boards731534534", "postCards912774190")
        value is a JsonDocument with information for the block instance
*/

public abstract class IApp : IDisposable
{
    protected Dictionary<string, string> Values = [];
    protected Dictionary<string, Dictionary<string, dynamic>> Dictionaries = [];
    public int status = (int)HttpStatusCode.NotFound;

    public virtual Dictionary<string, dynamic> GetDictionary(string key)
    {
        return Dictionaries[key];
    }

    public virtual bool TryGetDictionary(string key, out Dictionary<string, dynamic> value)
    {
        return Dictionaries.TryGetValue(key, out value!);
    }

    public virtual string GetVal(string key)
    {
        return Values[key];
    }

    public virtual void SetDictionaryLocal(string key, Dictionary<string, dynamic> value)
    {
        Dictionaries[key] = value;
    }

    public virtual void SetVal(string key, string value)
    {
        SetValLocal(key, value);
        SetValStorage(key, value);
    }

    public virtual void SetValLocal(string key, string value)
    {
        Values[key] = value;
    }

    public virtual void SetManyValsLocal(string jsonString)
    {
        List<KeyValuePair<string, string>> appValues = AppDeserialize(jsonString);
        foreach (KeyValuePair<string, string> appValue in appValues)
        {
            SetValLocal(appValue.Key, appValue.Value.ToString());
        }
    }

    public virtual void SetManyValsLocal(JsonDocument jsonDoc)
    {
        foreach (JsonProperty entry in jsonDoc.RootElement.EnumerateObject())
        {
            SetValLocal(entry.Name, entry.Value.ToString());
        }
    }

    public abstract void SetValStorage(string key, string value);



    // SUPPORT FUNCTIONS //

    protected bool _disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            Dictionaries = [];
            Values = [];
        }
        _disposed = true;
    }

    private static List<KeyValuePair<string, string>> AppDeserialize(string jsonString)
    {
        ReadOnlySpan<byte> jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        Utf8JsonReader reader = new(jsonBytes);
        List<KeyValuePair<string, string>> jsonList = [];

        string key;
        JsonElement element;
        reader.Read();
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            key = reader.GetString()!;
            reader.Read();
            element = JsonElement.ParseValue(ref reader);
            jsonList.Add(new KeyValuePair<string, string>(key, element.ToString()));
            reader.Skip();
            reader.Read();
        }
        return jsonList;
    }

    public virtual void ValidateData(string type)
    {
        IDictionary[] reference = DataRegister.Get(type);

        foreach (DictionaryEntry required in reference[0])
        {
            if (!Values.ContainsKey((string)required.Key))
            {
                throw new ArgumentException($"App {type} is missing required entry for {required.Key}.");
            }
        }
        foreach (DictionaryEntry optional in reference[1])
        {
            if (!Values.ContainsKey((string)optional.Key))
            {
                Values[(string)optional.Key] = ((string[])optional.Value!)[1];
            }
        }
    }
}


public class DataRegister
{
    private static DataRegister? instance;
    private static readonly Lock _lock = new();
    private readonly Dictionary<string, Dictionary<string, JsonElement>> register;

    private DataRegister()
    {
        register = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(File.ReadAllText("./WebServer/Parts/partDataRegister.json"))!;
    }

    private static DataRegister Create()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new DataRegister();
            }
        }
        return instance;
    }

    public static void Initialize()
    {
        instance ??= Create();
    }

    public static IDictionary[] Get(string type)
    {
        instance ??= Create();
        Dictionary<string, JsonElement> typeHeirarchy = instance.register[type];
        Dictionary<string, string> required = JsonSerializer.Deserialize<Dictionary<string, string>>(typeHeirarchy["required"])!;
        Dictionary<string, string[]> optional = JsonSerializer.Deserialize<Dictionary<string, string[]>>(typeHeirarchy["optional"])!;

        return [required, optional];
    }

    public static void Refresh()
    {
        instance = new DataRegister();
    }
}


public class BlankApp : IApp
{
    public override void SetValStorage(string key, string value) { }
}