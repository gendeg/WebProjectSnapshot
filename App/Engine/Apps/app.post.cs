using System.Text.Json;
using DB.Documents;
using Cache;
using Authentication;
using Sessions;

namespace Apps.Post;


public class PostApp : IApp
{
    Documents db = new();

    public PostApp(UInt128 postId, string plazaName)
    {
        JsonDocument? result = db.GetDoc("posts", postId);
        if (result is null) return;
        ResolveAuthorName(ref result);
        SetManyValsLocal(result);
        if (GetVal("plaza") != plazaName && plazaName != "??Fetch??") return;
        /* Passing "??Fetch??" as the plazaName bypasses checking the post belongs to the current plaza.
           Fetch requests for posts may come from other plazas (e.g. from home page) so this check isn't
           necesssary.  The string "??Fetch??" should not be possible from Pages.PostBuilder requests
           because non-alphanum characters should have already been scrubbed, and an imporoper route
           with '?' in the plaza name would be interpreted as GET data instead.  */

        CurrentSession session = (CurrentSession)ThisContext.Get().Items["currentSession"]!;
        string nonce = Nonce.Generate();
        session.tokens.SetCurrent("postCommentNonce", nonce);
        SetValLocal("postCommentNonce", nonce);

        status = 200;
    }

    private void ResolveAuthorName(ref JsonDocument doc)
    {
        Dictionary<string, JsonElement> temp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(doc)!;
        string? displayName = DisplayNameLookup.ResolveID(UInt128.Parse(temp["author"].GetString()!));
        temp["author"] = JsonSerializer.Deserialize<JsonElement>($"\"{displayName}\"");
        doc = JsonDocument.Parse(JsonSerializer.Serialize(temp));
    }

    public override void SetValStorage(string key, string value) { }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            db.Close();
        }
        _disposed = true;
    }
}