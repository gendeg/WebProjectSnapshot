using System.Net;
using Authentication;
using Pages.Builder;
using Fetch.Builder;
using Sessions;
using CSS.Plaza;
using Apps.Plaza;
using Presentation;

namespace WebServer;

public class RequestHandler
{
    private HttpRequest request;
    private string HTML = "";
    private string type;
    private int status = (int)HttpStatusCode.OK;

    public RequestHandler(string typeArg, HttpRequest requestArg, ref HttpResponse response)
    {
        if (requestArg.HasFormContentType)
        {
            // do POST actions
        }
        type = typeArg;
        request = requestArg;
        UpdateHeaders(ref response);
        BuildHTML();
    }

    private void BuildHTML()
    {
        PageDirector page = new(type, ref request);
        if (page.GetHTML() != "__404__")
        {
            page.BuildPage();
            HTML = page.GetHTML();
        }
        else
        {
            status = 404;
        }
        page.Dispose();
    }

    public IResult GetResult()
    {
        IResult response = Results.Text(content: HTML, contentType: "text/html", statusCode: status);
        CurrentSession.StoreTokens();
        return response;
    }

    private static void UpdateHeaders(ref HttpResponse response)
    {
        HeaderHandler.AddCommonHeaders(ref response);
    }
}


public class FetchHandler
{
    private HttpRequest request;
    private string result = "";
    private int status = (int)HttpStatusCode.OK;

    public FetchHandler(HttpRequest requestArg, ref HttpResponse response)
    {
        request = requestArg;
        UpdateHeaders(ref response);
        BuildResult();
    }

    private void BuildResult()
    {
        FetchDirector fetch = new(ref request);
        fetch.PrepJSONString();
        if (fetch.GetStatus() == 200)
        {
            result = fetch.GetJSONString();
        }
        else
        {
            status = fetch.GetStatus();
        }
        fetch.Dispose();
    }

    public IResult GetResult()
    {
        IResult response = Results.Text(content: result, contentType: "text/plain", statusCode: status);
        CurrentSession.UpdateTokens();
        return response;
    }

    private static void UpdateHeaders(ref HttpResponse response)
    {
        HeaderHandler.AddCommonHeaders(ref response);
    }
}


public class CSSHandler
{
    private HttpRequest request;
    private string CSS = "";
    private string type;
    private int status = (int)HttpStatusCode.OK;
    private string path;

    public CSSHandler(string typeArg, HttpRequest requestArg, ref HttpResponse response)
    {
        type = typeArg;
        request = requestArg;
        path = request.Path;
        UpdateHeaders(ref response);
        BuildCSS();
    }

    private void BuildCSS()
    {
        if (type == "plazaCSS")
        {
            string plazaName = (string)request.RouteValues["plazaName"]!;

            // check if RemoveNonAlphaNum removed any characters, and if so, return 404
            if (plazaName.Length == Pres.RemoveNonAlphaNum(plazaName).Length)
            {
                PlazaApp app = new(plazaName);
                if (app.status == 404)
                {
                    status = 404;
                }
                else
                {
                    CSS = new PlazaCSS(app).GetCSS();
                }
                app.Dispose();
            }
            else
            {
                status = 404;
            }
        }
    }

    public IResult GetResult()
    {
        IResult response = Results.Text(content: CSS, contentType: "text/css", statusCode: status);
        return response;
    }

    private static void UpdateHeaders(ref HttpResponse response)
    {
        HeaderHandler.AddCommonHeaders(ref response);
    }
}


public class HeaderHandler
{
    public static void AddCommonHeaders(ref HttpResponse response)
    {
        response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    }
}