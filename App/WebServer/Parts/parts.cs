using System.Collections.Immutable;
using System.Text.Json;
using Apps;
using AppSettings;
using BranchRegistration;
using Presentation;

namespace PartsClass;

public class Parts
{
#pragma warning disable CS8618
    private IApp app;
#pragma warning restore CS8618

    public string Get(string partName, IApp appArg)
    {
        app = appArg;
        return new string(GetPart(partName));
    }

    private char[] GetPart(string partName)
    {
        char[] partChars = PartCache.Get().parts[partName];
        ParseForCode(ref partChars);
        return partChars;
    }

    private static int FindCloseBrackets(ref char[] partChars, int curPos)
    {
        char close = '}';
        int end = partChars.Length;
        while (curPos < end)
        {
            curPos++;
            if (partChars[curPos] == close && partChars[curPos + 1] == close)
            {
                return curPos + 2;
            }
        }
        return -1;
    }

    private static int FindOpenBrackets(ref char[] partChars, ref int curPos)
    {
        char open = '{';
        for (int i = 0; i <= curPos; curPos--)
        {
            if (partChars[curPos] == open && partChars[curPos + 1] == open)
            {
                return curPos;
            }
        }
        return -1;
    }

    private static int FindPeriod(ref char[] partChars, int curPos)
    {
        char close = '.';
        int end = partChars.Length;
        while (curPos < end)
        {
            curPos++;
            if (partChars[curPos] == close)
            {
                return curPos;
            }
        }
        throw new ArgumentException("Improperly formatted tag; no '.' found.");
    }

    private void InsertBlocks(ref char[] partChars, int start, int end, string partType)
    {
        if (!app.TryGetDictionary(partType, out Dictionary<string, dynamic> partValues))
        {
            RemoveTag(ref partChars, start, end);
            return;
        }
        char[] newParts = [];

        string[] order = JsonSerializer.Deserialize<string[]>(app.GetVal(partType + "_order"))!;
        foreach (string ID in order)
        {
            BlankApp subApp = new();
            SetupSubApp(ref subApp, ref partValues, partType, ID);

            Parts subParts = new();
            char[] subPart = subParts.Get(partType, subApp).ToCharArray();
            subApp.Dispose();

            newParts = [.. newParts, .. subPart];
        }

        UpdateArray(ref partChars, start, end, newParts);
    }

    private void InsertBranch(ref char[] partChars, int start, int end, string type)
    {
        string partName = BranchRegister.Get().Lookup(type);
        char[] newPart = GetPart(partName);
        UpdateArray(ref partChars, start, end, newPart);
    }

    private void InsertPart(ref char[] partChars, int start, int end, string type)
    {
        char[] newPart = GetPart(type);
        UpdateArray(ref partChars, start, end, newPart);
    }

    private void InsertValue(ref char[] partChars, int start, int end, string value)
    {
        char[] insertVal = app.GetVal(value).ToCharArray();
        UpdateArray(ref partChars, start, end, insertVal);
    }

    private void ParseCodeValue(ref char[] partChars, int start, int end)
    {
        int period = FindPeriod(ref partChars, start);
        string type = new(partChars[(start + 2)..period]);
        string value = new(partChars[(period + 1)..(end - 2)]);

        if (type == "value")
        {
            InsertValue(ref partChars, start, end, value);
        }
        else if (type == "part")
        {
            InsertPart(ref partChars, start, end, value);
        }
        else if (type == "branch")
        {
            InsertBranch(ref partChars, start, end, value);
        }
        else if (type == "blocks")
        {
            InsertBlocks(ref partChars, start, end, value);
        }
    }

    private void ParseForCode(ref char[] partChars)
    {
        bool stillSearching = true;
        int curPos = partChars.Length - 1;
        while (stillSearching)
        {
            int start = FindOpenBrackets(ref partChars, ref curPos);
            int end = (start == -1) ? -1 : FindCloseBrackets(ref partChars, start);
            if (start != -1)
            {
                ParseCodeValue(ref partChars, start, end);
            }
            else
            {
                stillSearching = false;
            }
        }
    }

    private static void RemoveTag(ref char[] partChars, int start, int end)
    {
        UpdateArray(ref partChars, start, end, []);
    }

    private void SetupSubApp(ref BlankApp subApp, ref Dictionary<string, dynamic> partValues, string partType, string ID)
    {
        if (Pres.RemoveNonDigits(ID) == ID) ID = partType + ID; // add part type to identifier if it doesn't already exist
        JsonDocument doc = (JsonDocument)partValues[ID];
        subApp.SetManyValsLocal(doc);
        foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.Array)
            {
                subApp.SetDictionaryLocal(entry.Name, app.GetDictionary(entry.Name));
                subApp.SetValLocal(entry.Name + "_order", entry.Value.ToString());
            }
        }
        subApp.ValidateData(partType);
    }

    private static void UpdateArray(ref char[] partChars, int start, int end, char[] value)
    {
        partChars = [.. partChars[0..start], .. value, .. partChars[end..]];
    }

}

public class PartCache
{
    private static PartCache? instance;
    private static readonly Lock _lock = new();
    public readonly Dictionary<string, char[]> parts = [];
    private readonly ImmutableDictionary<string, string> partPaths;

    private PartCache()
    {
        string root = Settings.Get().rootPath;
        partPaths = JsonSerializer.Deserialize<ImmutableDictionary<string, string>>(File.ReadAllText(root + "WebServer/Parts/partPathRegister.json"))!;

        foreach (KeyValuePair<string, string> path in partPaths)
        {
            char[] partChars = File.ReadAllText(root + "Webserver/Parts/" + path.Value).ToCharArray();
            parts[path.Key] = partChars;
        }
    }

    public static PartCache Get()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new PartCache();
            }
        }
        return instance;
    }

    public static void Refresh()
    {
        instance = new PartCache();
    }
}