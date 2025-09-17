using Fetch.Builder;
using PartsClass;
using Apps.Post;
using Presentation;
using System.Text.Json;

namespace Fetch.PostPage;


public class LoadPostPage(JsonElement requestArg) : IFetchBuilder(requestArg)
{
    public override void BuildResponseValues()
    {
        string rawPostId;
        if (requestDoc.TryGetProperty("postId", out JsonElement postIdEl))
        {
            rawPostId = postIdEl.GetString()!;
        }
        else
        {
            Return404();
            return;
        }

        if (!UInt128.TryParse(rawPostId, out UInt128 postId))
        {
            Return404();
            return;
        }

        PostApp app = new(postId, "??Fetch??");
        if (app.status == 404)
        {
            Return404();
            return;
        }

        string pageHTML = new Parts().Get("postPage", app);
        AddValue("html", pageHTML);
    }

    private bool InputsAreValid(string postURL, string plazaName)
    {
        return Pres.ValidateIdURL(postURL) && plazaName.Length == Pres.RemoveNonAlphaNum(plazaName).Length;
    }
}