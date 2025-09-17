using System.Text.Json;
using DB.Documents;
using Cache;
using Presentation;

namespace Apps.Plaza;

public class PlazaApp : IApp
{
    public UInt128 plazaID = 0;
    Documents db = new();
    public List<string> panelList = [];

    public PlazaApp(string plazaName)
    {
        db.SetOR();
        object[]? result = db.GetDocByName("plazas", plazaName);
        if (result is null) return;
        plazaID = (UInt128)result[0];

        SetManyValsLocal((JsonDocument)result[1]);

        SetValLocal("titleLetter", Values["title"][0].ToString());
        SetPostData();
        ResolveAuthorNames();

        ValidateData("plazas");
        status = 200;
    }

    private void AddValueEnumerable(string name, ref List<JsonDocument> docs, ref List<UInt128> IDs)
    {
        if (IDs.Count != docs.Count) throw new ArgumentException($"Did not receive the expected number of {name} from the database");

        Dictionary<string, dynamic> dictionary = [];
        for (int i = 0; i < docs.Count; i++)
        {
            dictionary[name + IDs[i].ToString()] = docs[i];
        }

        Dictionaries[name] = dictionary;
    }

    private void SetPostData()
    {
        if (!Dictionaries.ContainsKey("boards")) return;

        List<UInt128> postIDList = GetPostIDs();
        List<JsonDocument>? results = db.GetManyDocs("posts", "id", postIDList, orderBy: "id ASC");
        if (results is null) return;
        postIDList.Sort();

        AddValueEnumerable("postCards", ref results, ref postIDList);
    }

    private void ResolveAuthorNames()
    {
        if (!Dictionaries.ContainsKey("postCards")) return;

        foreach (KeyValuePair<string, dynamic> post in Dictionaries["postCards"])
        {
            Dictionary<string, JsonElement> temp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(post.Value);
            string? displayName = DisplayNameLookup.ResolveID(UInt128.Parse(temp["author"].GetString()!));
            temp["author"] = JsonSerializer.Deserialize<JsonElement>($"\"{displayName}\"");
            Dictionaries["postCards"][post.Key] = JsonDocument.Parse(JsonSerializer.Serialize(temp));
        }

    }

    private List<UInt128> GetPostIDs()
    {
        var postsDocs = Dictionaries["boards"];

        List<UInt128> postIDList = [];
        foreach (KeyValuePair<string, dynamic> doc in postsDocs)
        {
            if (doc.Value.RootElement.TryGetProperty("postCards", out JsonElement postsArray))
            {
                List<string> rawList = JsonSerializer.Deserialize<List<string>>(postsArray)!;
                postIDList.AddRange(rawList.ConvertAll(UInt128.Parse));
            }
        }
        return postIDList;
    }

    private void ParseCarouselArray(JsonProperty cards)
    {
        SetValLocal("carouselCards_order", cards.Value.ToString());
        List<string> cardIDs = cards.Value.Deserialize<List<string>>()!;

        Dictionary<string, dynamic> carouselCards = [];
        foreach (string card in cardIDs)
        {
            string type = Pres.RemoveNonAlpha(card);
            string title = ((JsonDocument)Dictionaries[type][card]).RootElement.GetProperty("title").GetString()!;
            carouselCards.Add(card, JsonDocument.Parse($$"""{"title": "{{title}}", "target":"{{card}}"}"""));
        }

        Dictionaries["carouselCards"] = carouselCards;
    }

    private void ParseJSONArray(JsonProperty entry)
    {
        SetValLocal(entry.Name + "_order", entry.Value.ToString());
        List<string> rawArrayVals = entry.Value.Deserialize<List<string>>()!;
        List<UInt128> arrayVals = rawArrayVals.ConvertAll(UInt128.Parse);

        List<JsonDocument>? results = db.GetManyDocs(entry.Name, "id", arrayVals, orderBy: "id ASC");
        if (results is null) return;
        arrayVals.Sort();

        AddValueEnumerable(entry.Name, ref results, ref arrayVals);
    }

    public override void SetValStorage(string key, string value)
    {

    }

    public override void SetManyValsLocal(JsonDocument jsonDoc)
    {
        JsonProperty? carousel = null;
        foreach (JsonProperty entry in jsonDoc.RootElement.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.Array)
            {
                if (entry.Name != "carouselCards") ParseJSONArray(entry);
                else carousel = entry;
            }
            else
            {
                SetValLocal(entry.Name, entry.Value.ToString());
            }
        }
        if (carousel is not null) ParseCarouselArray((JsonProperty)carousel);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            Dictionaries = [];
            Values = [];
            db.Close();
        }
        _disposed = true;
    }
}