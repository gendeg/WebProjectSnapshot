using Apps.Post;
using Pages.Builder;
using PartsCommon;
using Presentation;

namespace Pages.Post;


public class PostBuilder : IPageBuilder
{
    private UInt128 postId;

    public PostBuilder(ref HttpRequest request)
    {
        string postURL = (string)request.RouteValues["postURL"]!;
        if (!Pres.ValidateIdURL(postURL))
        {
            Return404();
            return;
        }
        postId = Pres.URLToID(postURL);

        string plazaName = (string)request.RouteValues["plazaName"]!;
        if (plazaName.Length != Pres.RemoveNonAlphaNum(plazaName).Length)
        {
            Return404();
            return;
        }

        app = new PostApp(postId, plazaName);
        if (app.status == 404) Return404();
    }

    public override void BuildHead()
    {
        HTML.Append(CommonParts.HTMLDocStart);
        HTML.Append($"<title>{app.GetVal("title")}</title>\n");
        HTML.Append(CommonParts.HeadClose);
    }

    public override void BuildBody()
    {
        HTML.Append("<body>\n    ");
        HTML.Append(parts.Get("header", app));
        HTML.Append("    <main>\n");
        HTML.Append(parts.Get("postPage", app));
        HTML.Append("    </main>\n</body>\n");
    }

    public override void ClosePage()
    {
        HTML.Append(CommonParts.HTMLDocEnd);
    }
}