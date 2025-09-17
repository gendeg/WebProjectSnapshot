using Apps;
using Authentication;
using Pages.Builder;
using Sessions;
using PartsCommon;

namespace Pages.HomePage;


public class HomePageBuilder : IPageBuilder
{
    public HomePageBuilder(ref HttpRequest requestArg)
    {
        if (requestArg.Query.TryGetValue("login", out var loginVal))
        {
            Authenticator auth = new();
            if (loginVal == "test")
            {
                auth.Login("testUser");
            }
            if (loginVal == "logout")
            {
                auth.Logout();
            }
        }
        app = new BlankApp();
    }

    public override void BuildHead()
    {
        HTML.Append(CommonParts.HTMLDocStart);
        HTML.Append("<title>Plaza Home</title>\n");
        HTML.Append(CommonParts.HeadClose);
    }
    public override void BuildBody()
    {
        HTML.Append(parts.Get("home", app));
        /*
        HTML.Append("Yeehaw! A Header!\n");
        HTML.Append("I'm the Main Attraction!<a href=\"/Plaza/demo\"> link</a>\n");
        */
    }
    public override void ClosePage()
    {
        // HTML.Append("Down here at the foot!\n");
        if (ThisContext.LoggedIn())
        {
            CurrentSession sesh = (CurrentSession)ThisContext.Get().Items["currentSession"]!;
            HTML.Append($"The logged in User is {sesh.user!.UserName}\n");
        }
        HTML.Append(CommonParts.HTMLDocEnd);
    }
}


