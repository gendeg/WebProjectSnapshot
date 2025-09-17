using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using System.Reflection;
using Authentication;

namespace Sessions;

public class CurrentSession
{
    public SessionUser? user;
    public TokenStorage tokens;

    public CurrentSession(ref HttpContext context)
    {
        string? userString = context.Session.GetString("user");
        if (userString is not null)
        {
            user = StringToUser(userString);
        }

        string? tokenString = context.Session.GetString("tokens");
        if (tokenString is not null)
        {
            tokens = new(tokenString);
        }
        else
        {
            tokens = new("");
        }
    }

    public SessionUser? GetUser()
    {
        return user;
    }

    public static string UserToString(SessionUser user)
    {
        PropertyInfo[] properties = typeof(SessionUser).GetProperties();

        Dictionary<string, object> values = [];
        foreach (PropertyInfo prop in properties)
        {
            if (prop.Name != "Id")
            {
                values[prop.Name] = prop.GetValue(user)!;
            }
            else
            {
                values[prop.Name] = prop.GetValue(user)!.ToString()!;
            }
        }

        return JsonSerializer.Serialize(values);
    }

    public static SessionUser StringToUser(string userString)
    {
        Dictionary<string, JsonElement> values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userString)!;
        SessionUser user = new(values["UserName"].ToString()!);

        Type type = typeof(SessionUser);

        foreach (KeyValuePair<string, JsonElement> entry in values)
        {
            PropertyInfo property = type.GetProperty(entry.Key)!;

            if (SessionUser.stringVals.Contains(entry.Key))
            {
                property.SetValue(user, entry.Value.GetString());
            }
            else if (SessionUser.boolVals.Contains(entry.Key))
            {
                property.SetValue(user, entry.Value.GetBoolean());
            }
            else if (entry.Key == "Restrictions")
            {
                property.SetValue(user, (ContentRestrictions)entry.Value.GetInt16());
            }
            else if (entry.Key == "AccountType ")
            {
                property.SetValue(user, (AccountType)entry.Value.GetInt16());
            }
            else if (entry.Key == "SubscriptionLevel")
            {
                property.SetValue(user, (SubscriptionLevel)entry.Value.GetInt16());
            }
            else if (entry.Key == "SuspensionEnd" || entry.Key == "LockoutEnd")
            {
                if (entry.Value.ToString() != "") property.SetValue(user, entry.Value.GetDateTimeOffset());
            }
            else if (entry.Key == "AccessFailedCount")
            {
                property.SetValue(user, entry.Value.GetInt32());
            }
            else if (entry.Key == "Id")
            {
                property.SetValue(user, UInt128.Parse(entry.Value.ToString()));
            }
        }

        return user;
    }

    public static void StoreTokens()
    {
        HttpContext context = ThisContext.Get();
        string tokens = ((CurrentSession)context.Items["currentSession"]!).tokens.ExportCurrent();
        context.Session.SetString("tokens", tokens);
    }

    public static void UpdateTokens()
    {
        HttpContext context = ThisContext.Get();
        string tokens = ((CurrentSession)context.Items["currentSession"]!).tokens.ExportBoth();
        context.Session.SetString("tokens", tokens);
    }
}


public class SessionUser : IdentityUser<UInt128>
{
    public readonly static string[] stringVals = ["DisplayName", "ConcurrencyStamp", "Email", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "SecurityStamp", "UserName"];
    public readonly static string[] boolVals = ["IdConfirmed", "Suspended", "EmailConfirmed", "LockoutEnabled", "PhoneNumberConfirmed", "TwoFactorEnabled"];

    public SessionUser(string AccountName)
    {
        UserName = AccountName;
        NormalizedUserName = AccountName.ToUpper();
        DisplayName = "";
        IdConfirmed = false;
        Restrictions = ContentRestrictions.Default;
        SubLevel = SubscriptionLevel.Free;
        Suspended = false;
    }

    public string DisplayName { get; set; }
    public bool IdConfirmed { get; set; }
    public ContentRestrictions Restrictions { get; set; }
    public AccountType AccountType { get; set; }
    public SubscriptionLevel SubLevel { get; set; }
    public bool Suspended { get; set; }
    public DateTimeOffset? SuspensionEnd { get; set; }

    /*
    --- Inhereted IdentityUser properties ---

    Int32 AccessFailedCount

    string ConcurrencyStamp
    A random value that must change whenever a user is persisted to the store

    string Email

    bool EmailConfirmed

    UInt128 Id

    bool LockoutEnabled

    DateTimeOffset? LockoutEnd

    string NormalizedEmail
    Gets or sets the normalized email address for this user - ALL UPPERCASE

    string NormalizedUserName
    Gets or sets the normalized user name for this user - ALL UPPERCASE

    string PasswordHash

    string PhoneNumber

    bool PhoneNumberConfirmed

    string SecurityStamp
    A random value that must change whenever a users credentials change (password changed, login removed)

    bool TwoFactorEnabled

    string UserName

    */
}


public enum SubscriptionLevel : ushort
{
    Free = 0,
    Basic = 1,
    Premium = 2
}


public enum AccountType : ushort
{
    User = 0,
    Corporate = 1,
    Bot = 2
}


public enum ContentRestrictions : ushort
{
    Default = 0,
    Child = 1,
    Adult = 2
}


public class TokenStorage
{
    private Dictionary<string, string> priorSession = [];
    private Dictionary<string, string> currentSession = [];

    public TokenStorage(string prior)
    {
        ImportPrior(prior);
    }

    public string ExportBoth()
    {
        Dictionary<string, string> combinedTokens = new(priorSession);
        foreach (KeyValuePair<string, string> token in currentSession) {
            combinedTokens[token.Key] = token.Value;
        }
        return JsonSerializer.Serialize(combinedTokens);
    }

    public string ExportCurrent()
    {
        return JsonSerializer.Serialize(currentSession);
    }

    public void ImportPrior(string prior)
    {
        if (prior != "")
        {
            priorSession = JsonSerializer.Deserialize<Dictionary<string, string>>(prior)!;
        }
    }

    public void SetCurrent(string key, string value)
    {
        currentSession[key] = value;
    }

    public void SetPrior(string key, string value)
    {
        priorSession[key] = value;
    }

    public bool TryGetCurrent(string key, out string? value)
    {
        return currentSession.TryGetValue(key, out value);
    }

    public bool TryGetPrior(string key, out string? value)
    {
        return priorSession.TryGetValue(key, out value);
    }
}