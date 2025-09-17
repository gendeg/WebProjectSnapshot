using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sessions;
using System.Security.Cryptography;

namespace Authentication;

public class Authenticator
{
    private bool loggedIn;
    private SessionUser? user;


    public Authenticator()
    {
        loggedIn = ThisContext.LoggedIn();
        if (loggedIn)
        {
            user = ((CurrentSession)ThisContext.Get().Items["currentSession"]!).GetUser();
        }
    }

    public async void Login(string LoginName)
    {
        Claim[] claims = [new(ClaimTypes.Name, LoginName, ClaimValueTypes.String)];
        ClaimsIdentity userIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal userPrincipal = new(userIdentity);
        string userString = CurrentSession.UserToString(new SessionUser(LoginName));

        await ThisContext.Get().SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userPrincipal,
            new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddMinutes(20),
                IsPersistent = false,
                AllowRefresh = true
            });
        ThisContext.Get().Session.SetString("user", userString);
    }

    public async void Logout()
    {
        await ThisContext.Get().SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}


public static class ThisContext
{
    private static HttpContext? currentContext;
    private static bool loggedIn;

    public static void Initialize(HttpContext context)
    {
        currentContext = context;
        loggedIn = context.User.Identity is not null && context.User.Identity.IsAuthenticated;
    }

    public static HttpContext Get()
    {
        return currentContext!;
    }

    public static bool LoggedIn()
    {
        return loggedIn;
    }
}


public static class Nonce
{
    public static string Generate()
    {
        return RandomNumberGenerator.GetHexString(16);
    }
}