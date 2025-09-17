using Apps.Plaza;
using Pages.Builder;
using PartsCommon;
using Presentation;

namespace Pages.Plaza;


public class PlazaBuilder : IPageBuilder
{
    private string plazaName;

    public PlazaBuilder(ref HttpRequest request)
    {
        plazaName = (string)request.RouteValues["plazaName"]!;

        // check if RemoveNonAlphaNum removes any characters, and if so, return 404
        if (plazaName.Length == Pres.RemoveNonAlphaNum(plazaName).Length)
        {
            app = new PlazaApp(plazaName);
            if (app.status == 404) Return404();
        }
        else
        {
            Return404();
        }
    }

    public override void BuildHead()
    {
        HTML.Append(CommonParts.HTMLDocStart);
        HTML.Append($"""<link rel="stylesheet" href="{plazaName}/style.css">""");
        HTML.Append("""<script src="/scripts/responsive.js"></script>""");
        HTML.Append($"<title>{app.GetVal("title")}</title>\n");
        HTML.Append(CommonParts.HeadClose);
    }

    public override void BuildBody()
    {
        HTML.Append(parts.Get("plaza", app));
    }

    public override void ClosePage()
    {
        HTML.Append("<script src=\"/scripts/listeners.js\"></script>\n");
        HTML.Append(CommonParts.HTMLDocEnd);
    }
}